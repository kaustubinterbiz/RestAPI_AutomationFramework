# 📦 PART 2 — ApiHost & RestClientFactory

> **Goal:** Samajhna ki framework **kaunse server pe** request bhejta hai, woh decision
> kaise hota hai, aur `RestClient` (actual HTTP client) kaise banta aur cache hota hai.

Yeh layer "traffic director" hai — har request se pehle decide hota hai:
**Auth server (B2C login) pe jaao ya App API server pe?**

---

## 2.1 — ApiHost enum (do servers, do names)

📄 `Core/Clients/ApiHost.cs`

```csharp
public enum ApiHost
{
    Auth,   // Azure AD B2C → login/token calls
    Api     // Application server → actual data APIs
}
```

Bas do options. Har HTTP call ke saath yeh enum pass hota hai taaki correct
base URL use ho.

`appsettings.json` mein mapping:
```json
"ApiUrls": {
  "AuthBaseUrl": "https://testRovicare.b2clogin.com/",
  "ApiBaseUrl":  "https://as-rc-test-api.azurewebsites.net/"
}
```

**Rule:**
- `ApiHost.Auth` → `AuthBaseUrl` — sirf **login/token** ke liye
- `ApiHost.Api` → `ApiBaseUrl` — login ke baad ki **saari API calls**

---

## 2.2 — RestClientFactory (client banane + cache karne ki machine)

📄 `Core/Clients/RestClientFactory.cs`

```csharp
public sealed class RestClientFactory
{
    private readonly ApiEnvironmentSettings _settings;
    private readonly ConcurrentDictionary<ApiHost, RestClient> _clients = new();

    public RestClient GetClient(ApiHost host) =>
        _clients.GetOrAdd(host, CreateClient);   // cache hit → same client
}
```

### Kaam kaise karta hai

1. `GetClient(ApiHost.Api)` call aaya.
2. Dictionary mein check karta hai — client pehle se hai?
   - **Hai** → wahi return (koi naya object nahi banta).
   - **Nahi** → `CreateClient(ApiHost.Api)` call, naya banata hai, dictionary mein daalta hai.
3. Thread-safe hai (`ConcurrentDictionary`) — parallel scenarios mein koi race condition nahi.

### CreateClient — andar kya hota hai

```csharp
private RestClient CreateClient(ApiHost host)
{
    var baseUrl = host switch
    {
        ApiHost.Auth => _settings.AuthBaseUrl,   // "https://testRovicare.b2clogin.com/"
        ApiHost.Api  => _settings.ApiBaseUrl,    // "https://as-rc-test-api.azurewebsites.net/"
        _ => throw new ArgumentOutOfRangeException(...)
    };

    var options = new RestClientOptions(baseUrl.TrimEnd('/') + "/")
    {
        Timeout = TimeSpan.FromMilliseconds(_settings.TimeoutMilliseconds) // default 30s
    };

    return new RestClient(options);
}
```

🧠 **Insight:** BaseUrl ke end mein `/` ensure hota hai — taaki endpoint ke saath
join karte waqt double-slash ya missing-slash ki problem na aaye.

---

## 2.3 — ApiHostContext (scenario ke liye current host store karta hai)

📄 `Core/Clients/ApiHostContext.cs`

```csharp
public static class ApiHostContext
{
    private static readonly AsyncLocal<ApiHost?> Current = new();

    public static ApiHost CurrentOrDefault => Current.Value ?? ApiHost.Api;
    public static bool HasValue            => Current.Value.HasValue;

    public static void Set(ApiHost host)   => Current.Value = host;
    public static void Clear()             => Current.Value = null;
}
```

### AsyncLocal — kyun important hai

`AsyncLocal<T>` matlab har async chain ka apna independent value. Parallel mein
chalte 10 scenarios mein se ek `Auth` set kare, doosre pe koi asar nahi.

```
Scenario A (async)  → ApiHostContext.Set(ApiHost.Auth)  → sirf A ko dikhta hai
Scenario B (async)  → ApiHostContext.Set(ApiHost.Api)   → sirf B ko dikhta hai
```

- **`CurrentOrDefault`** → agar koi set nahi, default `ApiHost.Api` milta hai (safe fallback)
- **`Clear()`** → `AfterScenario` hook mein call hota hai — scenario khatam, value reset

---

## 2.4 — ApiHostResolver (string → ApiHost)

📄 `Core/Clients/ApiHostResolver.cs`

Feature files mein step likhte ho:
```gherkin
When User sends POST request on "Auth" base url
```

Yeh `"Auth"` string hai. `ApiHostResolver` isko `ApiHost.Auth` mein convert karta hai.

```csharp
// Auth ke aliases
AuthKeys = { "Auth", "B2C", "Login", "Token", "Identity", "AuthBaseUrl" }

// Api ke aliases
ApiKeys  = { "Api", "App", "Application", "ApiBaseUrl", "Rovicare" }
```

Main methods:

```csharp
// String → ApiHost (throw karta hai agar unknown)
ApiHost host = ApiHostResolver.ResolveFromKey("Auth");   // → ApiHost.Auth
ApiHost host = ApiHostResolver.ResolveFromKey("B2C");    // → ApiHost.Auth (alias)
ApiHost host = ApiHostResolver.ResolveFromKey("App");    // → ApiHost.Api

// Try pattern (throw nahi karta)
if (ApiHostResolver.TryResolveFromKey("Auth", out var host)) { ... }

// Tags se resolve (feature/scenario ke @Tags padhke)
ApiHost? host = ApiHostResolver.ResolveFromTags(new[] { "@Auth", "@SuperAdmin" });
// → ApiHost.Auth (sirf Auth/Api aliases match karta hai, baaki ignore)
```

**Conflict protection:**
```csharp
// Dono tags lage ho to throw
@Auth @Api   // ❌ InvalidOperationException — conflicting tags
@Auth @SuperAdmin  // ✅ OK — SuperAdmin Auth/Api nahi hai, ignore hoga
```

---

## 2.5 — FeatureBaseUrlResolver (feature name → ApiHost)

📄 `Core/Clients/FeatureBaseUrlResolver.cs`

Kuch steps mein feature ka naam pass karte ho:
```gherkin
When User sends GET request for feature "User API Testing"
```

`FeatureBaseUrlResolver` yeh kaam karta hai:

```csharp
public static ApiHost Resolve(string featureOrBaseUrlName)
{
    // Step 1: appsettings.json ke FeatureBaseUrlMap mein dhoondo
    if (AppConfiguration.FeatureBaseUrlMap.TryGetValue(name, out var mappedTypeKey))
        return ApiHostResolver.ResolveFromKey(mappedTypeKey);  // "Api" → ApiHost.Api

    // Step 2: Direct alias hai kya? ("Auth" / "Api")
    if (ApiHostResolver.TryResolveFromKey(name, out var host))
        return host;

    // Step 3: Kuch nahi mila → throw
    throw new InvalidOperationException(...);
}
```

`appsettings.json` mein map:
```json
"FeatureBaseUrlMap": {
  "User API Testing": "Api",
  "Login":            "Auth",
  "Login and Logout": "Auth",
  "TokenRefresh":     "Api"
}
```

Matlab `"User API Testing"` → map se `"Api"` → `ApiHostResolver` → `ApiHost.Api`.

---

## 2.6 — ApiHostHooks (har scenario se pehle host auto-set)

📄 `Hooks/ApiHostHooks.cs`

```csharp
[BeforeScenario(Order = 1)]
public void ApplyBaseUrlTypeFromFeatureTags()
{
    var scenarioTags = _scenarioContext.ScenarioInfo.Tags;
    var featureTags  = _featureContext.FeatureInfo.Tags;

    // pehle scenario tags, phir feature tags
    var host =
        ApiHostResolver.ResolveFromTags(scenarioTags)
        ?? ApiHostResolver.ResolveFromTags(featureTags);

    if (host.HasValue)
        ApiHostContext.Set(host.Value);    // → AsyncLocal mein store
}

[AfterScenario(Order = 9999)]
public void ClearBaseUrlType() => ApiHostContext.Clear();  // clean up
```

🧠 **Matlab:** Agar tumne scenario ya feature pe `@Auth` ya `@Api` tag lagaya hai,
toh **step mein base URL manually set karne ki zaroorat nahi** — hook automatically
kar deta hai `BeforeScenario` mein.

---

## 2.7 — ApiHostStepHelper (steps ke liye shortcut)

📄 `StepDefinitions/ApiHostStepHelper.cs`

```csharp
internal static class ApiHostStepHelper
{
    // Step mein string aata hai → resolve → set → return
    public static ApiHost ApplyBaseUrlType(string baseUrlType)
    {
        var host = ApiHostResolver.ResolveFromKey(baseUrlType);
        ApiHostHooks.SetHost(host);       // ApiHostContext.Set
        return host;
    }

    // Feature name se set karo
    public static ApiHost ApplyFeatureName(string featureName)
    {
        var host = FeatureBaseUrlResolver.Resolve(featureName);
        ApiHostHooks.SetHost(host);
        return host;
    }
}
```

Steps isko call karte hain:
```csharp
[When(@"User sends GET request for feature ""(.*)""")]
public async Task GetRequestForFeature(string featureName)
{
    ApiHostStepHelper.ApplyFeatureName(featureName);  // host set
    SaveResponse(await _driver.GetAsync());           // correct server pe call
}
```

---

## 2.8 — Poora flow ek saath (visual)

```
Feature file tag @Auth  ──→  ApiHostHooks.BeforeScenario
                                  └→ ApiHostResolver.ResolveFromTags(["Auth"])
                                  └→ ApiHostContext.Set(ApiHost.Auth)

     ── YA ──

Step: "Auth" base url  ──→  ApiHostStepHelper.ApplyBaseUrlType("Auth")
                                  └→ ApiHostResolver.ResolveFromKey("Auth")
                                  └→ ApiHostContext.Set(ApiHost.Auth)

     ── YA ──

Step: feature "User API Testing"  ──→  ApiHostStepHelper.ApplyFeatureName(...)
                                           └→ FeatureBaseUrlResolver.Resolve(...)
                                           └→ appsettings FeatureBaseUrlMap lookup
                                           └→ ApiHostContext.Set(ApiHost.Api)

                        ↓ (host set ho gaya)

ApiClient.GetAsync(endpoint, host: ApiHostContext.CurrentOrDefault)
     └→ RestClientFactory.GetClient(ApiHost.Api)  ← cached client milta hai
     └→ RestSharp → HTTP call → response
```

---

## ✅ Part 2 Summary

| Class | Role |
|-------|------|
| `ApiHost` | Enum — `Auth` ya `Api`, bas do choices |
| `RestClientFactory` | `RestClient` banata + `ConcurrentDictionary` mein cache karta |
| `ApiHostContext` | `AsyncLocal` — current scenario ka active host store |
| `ApiHostResolver` | String alias → `ApiHost` convert karta (`"B2C"` → `Auth`) |
| `FeatureBaseUrlResolver` | Feature name → appsettings map → `ApiHost` |
| `ApiHostHooks` | `BeforeScenario` mein tags se host auto-set, `AfterScenario` mein clear |
| `ApiHostStepHelper` | Steps ke liye one-liner helper — resolve + set ek saath |

**Key takeaway:** Host ka decision **teen jagah** ho sakta hai —
(1) feature/scenario tags se auto, (2) step mein string se, (3) feature naam se.
Teeno ultimately `ApiHostContext.Set()` tak pohonchte hain, aur `RestClientFactory`
wahan se sahi client deta hai.

➡️ **Next:** [Part 3 — Authentication](README.md) — token kaise milta hai, kahan rehta
hai, aur khud-ba-khud request mein kaise lagta hai.
