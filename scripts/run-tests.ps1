# Run Reqnroll/NUnit tests (works with dotnet test classic syntax on .NET 8)
param(
    [string]$Filter = "",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "EnterpriseApiAutomationFramework.csproj"

Write-Host "Building test project ($Configuration)..." -ForegroundColor Cyan
dotnet build $project --configuration $Configuration
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$testArgs = @(
    "test",
    $project,
    "--configuration", $Configuration,
    "--no-build",
    "--logger", "console;verbosity=normal"
)

if ($Filter) {
    $testArgs += @("--filter", $Filter)
}

Write-Host "Running tests..." -ForegroundColor Cyan
dotnet @testArgs
exit $LASTEXITCODE
