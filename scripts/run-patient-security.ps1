# Run all Patient P0 security scenarios (@Patient tag)
$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)
dotnet test --filter "TestCategory=Patient" --logger "console;verbosity=normal"
