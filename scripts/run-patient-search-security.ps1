# Run Patient Search API security scenarios (@PatientSearch tag)
$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)
dotnet test --filter "TestCategory=PatientSearch" --logger "console;verbosity=normal"
