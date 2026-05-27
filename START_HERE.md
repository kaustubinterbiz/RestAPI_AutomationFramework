# Start Here — Simple Guide (Naye bande ke liye)

Pehle yeh padho. Detail ke liye: [ARCHITECTURE_GUIDE_HINDI.md](ARCHITECTURE_GUIDE_HINDI.md)

---

## Sirf 3 cheezein yaad rakho

```
Feature file (.feature)
    ↓
StepDefinitions (UserSteps.cs)
    ↓
UserDriver → ApiAuth (token) → ApiClient (HTTP)
```

| File | Kaam |
|------|------|
| `Features/Login.feature` | Test likho (Gherkin) |
| `StepDefinitions/UserSteps.cs` | Feature steps ka code |
| `Drivers/UserDriver.cs` | Login, GET, POST... |
| `Core/Authentication/ApiAuth.cs` | **Token — sirf yahan se** |
| `appsettings.json` | URLs + saved `access_token` |
| `TestData/.../RequestEndPoint.json` | API paths (`get`, `post`, ...) |

---

## Token ka simple rule

Har API call se pehle token chahiye. Framework yeh order follow karta hai:

1. Scenario mein jo token save hua (login ke baad)
2. `appsettings.json` → `"access_token"`
3. Agar dono nahi → automatic login

**Aapko manually header nahi lagana.** `UserDriver.GetAsync()` khud `Authorization: Bearer ...` bhejta hai.

---

## Scenario 2 example (Login.feature)

```gherkin
When User sends POST request on "Auth" base url      # 1. Login
Then Status should be OK
And the access token is stored from the last login response   # 2. Token save
When User sends GET request for feature "User API Testing" with cached id  # 3. GET + token
Then Status code should be 200
```

Code flow:

1. `LoginAsync()` → B2C se token
2. `ApiAuth.SaveTokenFromLoginResponse()` → `appsettings.json` mein save
3. `GetAsync()` → `ApiAuth.EnsureReadyAsync()` → GET + Bearer header

---

## Naya GET test kaise likhein

**Feature file:**
```gherkin
@Api
Scenario: My API test
    When User sends POST request on "Auth" base url
    And the access token is stored from the last login response
    When User sends GET request for feature "User API Testing"
    Then Status code should be 200
```

**Endpoint change:** `TestData/Request Endpoint/RequestEndPoint.json` → `"get"` key update karo.

---

## Run commands

```powershell
dotnet build RestSharpAPI_Automation.sln
dotnet test EnterpriseApiAutomationFramework.csproj
dotnet test --filter "_2_VerifyGETAPIToGetCachedId"
```

Report: `Reports/Html/` folder.

---

## Agar build fail ho (file locked)

Visual Studio mein **Stop Debugging** (`Shift+F5`), phir:

```powershell
Get-Process testhost -ErrorAction SilentlyContinue | Stop-Process -Force
dotnet build RestSharpAPI_Automation.sln
```

---

## Confusion? Yeh mat padho abhi

- `SharedTokenProvider`, `TokenCacheStore` — advanced parallel runs ke liye
- `EnterpriseRestBuilder` — ab use nahi ho raha
- `README_PARALLEL_EXECUTION.md` — jab parallel chalana ho tab

**Roz ke kaam ke liye:** `UserDriver` + `ApiAuth` + `Login.feature` — bas.

---

## Quick debug checklist

| Problem | Check |
|---------|--------|
| GET mein token nahi | `appsettings.json` → `access_token` empty to nahi? Login step pehle chala? |
| 401 Unauthorized | Token expire — dubara login scenario chalao |
| Step undefined (IDE) | Cucumber extension install + `.vscode/settings.json` |
| Build MSB3027 | testhost band karo (upar wala command) |
