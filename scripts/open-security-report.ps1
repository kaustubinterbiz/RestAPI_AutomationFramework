# Opens the security living report in your default browser.
param(
    [switch]$Latest
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$reportDir = Join-Path $ProjectRoot "Reports\Security\LivingReport"
$indexPath = Join-Path $reportDir "index.html"
$latestPath = Join-Path $reportDir "latest.html"

$report = $null

if ($Latest -and (Test-Path $latestPath)) {
    $report = $latestPath
}
elseif (-not $Latest -and (Test-Path $indexPath)) {
    $report = $indexPath
}
else {
    $manifestPath = Join-Path $ProjectRoot "Reports\Security\runs\manifest.json"
    if (Test-Path $manifestPath) {
        $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
        if ($manifest -and $manifest.Count -gt 0) {
            $candidate = Join-Path $reportDir $manifest[0].htmlFile
            if (Test-Path $candidate) {
                $report = $candidate
            }
        }
    }
}

if (-not $report) {
    $report = Get-ChildItem -Path $reportDir -Filter "*.html" -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -ne "index.html" } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1 -ExpandProperty FullName
}

if (-not $report) {
    Write-Host "No security report found. Run: .\scripts\run-security-report.ps1" -ForegroundColor Red
    exit 1
}

Write-Host "Opening: $report" -ForegroundColor Green
Start-Process $report
