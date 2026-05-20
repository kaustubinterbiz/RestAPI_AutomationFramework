# True parallel execution (out-of-process). Does not affect normal dotnet test.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Write-Host "Starting parallel test runner..." -ForegroundColor Cyan
dotnet run --project "$root\ParallelTestRunner\ParallelTestRunner.csproj" --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Host "Parallel run failed with exit code $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "Parallel run completed. Open Reports/Parallel/Consolidated/ for the merged report." -ForegroundColor Green
