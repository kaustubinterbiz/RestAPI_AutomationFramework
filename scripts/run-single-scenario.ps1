# Runs a single security scenario and generates one consolidated report for that run.
param(
    [Parameter(Mandatory = $true)]
    [string]$ScenarioName,

    [switch]$OpenIndex
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $ProjectRoot

$filter = "FullyQualifiedName~$ScenarioName"
$env:SECURITY_TEST_FILTER = $filter

Write-Host "Building solution..." -ForegroundColor Cyan
dotnet build EnterpriseApiSecurityAutomationFramework.csproj --verbosity quiet
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$resultsDir = Join-Path $ProjectRoot "TestResults"
if (-not (Test-Path $resultsDir)) {
    New-Item -ItemType Directory -Path $resultsDir | Out-Null
}

Write-Host "Running single scenario: $ScenarioName" -ForegroundColor Cyan
dotnet test EnterpriseApiSecurityAutomationFramework.csproj `
    --no-build `
    --filter $filter `
    --logger "trx;LogFileName=security-results.trx"

$exitCode = $LASTEXITCODE

Write-Host ""
Write-Host "Consolidated security report generated for this single-scenario run." -ForegroundColor Green

$reportDir = Join-Path $ProjectRoot "Reports\Security\LivingReport"
$manifestPath = Join-Path $ProjectRoot "Reports\Security\runs\manifest.json"
$timestampedHtml = $null

if (Test-Path $manifestPath) {
    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
    if ($manifest -and $manifest.Count -gt 0) {
        $latest = $manifest[0]
        $timestampedHtml = Join-Path $reportDir $latest.htmlFile
        Write-Host "Timestamped HTML: $timestampedHtml" -ForegroundColor Yellow
        Write-Host "Run ID          : $($latest.runId)" -ForegroundColor Yellow
        Write-Host "Run Mode        : $($latest.runMode)" -ForegroundColor Yellow
    }
}

$openTarget = if ($OpenIndex) {
    Join-Path $reportDir "index.html"
} else {
    $timestampedHtml
}

if ($openTarget -and (Test-Path $openTarget)) {
    Write-Host "Opening report..." -ForegroundColor Cyan
    Start-Process $openTarget
}

Remove-Item Env:SECURITY_TEST_FILTER -ErrorAction SilentlyContinue
exit $exitCode
