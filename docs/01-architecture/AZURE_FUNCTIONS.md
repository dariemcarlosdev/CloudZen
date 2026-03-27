# Azure Functions Architecture

## Hosting Model

CloudZen uses the **Isolated Worker Model** for Azure Functions v4 on .NET 8.

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Hosting model | Isolated Worker | .NET 8 requirement (in-process limited to .NET 6) |
| Process model | Separate worker process | Better security isolation for API keys |
| HTTP integration | ASP.NET Core (`HttpRequest`/`IActionResult`) | Full access to `HttpContext`, headers, middleware patterns |
| DI pattern | Constructor injection | Clean, testable code with `ILogger`, `IConfiguration`, custom services |
| Entry point | `Program.cs` + `FunctionsApplication.CreateBuilder()` | Standard .NET 8 host pattern |

> In-process model is deprecated (end of support: November 2026). All new development should use Isolated Worker.

---

## Architecture

```
┌──────────────────────────────────────────────────────────────────────┐
│                        CloudZen Solution                             │
├────────────────────────┬─────────────────────────────────────────────┤
│  CloudZen.csproj       │  CloudZen.Api.csproj                       │
│  (Blazor WebAssembly)  │  (Azure Functions v4, Isolated Worker)     │
│                        │                                             │
│  .NET 8, browser       │  .NET 8, server-side                       │
│  ApiEmailService   ──HTTP──► SendEmailFunction → Brevo SMTP         │
│  ChatbotService    ──HTTP──► ChatFunction → Anthropic Claude         │
│  AppointmentService ─HTTP──► BookAppointmentFunction → n8n Webhook  │
└────────────────────────┴─────────────────────────────────────────────┘
```

### Process Architecture (Isolated Worker)

```
┌─────────────────────────────────────────────────┐
│            Azure Functions Host                  │
│  ┌──────────────┐    gRPC    ┌────────────────┐ │
│  │ Host Process  │◄─────────►│ Worker Process  │ │
│  │ (Runtime)     │           │ (Your .NET 8)   │ │
│  │ • Triggers    │           │ • Functions     │ │
│  │ • Scaling     │           │ • Custom DI     │ │
│  │ • Bindings    │           │ • Full control  │ │
│  └──────────────┘           └────────────────┘ │
└─────────────────────────────────────────────────┘
```

---

## Key Differences: Isolated vs In-Process

| Aspect | Isolated Worker ✅ | In-Process ⚠️ |
|--------|-------------------|---------------|
| .NET support | .NET 6–9+ | .NET 6 only |
| Process | Separate from host | Shared with host |
| Dependency control | Full | May conflict with host |
| DI | Constructor injection | Parameter injection |
| Namespace | `Microsoft.Azure.Functions.Worker` | `Microsoft.Azure.WebJobs` |
| Function attribute | `[Function("Name")]` | `[FunctionName("Name")]` |
| Entry point | `Program.cs` | `Startup.cs` (limited) |
| ASP.NET Core | Full integration | Limited |
| Cold start | Slightly slower (two processes) | Faster |
| Status | Active development | Deprecated |

---

## Program.cs Structure

```csharp
var builder = FunctionsApplication.CreateBuilder(args);

// Configuration: local.settings.json + env vars + Key Vault
builder.Configuration
    .AddJsonFile("local.settings.json", optional: true)
    .AddEnvironmentVariables();

// Key Vault (production)
var kvEndpoint = Environment.GetEnvironmentVariable("KEY_VAULT_ENDPOINT");
if (!string.IsNullOrEmpty(kvEndpoint))
    builder.Configuration.AddAzureKeyVault(new Uri(kvEndpoint), new DefaultAzureCredential());

// IOptions
builder.Services.AddOptions<RateLimitOptions>().BindConfiguration("RateLimiting");
builder.Services.AddOptions<EmailSettings>().BindConfiguration("EmailSettings");

// CORS
builder.Services.AddSingleton(new CorsSettings(allowedOrigins));

// Services
builder.Services.AddSingleton<IRateLimiterService, PollyRateLimiterService>();
builder.Services.AddHttpClient("SecureClient", c => {
    c.DefaultRequestHeaders.Add("User-Agent", "CloudZen-Api/1.0");
    c.Timeout = TimeSpan.FromSeconds(30);
});

// Telemetry + HTTP integration
builder.Services.AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();
builder.ConfigureFunctionsWebApplication();

builder.Build().Run();
```

---

## Function Implementation Pattern

```csharp
public class SendEmailFunction
{
    private readonly ILogger<SendEmailFunction> _logger;
    private readonly IConfiguration _config;
    private readonly IRateLimiterService _rateLimiter;
    private readonly CorsSettings _corsSettings;

    public SendEmailFunction(ILogger<SendEmailFunction> logger, IConfiguration config,
        IRateLimiterService rateLimiter, CorsSettings corsSettings)
    {
        _logger = logger;
        _config = config;
        _rateLimiter = rateLimiter;
        _corsSettings = corsSettings;
    }

    [Function("SendEmail")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options", Route = "send-email")]
        HttpRequest req)
    {
        // Full ASP.NET Core HttpContext access
        req.HttpContext.Response.AddCorsHeaders(req, _corsSettings);
        req.HttpContext.Response.AddSecurityHeaders();
        var clientIp = req.GetClientIpAddress();
    }
}
```

---

## Project File (csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <AzureFunctionsVersion>V4</AzureFunctionsVersion>
    <OutputType>Exe</OutputType>  <!-- Standalone executable (isolated) -->
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="Microsoft.Azure.Functions.Worker" />
    <PackageReference Include="Microsoft.Azure.Functions.Worker.Sdk" />
    <PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore" />
    <PackageReference Include="Microsoft.Azure.Functions.Worker.ApplicationInsights" />
    <PackageReference Include="Azure.Identity" />
    <PackageReference Include="Azure.Extensions.AspNetCore.Configuration.Secrets" />
  </ItemGroup>
</Project>
```

---

## Troubleshooting

| Error | Cause | Solution |
|-------|-------|----------|
| `CS0234: 'Azure' not found` | Corrupted build artifacts | `Remove-Item -Recurse Api\obj, Api\bin` then `dotnet restore && dotnet build` |
| Blazor project includes Api files | Default glob patterns | Add `<DefaultItemExcludes>$(DefaultItemExcludes);Api\**</DefaultItemExcludes>` to `CloudZen.csproj` |
| `Duplicate AssemblyCompanyAttribute` | Stale obj folders | Delete all `obj/bin` folders, restore, rebuild |
| Function returns 404 | Missing `host.json` or wrong route | Verify `Api/host.json` exists with `"routePrefix": "api"` |
| Cold start >10s | Isolated worker startup cost | Use Premium/Dedicated plan with Always On |

---

## Related Docs

- [API Endpoints](API_ENDPOINTS.md) — All 3 endpoint specs (routes, request/response, validation, error codes)
- [Configuration](CONFIGURATION.md) — IOptions binding, Key Vault integration, secrets strategy
- [Vertical Slice Architecture](VERTICAL_SLICE_ARCHITECTURE.md) — API folder structure (`Api/Features/`, `Api/Shared/`)
- [Azure Functions Proxy Pattern](../06-patterns/01_azure_functions_proxy_api.md) — Full proxy pattern with code examples

---

*Last Updated: March 2026*
