# 📦 PART 1 — Configuration Layer (Framework ki neev)

> **Goal:** Samajhna ki framework apni saari values (URLs, credentials, endpoints,
> tokens, test data) kahan se padhta aur kahan likhta hai.

Yeh layer sabse pehle aati hai kyunki **baaki saari layers isi se data maangti hain** —
login credentials, base URLs, endpoint paths, cached IDs, token — sab kuch JSON files
mein hai, aur unhe padhne ka kaam yeh layer karti hai.

---

## 1.1 — JSON files ka role (data kahan rehta hai)

| File | Kya rakhti hai | Example key |
|------|----------------|-------------|
| `appsettings.json` | Base URLs, environment, auth mode, file paths, cached IDs, token | `ApiUrls`, `access_token`, `CacheId` |
| `appsettings.QA.json` / `appsettings.Staging.json` | Environment-specific overrides | per-env URLs |
| `TestData/Login/LoginRequest.json` | Login credentials, role-wise | `SuperAdmin`, `HospitalRole` |
| `TestData/Request Endpoint/RequestEndPoint.json` | API endpoint paths (placeholders ke saath) | `getExistingUser` |
| `TestData/Request Endpoint/RequestBody.json` | POST/PUT request bodies | `productCreateBody` |

### `appsettings.json` ka anatomy (zaroori keys)
```jsonc
{
  "Environment": "QA",
  "ApiUrls": {
    "AuthBaseUrl": "https://testRovicare.b2clogin.com/",   // login server
    "ApiBaseUrl":  "https://as-rc-test-api.azurewebsites.net/" // app server
  },
  "Authentication": {
    "Mode": "Shared",            // Shared ya PerScenario
    "PersistAccessToken": true,  // token wapas file mein likhna hai?
    "UseCachedAccessToken": true
  },
  "EndpointJson": "TestData/Request Endpoint/RequestEndPoint.json", // endpoints file ka path
  "LoginJson":    "TestData/Login/LoginRequest.json",              // creds file ka path
  "LoginRoleKey": "HospitalRole",   // default role
  "access_token": "eyJhbGc...",     // last login ka token (framework khud likhta hai)
  "CacheId": "2c528cd8-...",        // last response se nikla cached id
  "EmailId": "hospital@rovicare.com"
}
```

🧠 **Insight:** `appsettings.json` ke andar **paths bhi keys hain** — `EndpointJson`,
`LoginJson`. Matlab framework pehle `appsettings.json` padhta hai, usme se doosri file
ka path nikaalta hai, fir woh file padhta hai. (Indirection — yeh aage Part 4/5 mein
dikhega.)

---

## 1.2 — `ConfigReaderNew` (main hero of this layer)

📄 `Core/Configurations/ConfigReaderNew.cs`

Yeh ek **static, thread-safe, file-based** config loader hai. Sabki nazar ismein hi
rehti hai. Iska design rule:

> **Ek time pe ek file "active" rehti hai.** Jab tum nayi file `LoadConfig` karte ho,
> purani memory se dispose ho jaati hai.

### Sabse zyada use hone wale methods

#### (a) `LoadConfig(fileName)` — file ko active banao
```csharp
ConfigReaderNew.LoadConfig("appsettings.json");
```
- File ko memory mein load karta hai aur "default" active config bana deta hai.
- Purani saari entries dispose kar deta hai (clean slate).

#### (b) `GetValue(key)` — active file se value
```csharp
string email = ConfigReaderNew.GetValue("EmailId");   // "hospital@rovicare.com"
string url   = ConfigReaderNew.GetValue("ApiUrls:ApiBaseUrl"); // nested → ':' use karo
```
- Nested key ke liye colon (`:`) — jaise `"Authentication:Mode"`.
- Agar key nahi mili to **empty string** deta hai (null nahi).

#### (c) `GetJsonBody(file, key)` — poora nested block string mein
```csharp
// LoginRequest.json mein "SuperAdmin" ek poora object hai
string creds = ConfigReaderNew.GetJsonBody("TestData/Login/LoginRequest.json", "SuperAdmin");
// creds = {"grant_type":"password","client_id":"...","username":"admin@...","password":"..."}
```
🧠 Yeh `GetValue` se alag hai — `GetValue` flat string deta hai, `GetJsonBody` **poora
JSON sub-tree** deta hai. Login body banane ke liye yahi use hota hai.

#### (d) `UpdateJsonValue(file, key, value)` — file mein WAPAS likhna
```csharp
ConfigReaderNew.UpdateJsonValue("appsettings.json", "access_token", newToken);
```
- Login ke baad token isi se `appsettings.json` mein persist hota hai.

#### (e) `GetSection<T>(name)` — strongly-typed binding
```csharp
var urls = ConfigReaderNew.GetSection<ApiEnvironmentSettings>("ApiUrls");
// urls.AuthBaseUrl, urls.ApiBaseUrl
```

#### Aur bhi (reference)
| Method | Kaam |
|--------|------|
| `ReadJson<T>(file)` | File ko `T` object mein deserialize (disk se, cache nahi) |
| `ReadJson(file)` | Mutable `JsonNode` tree |
| `WriteJson<T>(file, data)` | Object ko JSON file mein likho |
| `UpdateJsonSection(file, section, dict)` | Nested section update/create (e.g. `SessionInfo`) |
| `GetJsonValue(file, key)` | File se directly top-level value (cache bypass) |
| `GetJsonSectionValue(file, section, prop)` | Nested section se ek value |

---

## 1.3 — Path resolution ka jaadu (kaha se file dhoondta hai)

Yeh ek important detail hai. Jab tum `"appsettings.json"` doge (relative path),
`ConfigReaderNew` is order mein dhoondta hai:

1. **`.csproj` ke paas** (source tree) — `FindFileNearProjectRoot`
2. **`bin/Debug/...`** (output folder) — `AppContext.BaseDirectory`
3. **Current working directory**

🧠 **Kyun?** Taaki jab framework token wapas `appsettings.json` mein likhe, woh
**source tree** wali file update ho (jo tum IDE mein dekhte ho) — sirf `bin` wali copy
nahi. Isiliye source dir ko prefer karta hai.

---

## 1.4 — `AppConfiguration` (layered, read-only config)

📄 `Core/Configurations/AppConfiguration.cs`

`ConfigReaderNew` "ek file at a time" model hai. `AppConfiguration` iske ulat —
**layered + cached** model deta hai, jo startup pe ek baar build hota hai:

```
appsettings.json
  + appsettings.{Environment}.json   (optional override)
  + Environment variables (prefix "ROVI_")
```

Environment ka naam yahan se aata hai (priority order):
1. `TEST_ENVIRONMENT` environment variable
2. `appsettings.json` ka `"Environment"` key
3. default `"QA"`

### Key members
```csharp
AppConfiguration.ApiUrls;          // → ApiEnvironmentSettings (AuthBaseUrl, ApiBaseUrl, Timeout)
AppConfiguration.EnvironmentName;  // "QA" / "Staging"
AppConfiguration.Authentication;   // → AuthenticationSettings (Mode, PersistAccessToken...)
AppConfiguration.FeatureBaseUrlMap;// feature name → host map
AppConfiguration.GetValue("BaseUrl");
AppConfiguration.GetBool("UseCachedAccessToken", true);
AppConfiguration.Reload();         // cache clear karke dobara build
```

### `ApiEnvironmentSettings` (bound object)
📄 `Core/Configurations/ApiEnvironmentSettings.cs`
```csharp
public sealed class ApiEnvironmentSettings
{
    public string AuthBaseUrl { get; set; } = "";
    public string ApiBaseUrl  { get; set; } = "";
    public int TimeoutMilliseconds { get; set; } = 30_000;
}
```
Yeh `appsettings.json` ke `"ApiUrls"` + `"Timeout"` se bind hota hai. Part 2 mein
`RestClientFactory` isi ko use karke clients banata hai.

---

## 1.5 — Do config classes kyun? (`ConfigReaderNew` vs `AppConfiguration`)

| | `ConfigReaderNew` | `AppConfiguration` |
|--|-------------------|--------------------|
| Model | Ek file at a time (swap-able) | Layered (merged), cached |
| Read | ✅ | ✅ |
| Write | ✅ (`UpdateJsonValue`, `WriteJson`) | ❌ (read-only) |
| Env overrides | ❌ | ✅ (`appsettings.{env}.json`, env vars) |
| Use case | Test data / creds / endpoints / token persist | Base URLs, auth mode, framework settings |

🧠 **Rule of thumb:**
- **Dynamic test data ya likhna hai** → `ConfigReaderNew`
- **Framework-level settings (URLs, mode) chahiye** → `AppConfiguration`

---

## 1.6 — Mini walkthrough: "SuperAdmin ke credentials kaise milte hain?"

```csharp
// 1. appsettings.json active karo
ConfigReaderNew.LoadConfig("appsettings.json");

// 2. usme se creds-file ka PATH nikaalo
string loginFile = ConfigReaderNew.GetValue("LoginJson");
// → "TestData/Login/LoginRequest.json"

// 3. ab us file se "SuperAdmin" ka poora block lo
string credsJson = ConfigReaderNew.GetJsonBody(loginFile, "SuperAdmin");
// → {"grant_type":"password","client_id":"...","username":"admin@rovicare.com","password":"..."}

// 4. ab isse LoginRequest object banega (yeh Part 3 mein)
```

Bas yahi indirection pattern poore framework mein baar-baar dikhega:
**appsettings → path key → doosri file → actual value.**

---

## ✅ Part 1 Summary

- **JSON files** = saara data (URLs, creds, endpoints, bodies, token, cached IDs).
- **`ConfigReaderNew`** = ek-file-at-a-time read/write engine. `LoadConfig` →
  `GetValue` / `GetJsonBody` / `UpdateJsonValue` core methods hain.
- **`AppConfiguration`** = layered + cached + env-aware, read-only framework settings.
- **`ApiEnvironmentSettings`** = base URLs ka typed model.
- **Indirection pattern**: `appsettings.json` mein doosri files ke paths hote hain.

➡️ **Next:** [Part 2 — ApiHost & RestClientFactory](README.md) — "kaunsa base URL,
kaunsa client".
