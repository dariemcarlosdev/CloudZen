# Issue #3: ECONNREFUSED - Cannot Connect to Azure Function

## Quick Description
Postman or browser shows:
```
Error: connect ECONNREFUSED 127.0.0.1:7071
```

## Why This Issue Happens
The Azure Function is not running. Common causes:
1. Forgot to start the function
2. Azure Functions Core Tools not installed
3. Another process using the port
4. Build errors preventing startup

## Resolution

**Check 1: Is Azure Functions Core Tools installed?**
```powershell
func --version
```
If not installed:
```powershell
winget install Microsoft.Azure.FunctionsCoreTools
```

**Check 2: Start the function**
```powershell
cd Api
func start
```

**Check 3: Kill processes using the port**
```powershell
Get-Process -Name "func" -ErrorAction SilentlyContinue | Stop-Process -Force
```

**Check 4: Build first**
```powershell
cd Api
dotnet build
func start
```
