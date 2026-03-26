# Issue #1: CORS Error - Blazor Contact Form Cannot Call Azure Function

## Quick Description
The Blazor WebAssembly contact form fails to send emails with the following browser console error:
```
Access to fetch at 'http://localhost:7257/api/send-email' from origin 'https://localhost:44370' 
has been blocked by CORS policy: Response to preflight request doesn't pass access control check: 
No 'Access-Control-Allow-Origin' header is present on the requested resource.
```

## Why This Issue Happens
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

## Resolution

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
