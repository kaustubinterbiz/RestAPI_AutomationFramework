# Enterprise API Automation Framework

> **Naye ho? Pehle yeh padho:** [START_HERE.md](START_HERE.md) (simple Hindi/English guide)  
> **Detail architecture:** [ARCHITECTURE_GUIDE_HINDI.md](ARCHITECTURE_GUIDE_HINDI.md)

## Project structure

See **[PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md)** for a full file/folder reference.

```
RestAPI_AutomationFramework/
├── Core/
│   ├── Reporting/              ← Sequential Extent reports
│   └── ParallelExecution/      ← Parallel orchestration + consolidated dashboard
├── Hooks/ExtentReportHooks.cs
├── ParallelTestRunner/           ← Console host for parallel runs
├── ParallelExecution/            ← NUnit [Explicit] parallel entry
├── Reports/
│   ├── Html/                   ← Sequential Extent output
│   └── Parallel/               ← Parallel consolidated output
├── Features/
├── StepDefinitions/
├── scripts/run-parallel-tests.ps1
└── TestData/
```

After `dotnet test`, open the latest report from **`Reports/Html/`** (project root, not `bin/`).

For parallel execution with Chart.js dashboard, see **[README_PARALLEL_EXECUTION.md](README_PARALLEL_EXECUTION.md)**.

## Included Features

- RestSharp Framework
- SpecFlow BDD
- Extent HTML reporting (`Reports/Html`)
- Dependency Injection Ready
- Request Builder
- GET / POST / PUT / PATCH / DELETE
- Token Synchronization
- Dynamic Headers
- Query Parameters
- JSON Payload Support
- Response Validation
- Status Code Validation
- Response Time Validation
- JSON Path Validation
- Logging Framework
- Environment Handling
- Enterprise Folder Structure
- Reusable Drivers
- Parallel Execution Ready

## Run Commands

```bash
dotnet restore
dotnet build
dotnet test                                    # Sequential + Extent report
.\scripts\run-parallel-tests.ps1               # Parallel + consolidated dashboard
```