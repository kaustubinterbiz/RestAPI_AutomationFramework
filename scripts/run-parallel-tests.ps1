# True parallel execution (out-of-process). Does not affect normal dotnet test.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Write-Host "Starting parallel test runner..." -ForegroundColor Cyan
dotnet run --project "$root\ParallelTestRunner\ParallelTestRunner.csproj" --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Host "Parallel run failed with exit code $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "Parallel run completed. Open Reports/Parallel/Consolidated/ for the parallel dashboard." -ForegroundColor Green

$manifestPath = Join-Path $root "Reports\Security\runs\manifest.json"
$reportDir = Join-Path $root "Reports\Security\LivingReport"

if (Test-Path $manifestPath) {
    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
    $parallelRun = $manifest | Where-Object { $_.runMode -eq "ParallelBatch" } | Select-Object -First 1
    if ($parallelRun) {
        $securityHtml = Join-Path $reportDir $parallelRun.htmlFile
        Write-Host "Security consolidated report: $securityHtml" -ForegroundColor Yellow
        Write-Host "Run ID                      : $($parallelRun.runId)" -ForegroundColor Yellow
    }
}
