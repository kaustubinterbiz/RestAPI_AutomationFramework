# 📚 Framework Guide — RestAPI Automation Framework

Yeh guide poore framework ko **7 parts** mein todti hai. Har part ek layer hai, aur
har layer agle ko feed karti hai. Neeche se upar padho — sab jud jayega.

> **Tech stack:** C# (.NET 8) · RestSharp · Reqnroll (SpecFlow) · FluentAssertions · ExtentReports

---

## 🗺️ Parts Index

| Part | Layer | File | Kya seekhoge |
|------|-------|------|--------------|
| 1 | Configuration | [Part1-Configuration-Layer.md](Part1-Configuration-Layer.md) | JSON read/write, `ConfigReaderNew`, `AppConfiguration` |
| 2 | Hosts & Clients | [Part2-ApiHost-RestClientFactory.md](Part2-ApiHost-RestClientFactory.md) | `ApiHost`, `RestClientFactory`, `ApiHostContext`, `ApiHostResolver` |
| 3 | Authentication | _(aane wala)_ | `AuthService`, `TokenManager`, token lifecycle |
| 4 | Endpoint Resolution | _(aane wala)_ | `EndpointHelper`, `{placeholder}` replace |
| 5 | Request & Send | _(aane wala)_ | `RequestBuilder`, `ApiClient`, flexible requests |
| 6 | Driver Layer | _(aane wala)_ | `UserDriver` — test actions facade |
| 7 | Feature/Steps/Hooks/Validator | _(aane wala)_ | Gherkin → C# binding, assertions |

---

## 🔗 End-to-end flow (ek line mein)

```
Feature step → ApiHostStepHelper (host set) → UserDriver (action)
   → ApiClient (verb) → RequestBuilder (request + token) → EndpointHelper (placeholder)
   → RestClientFactory (client) → RestSharp (HTTP) → response
   → TokenContext (save) → ResponseValidator (assert)
```
