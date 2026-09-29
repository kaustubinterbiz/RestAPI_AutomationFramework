# Run Patient security suite and open Living Report
$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)
dotnet test --filter "TestCategory=Patient" --logger "console;verbosity=normal"
& "$PSScriptRoot\open-security-report.ps1"
