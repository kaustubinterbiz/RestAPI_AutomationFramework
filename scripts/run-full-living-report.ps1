# Runs ALL framework scenarios and generates the API Security Living Report.
# Every scenario is captured (Authentication + Authorization + Functional suites).
# Output: Reports/Security/LivingReport/security-living-report_{RunId}.html

param(
    [switch]$OpenIndex
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $ProjectRoot

Write-Host "Building solution..." -ForegroundColor Cyan
dotnet build EnterpriseApiSecurityAutomationFramework.csproj --verbosity quiet
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$resultsDir = Join-Path $ProjectRoot "TestResults"
if (-not (Test-Path $resultsDir)) {
    New-Item -ItemType Directory -Path $resultsDir | Out-Null
}

$env:SECURITY_TEST_FILTER = "FullSuite"

Write-Host "Running full framework test suite (all scenarios)..." -ForegroundColor Cyan
dotnet test EnterpriseApiSecurityAutomationFramework.csproj `
    --no-build `
    --logger "trx;LogFileName=full-suite-results.trx"

$exitCode = $LASTEXITCODE

Write-Host ""
Write-Host "Living report is generated automatically after the test run." -ForegroundColor Green

$reportDir = Join-Path $ProjectRoot "Reports\Security\LivingReport"
$manifestPath = Join-Path $ProjectRoot "Reports\Security\runs\manifest.json"
$timestampedHtml = $null

if (Test-Path $manifestPath) {
    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
    if ($manifest -and $manifest.Count -gt 0) {
        $latest = $manifest[0]
        $timestampedHtml = Join-Path $reportDir $latest.htmlFile
        Write-Host "Timestamped HTML (this run): $timestampedHtml" -ForegroundColor Yellow
        Write-Host "Run ID                     : $($latest.runId)" -ForegroundColor Yellow
        Write-Host "Run Mode                   : $($latest.runMode)" -ForegroundColor Yellow
        Write-Host "Scenarios                  : $($latest.scenarioCount)" -ForegroundColor Yellow
    }
}

Remove-Item Env:SECURITY_TEST_FILTER -ErrorAction SilentlyContinue

$latestShortcut = Join-Path $reportDir "latest.html"
$indexPath = Join-Path $reportDir "index.html"

if (Test-Path $latestShortcut) {
    Write-Host "Latest shortcut              : $latestShortcut" -ForegroundColor DarkYellow
}

if (Test-Path $indexPath) {
    Write-Host "Run history index            : $indexPath" -ForegroundColor DarkYellow
}

$openTarget = if ($OpenIndex -and (Test-Path $indexPath)) {
    $indexPath
} elseif ($timestampedHtml -and (Test-Path $timestampedHtml)) {
    $timestampedHtml
} elseif (Test-Path $latestShortcut) {
    $latestShortcut
} else {
    Get-ChildItem -Path $reportDir -Filter "security-living-report_*.html" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1 -ExpandProperty FullName
}

if ($openTarget) {
    Write-Host "Opening report in default browser..." -ForegroundColor Cyan
    Start-Process $openTarget
}

exit $exitCode
