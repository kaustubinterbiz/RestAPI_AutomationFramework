# Project Structure & File Reference

This document lists the purpose of each folder and source file in **RestAPI_AutomationFramework**.

---

## Solution layout

| Path | Purpose |
|------|---------|
| `EnterpriseApiAutomationFramework.csproj` | Main test project (SpecFlow + NUnit + RestSharp + Extent) |
| `ParallelTestRunner/` | Standalone console host for parallel runs (avoids test-host DLL locks) |
| `RestSharpAPI_Automation.sln` | Solution containing both projects |
| `scripts/run-parallel-tests.ps1` | Runs `ParallelTestRunner` (parallel mode only) |
| `appsettings.json` | Environment, endpoints, reporting, parallel execution config |
| `specflow.json` | SpecFlow language/binding settings |

---

## Core — API & framework

| File | Purpose |
|------|---------|
| `Core/Authentication/AuthService.cs` | Login flow and token storage |
| `Core/Authentication/TokenManager.cs` | Shared bearer token access |
| `Core/Builders/RequestBuilder.cs` | Builds RestSharp requests with auth/headers/body |
| `Core/Builders/EnterpriseRestBuilder.cs` | Enterprise REST builder implementation |
| `Core/Clients/ApiClient.cs` | HTTP verbs + API call recording for Extent |
| `Core/Clients/RestClientFactory.cs` | RestClient instance factory |
| `Core/Configurations/ConfigReader.cs` | `appsettings.json` reader |
| `Core/Configurations/ConfigReaderNew.cs` | Multi-config JSON loader/updater |
| `Core/Extensions/ServiceCollectionExtensions.cs` | DI service registration |
| `Core/Helpers/EndpointHelper.cs` | URL segment resolution |
| `Core/Helpers/JsonHelper.cs` | JSON file read helper |
| `Core/Interfaces/IRestBuilder.cs` | REST builder contract |
| `Core/Services/ResponseVerificationService.cs` | Response verification helpers |
| `Core/Utilities/LoggerManager.cs` | Serilog file logging (`Reports/logs.txt`) |
| `Core/Validators/ResponseValidator.cs` | FluentAssertions-based response checks |

---

## Core — Sequential Extent reporting (unchanged workflow)

| File | Purpose |
|------|---------|
| `Core/Reporting/ExtentReportManager.cs` | Creates/flushes Extent Spark HTML report |
| `Core/Reporting/ReportExecutionContext.cs` | Per-step API call tracking (`AsyncLocal`) |
| `Core/Reporting/ApiCallRecord.cs` | Single API call model for reports |
| `Core/Reporting/ApiRequestPayloadHelper.cs` | Extracts request body for logging |
| `Core/Reporting/Templates/extent-report-custom.css` | Dashboard spacing/dark-mode styles |
| `Core/Reporting/Templates/extent-report-custom.js` | Theme toggle for Extent dashboard |
| `Hooks/ExtentReportHooks.cs` | SpecFlow hooks: run/feature/scenario/step logging |

**Output:** `Reports/Html/ApiTestReport_<timestamp>.html` after `dotnet test`

---

## Core — Parallel execution (isolated module)

| File | Purpose |
|------|---------|
| `Core/ParallelExecution/Configuration/ParallelExecutionSettings.cs` | Parallel config from `appsettings.json` |
| `Core/ParallelExecution/Discovery/ParallelExecutionDiscovery.cs` | Routes feature vs scenario discovery |
| `Core/ParallelExecution/Discovery/FeatureModuleDiscovery.cs` | Discovers `Features/*.feature` |
| `Core/ParallelExecution/Discovery/ScenarioModuleDiscovery.cs` | Discovers scenarios for max parallelism |
| `Core/ParallelExecution/Execution/ParallelOrchestrator.cs` | Build once → run workers → merge report |
| `Core/ParallelExecution/Execution/ParallelWorkScheduler.cs` | `Parallel.ForEachAsync` + statistics |
| `Core/ParallelExecution/Execution/ProcessIsolatedFeatureExecutor.cs` | One `dotnet test` process per unit |
| `Core/ParallelExecution/Models/*.cs` | Execution/result/chart DTOs |
| `Core/ParallelExecution/Reporting/ConsolidatedReportBuilder.cs` | JSON + HTML + table after all workers finish |
| `Core/ParallelExecution/Reporting/ParallelDashboardAnalyticsCollector.cs` | Pie/timeline dataset aggregation |
| `Core/ParallelExecution/Reporting/ParallelDashboardHtmlRenderer.cs` | Chart.js dashboard (parallel only) |
| `Core/ParallelExecution/Reporting/ExtentWorkerMetricsExtractor.cs` | Step counts from worker Extent HTML |
| `Core/ParallelExecution/Reporting/TrxResultParser.cs` | Parses NUnit TRX per worker |

**Output:** `Reports/Parallel/Consolidated/run_<timestamp>/` (JSON, HTML dashboard, chart data, table)

---

## Parallel entry points

| File | Purpose |
|------|---------|
| `ParallelExecution/ParallelOrchestratorTests.cs` | NUnit `[Explicit]` orchestrator test |
| `ParallelTestRunner/Program.cs` | Console entry (recommended for CI/local parallel runs) |

---

## BDD tests & data

| Path | Purpose |
|------|---------|
| `Features/Login.feature` | User API test scenarios |
| `Features/Login.feature.cs` | SpecFlow-generated test code |
| `StepDefinitions/UserSteps.cs` | Step bindings |
| `Drivers/UserDriver.cs` | API actions used by steps |
| `Models/Request/LoginRequest.cs` | Login request model |
| `Models/Response/LoginResponse.cs` | Login response model |
| `TestData/Login/LoginRequest.json` | Login credentials payload |
| `TestData/Request Endpoint/*.json` | Endpoints and request bodies |

---

## Reports (generated — not committed)

| Path | Purpose |
|------|---------|
| `Reports/Html/` | Sequential Extent HTML (`.gitkeep` only in repo) |
| `Reports/Parallel/Workers/` | Per-worker TRX, logs, Extent (gitignored) |
| `Reports/Parallel/Consolidated/` | Merged parallel dashboard (gitignored) |

---

## Documentation

| File | Purpose |
|------|---------|
| `README.md` | Quick start and run commands |
| `README_PARALLEL_EXECUTION.md` | Parallel architecture and configuration |
| `PROJECT_STRUCTURE.md` | This file reference |
| `README_ENTERPRISE_ENHANCEMENTS.md` | Enterprise enhancement notes |

---

## Run commands

```bash
# Sequential (default — Extent report)
dotnet test

# Parallel + consolidated dashboard (recommended)
.\scripts\run-parallel-tests.ps1
# or
dotnet run --project ParallelTestRunner\ParallelTestRunner.csproj
```
