# Issue #4: File Locked by .NET Host

## Quick Description
Build fails with:
```
MSB3026: Could not copy "CloudZen.Api.dll" to "bin\Debug\net8.0\CloudZen.Api.dll". 
The file is locked by: ".NET Host (34152)"
```

## Why This Issue Happens
Another instance of the Azure Function is running in the background, holding a lock on the DLL file.

## Resolution
Kill the process and rebuild:
```powershell
# Find and kill the process
Get-Process | Where-Object { $_.ProcessName -like "*dotnet*" -or $_.ProcessName -like "*func*" } | Stop-Process -Force

# Rebuild
cd Api
dotnet build
```
