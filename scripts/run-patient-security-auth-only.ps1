# Run Patient authentication security scenarios only
$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)
dotnet test --filter "TestCategory=Patient&TestCategory=Authentication" --logger "console;verbosity=normal"
