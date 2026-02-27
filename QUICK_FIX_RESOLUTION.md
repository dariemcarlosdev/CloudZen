# Quick Fix Resolution Guide

This document contains common issues encountered in the CloudZen project and their resolutions.

---

## Issue #1: CORS Error - Blazor Contact Form Cannot Call Azure Function

### Quick Description
The Blazor WebAssembly contact form fails to send emails with the following browser console error:
```
Access to fetch at 'http://localhost:7257/api/send-email' from origin 'https://localhost:44370' 
has been blocked by CORS policy: Response to preflight request doesn't pass access control check: 
No 'Access-Control-Allow-Origin' header is present on the requested resource.
```

### Why This Issue Happens
1. **Azure Functions Isolated Worker Model**: The project uses `dotnet-isolated` runtime (`"FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"` in `local.settings.json`), which is the **isolated worker model**.

2. **host.json CORS Doesn't Work**: The CORS configuration in `host.json` only works for the **in-process model**, NOT for the isolated worker model:
   ```json
   // This does NOT work for dotnet-isolated!
   "extensions": {
       "http": {
           "cors": {
               "allowedOrigins": ["https://localhost:44370"]
           }
       }
   }
   ```

3. **ASP.NET Core CORS Middleware Incompatibility**: The standard `app.UseCors()` middleware cannot be used because `FunctionsApplication.CreateBuilder()` returns an `IHost`, not a `WebApplication`:
   ```csharp
   // This causes CS1061 error!
   var app = builder.Build();
   app.UseCors(); // IHost does not contain 'UseCors'
   ```

4. **Preflight Requests Not Handled**: Browsers send an OPTIONS preflight request before the actual POST request. Without handling this, the request fails.

### Resolution

**Step 1: Create CORS Settings Class** (`Api/Security/InputValidator.cs`)
```csharp
/// <summary>
/// CORS settings for isolated worker model functions.
/// </summary>
public record CorsSettings(string[] AllowedOrigins)
{
    public bool IsOriginAllowed(string? origin)
    {
        if (string.IsNullOrEmpty(origin)) return false;
        return AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
    }
}
```

**Step 2: Add CORS Extension Methods** (`Api/Security/InputValidator.cs`)
```csharp
public static class SecurityHeadersExtensions
{
    public static void AddCorsHeaders(this HttpResponse response, HttpRequest request, CorsSettings corsSettings)
    {
        var origin = request.Headers["Origin"].FirstOrDefault();
        
        if (!string.IsNullOrEmpty(origin) && corsSettings.IsOriginAllowed(origin))
        {
            response.Headers.TryAdd("Access-Control-Allow-Origin", origin);
            response.Headers.TryAdd("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.Headers.TryAdd("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With, X-Correlation-Id");
            response.Headers.TryAdd("Access-Control-Max-Age", "600");
        }
    }

    public static bool IsCorsPreflightRequest(this HttpRequest request)
    {
        return request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase) &&
               request.Headers.ContainsKey("Origin") &&
               request.Headers.ContainsKey("Access-Control-Request-Method");
    }
}
```

**Step 3: Register CORS Settings in Program.cs** (`Api/Program.cs`)
```csharp
using CloudZen.Api.Security;

// Configure allowed origins
string[] allowedOrigins = new[]
{
    "https://localhost:5001",
    "https://localhost:44370",  // Visual Studio IIS Express
    "http://localhost:7257"
};

builder.Services.AddSingleton(new CorsSettings(allowedOrigins));
```

**Step 4: Update Function to Handle CORS** (`Api/Functions/SendEmailFunction.cs`)
```csharp
public class SendEmailFunction
{
    private readonly CorsSettings _corsSettings;

    public SendEmailFunction(..., CorsSettings corsSettings)
    {
        _corsSettings = corsSettings;
    }

    [Function("SendEmail")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options", Route = "send-email")] HttpRequest req)
    {
        // Add CORS headers to ALL responses
        req.HttpContext.Response.AddCorsHeaders(req, _corsSettings);

        // Handle preflight requests
        if (req.IsCorsPreflightRequest())
        {
            return new StatusCodeResult(StatusCodes.Status204NoContent);
        }

        // ... rest of the function
    }
}
```

**Step 5: Configure Blazor App API URL** (`wwwroot/appsettings.Development.json`)
```json
{
  "ApiBaseUrl": "http://localhost:7257/api"
}
```

---

## Issue #2: TimeSpan Configuration Error in host.json

### Quick Description
Azure Function fails to start with error:
```
Failed to convert configuration value at 'AzureFunctionsJobHost:extensions:http:hsts:MaxAge' 
to type 'System.TimeSpan'. The TimeSpan string '31536000' could not be parsed.
```

### Why This Issue Happens
The `maxAge` property in `host.json` expects a **TimeSpan format**, not raw seconds:
```json
// WRONG - raw seconds
"maxAge": "31536000"

// CORRECT - TimeSpan format (days.hours:minutes:seconds)
"maxAge": "365.00:00:00"
```

### Resolution
Update `Api/host.json`:
```json
{
    "extensions": {
        "http": {
            "hsts": {
                "isEnabled": true,
                "maxAge": "365.00:00:00",  // 365 days in TimeSpan format
                "includeSubDomains": true,
                "preload": true
            }
        }
    }
}
```

**TimeSpan Format Reference:**
| Value | Format | Meaning |
|-------|--------|---------|
| 1 hour | `01:00:00` | hours:minutes:seconds |
| 1 day | `1.00:00:00` | days.hours:minutes:seconds |
| 365 days | `365.00:00:00` | 365 days |

---

## Issue #3: ECONNREFUSED - Cannot Connect to Azure Function

### Quick Description
Postman or browser shows:
```
Error: connect ECONNREFUSED 127.0.0.1:7071
```

### Why This Issue Happens
The Azure Function is not running. Common causes:
1. Forgot to start the function
2. Azure Functions Core Tools not installed
3. Another process using the port
4. Build errors preventing startup

### Resolution

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

---

## Issue #4: File Locked by .NET Host

### Quick Description
Build fails with:
```
MSB3026: Could not copy "CloudZen.Api.dll" to "bin\Debug\net8.0\CloudZen.Api.dll". 
The file is locked by: ".NET Host (34152)"
```

### Why This Issue Happens
Another instance of the Azure Function is running in the background, holding a lock on the DLL file.

### Resolution
Kill the process and rebuild:
```powershell
# Find and kill the process
Get-Process | Where-Object { $_.ProcessName -like "*dotnet*" -or $_.ProcessName -like "*func*" } | Stop-Process -Force

# Rebuild
cd Api
dotnet build
```

---

## Issue #5: Brevo API Key Not Configured

### Quick Description
Email sending fails with 500 error:
```json
{
    "error": "Email service is not configured properly."
}
```

### Why This Issue Happens
The `BREVO_API_KEY` environment variable is missing or empty in `local.settings.json`.

### Resolution
Update `Api/local.settings.json`:
```json
{
    "Values": {
        "BREVO_API_KEY": "xkeysib-your-actual-api-key-here"
    }
}
```

⚠️ **Security Note:** Never commit real API keys to source control. Ensure `local.settings.json` is in `.gitignore`.

---

## Issue #6: Blazor App Not Loading Development Configuration

### Quick Description
The Blazor app uses `/api` instead of `http://localhost:7257/api` for the API URL.

### Why This Issue Happens
1. `wwwroot/appsettings.Development.json` doesn't exist
2. The environment is not set to "Development"
3. Configuration file not being loaded

### Resolution

**Step 1: Create development config** (`wwwroot/appsettings.Development.json`)
```json
{
  "ApiBaseUrl": "http://localhost:7257/api"
}
```

**Step 2: Ensure file is copied to output**
The file should be in the `wwwroot` folder and will be automatically served.

**Step 3: Verify in browser**
Open browser DevTools → Network tab → Check the API request URL.

---

## Issue #7: Rate Limit Exceeded (429 Error)

### Quick Description
API returns:
```json
{
    "error": "Rate limit exceeded. Try again in 60 seconds."
}
```

### Why This Issue Happens
The rate limiter restricts requests to 10 per 60 seconds per client IP (default configuration).

### Resolution

**Option 1: Wait for the window to reset** (60 seconds)

**Option 2: Restart the Azure Function** (clears in-memory rate limiter)

**Option 3: Adjust rate limit settings** (`Api/local.settings.json`)
```json
{
    "Values": {
        "RateLimiting:PermitLimit": "100",
        "RateLimiting:WindowSeconds": "60"
    }
}
```

---

## Issue #8: Azurite Storage Emulator Not Running

### Quick Description
Azure Function fails with storage-related errors or connection refused to `127.0.0.1:10000`.

### Why This Issue Happens
The project uses `"AzureWebJobsStorage": "UseDevelopmentStorage=true"` which requires Azurite to be running.

### Resolution

**Option 1: Start via Visual Studio**
Visual Studio automatically starts Azurite when debugging Azure Functions projects.

**Option 2: Start manually**
```bash
# Install if needed
npm install -g azurite

# Start
azurite --silent --location c:\azurite
```

**Option 3: Use VS Code extension**
Install the "Azurite" extension and start from the command palette.

---

## Quick Reference: Development URLs

| Component | Default URL |
|-----------|-------------|
| Blazor App (IIS Express) | `https://localhost:44370` |
| Blazor App (Kestrel) | `https://localhost:5001` |
| Azure Function | `http://localhost:7071` or `http://localhost:7257` |
| Azurite Blob | `http://127.0.0.1:10000` |
| Azurite Queue | `http://127.0.0.1:10001` |
| Azurite Table | `http://127.0.0.1:10002` |

---

## Quick Reference: Key Files

| Purpose | File Path |
|---------|-----------|
| Azure Function Config | `Api/local.settings.json` |
| Azure Function Host Config | `Api/host.json` |
| Blazor Dev Config | `wwwroot/appsettings.Development.json` |
| Blazor Prod Config | `wwwroot/appsettings.json` |
| CORS Settings | `Api/Security/InputValidator.cs` |
| Email Function | `Api/Functions/SendEmailFunction.cs` |
| Email Service (Blazor) | `Services/ApiEmailService.cs` |

---

*Last Updated: February 2026*
