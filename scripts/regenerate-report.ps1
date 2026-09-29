# Rebuilds HTML/MD from an existing run_*.json snapshot without re-running tests.
param(
    [Parameter(Mandatory = $true)]
    [string]$RunId
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $ProjectRoot

Write-Host "Building SecurityReportRegenerator..." -ForegroundColor Cyan
dotnet build SecurityReportRegenerator\SecurityReportRegenerator.csproj --verbosity quiet
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Regenerating report for RunId: $RunId" -ForegroundColor Cyan
dotnet run --project SecurityReportRegenerator\SecurityReportRegenerator.csproj --no-build -- --runId $RunId
exit $LASTEXITCODE
