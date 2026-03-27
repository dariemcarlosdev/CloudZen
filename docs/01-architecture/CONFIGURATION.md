# Configuration Architecture

> **API keys and secrets live only in the Functions backend. The WASM client never holds secrets.**

---

## Overview

CloudZen has two distinct configuration environments:

| Aspect | Blazor WASM (Frontend) | Azure Functions (Backend) |
|--------|------------------------|---------------------------|
| **Runs where** | Browser (client-side) | Azure server (server-side) |
| **Config source** | `wwwroot/appsettings.*.json` | `local.settings.json` / Azure Portal / Key Vault |
| **Publicly visible** | ✅ Yes — anyone can read via DevTools | ❌ No — server-side only |
| **Secrets allowed** | ❌ Never | ✅ Yes |
| **Config changes** | Rebuild & redeploy app | Update Azure Portal, restart Function |

---

## Blazor WebAssembly Configuration

### Config Files

| File | Purpose | Loaded When |
|------|---------|-------------|
| `wwwroot/appsettings.json` | Base defaults | Always |
| `wwwroot/appsettings.Development.json` | Local dev overrides | `dotnet run` |
| `wwwroot/appsettings.Production.json` | Production overrides | Deployed to Azure |

Loading order: base → environment-specific (later overrides earlier).

### Example: `wwwroot/appsettings.json`

```json
{
  "EmailService": {
    "ApiBaseUrl": "/api",
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "SendEmailEndpoint": "send-email"
  },
  "BlobStorage": {
    "ResumeUrl": "https://cloudzenstorage.blob.core.windows.net/...",
    "ContainerName": "cloudzencontainer"
  }
}
```

### What's Safe / Unsafe in WASM Config

| ✅ Safe | ❌ Never |
|---------|----------|
| API endpoint paths (`/api`) | API keys |
| Timeouts, retry counts | Connection strings |
| Feature flags | Passwords or tokens |
| Read-only SAS URLs (limited expiry) | Private endpoints |

---

## Azure Functions Configuration

### Config Sources (priority order, last wins)

1. `Api/local.settings.json` — local development (gitignored)
2. Environment variables — Azure Portal → Configuration → Application settings
3. Azure Key Vault — via `KEY_VAULT_ENDPOINT` + `DefaultAzureCredential`

### Example: `Api/local.settings.json`

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AZURE_FUNCTIONS_ENVIRONMENT": "Development",
    "BREVO_SMTP_LOGIN": "<your-login>@smtp-brevo.com",
    "BREVO_SMTP_KEY": "xsmtpsib-<your-key>",
    "ANTHROPIC_API_KEY": "<your-key>",
    "N8N_WEBHOOK_URL": "<your-webhook-url>",
    "EmailSettings:FromEmail": "cloudzen.inc@gmail.com",
    "EmailSettings:CcEmail": "admin@example.com",
    "RateLimiting:PermitLimit": "10",
    "RateLimiting:WindowSeconds": "60"
  }
}
```

> ⚠️ `local.settings.json` must be in `.gitignore`. Never commit real credentials.

### Production Settings (Azure Portal)

| Setting | Required |
|---------|----------|
| `BREVO_SMTP_LOGIN` | ✅ |
| `BREVO_SMTP_KEY` | ✅ |
| `ANTHROPIC_API_KEY` | ✅ |
| `N8N_WEBHOOK_URL` | ✅ |
| `KEY_VAULT_ENDPOINT` | ✅ (for Key Vault integration) |
| `EmailSettings:FromEmail` | ✅ |
| `EmailSettings:CcEmail` | Optional |
| `AllowedOrigins` | ✅ (CORS) |

### Key Vault Integration

```csharp
// Api/Program.cs
var keyVaultEndpoint = Environment.GetEnvironmentVariable("KEY_VAULT_ENDPOINT");
if (!string.IsNullOrEmpty(keyVaultEndpoint))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultEndpoint),
        new DefaultAzureCredential());
}
```

Once configured, Key Vault secrets are accessible via `IConfiguration["SECRET-NAME"]`.

---

## IOptions Pattern

Both projects use `AddOptions<T>().BindConfiguration()` for strongly-typed config access.

### Pattern Variants

| Variant | Lifetime | Use When |
|---------|----------|----------|
| `IOptions<T>` | Singleton, read once | Default for both projects ✅ |
| `IOptionsSnapshot<T>` | Scoped, per-request | Server-side only (not WASM) |
| `IOptionsMonitor<T>` | Singleton + change tracking | Background services with dynamic config |

### Options Class Convention

```csharp
public class EmailServiceOptions
{
    public const string SectionName = "EmailService";          // Config section key
    public string ApiBaseUrl { get; set; } = "/api";           // Default value
    public int TimeoutSeconds { get; set; } = 30;
    public string SendEmailEndpoint { get; set; } = "send-email";
    public string SendEmailUrl => $"{ApiBaseUrl.TrimEnd('/')}/{SendEmailEndpoint}";  // Computed URL
}
```

### Registration (same in both projects)

```csharp
// Program.cs
builder.Services.AddOptions<EmailServiceOptions>()
    .BindConfiguration(EmailServiceOptions.SectionName);
```

### Injection

```csharp
public class ApiEmailService
{
    private readonly EmailServiceOptions _options;

    public ApiEmailService(IOptions<EmailServiceOptions> options)
    {
        _options = options.Value;
    }
}
```

### Validation (optional, recommended for Functions)

```csharp
builder.Services.AddOptions<EmailServiceOptions>()
    .BindConfiguration(EmailServiceOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();  // Fail fast on bad config
```

---

## Secrets Strategy

| Data Type | Access Pattern | Example |
|-----------|---------------|---------|
| Non-sensitive config | `IOptions<T>` | Email addresses, timeouts, rate limits |
| Secrets / API keys | `IConfiguration["KEY"]` | `BREVO_SMTP_KEY`, `ANTHROPIC_API_KEY` |

```csharp
public class SendEmailFunction
{
    private readonly IConfiguration _config;        // For secrets
    private readonly EmailSettings _emailSettings;  // For non-secrets

    public SendEmailFunction(IConfiguration config, IOptions<EmailSettings> options)
    {
        _config = config;
        _emailSettings = options.Value;
    }

    public async Task<IActionResult> Run(HttpRequest req)
    {
        var smtpKey = _config["BREVO_SMTP_KEY"];          // Secret from Key Vault / env var
        var fromEmail = _emailSettings.FromEmail;          // Non-secret from IOptions
    }
}
```

> ❌ Never put secrets in IOptions classes — default values could leak.

---

## Local Development Override

In `Program.cs`, dev mode redirects API calls to the local Functions instance:

```csharp
if (builder.HostEnvironment.IsDevelopment())
{
    const string functionsLocalUrl = "http://localhost:7257/api";
    builder.Configuration["ChatbotService:ApiBaseUrl"] = functionsLocalUrl;
    builder.Configuration["EmailService:ApiBaseUrl"] = functionsLocalUrl;
    builder.Configuration["BookingService:ApiBaseUrl"] = functionsLocalUrl;
}
```

In production, the default `/api` works because Azure Static Web Apps proxies `/api/*` to the linked Functions app.

---

## Current Options Classes

### Frontend (Blazor WASM)

| Class | Location | Section | Key Properties |
|-------|----------|---------|----------------|
| `EmailServiceOptions` | `Features/Contact/` | `EmailService` | `ApiBaseUrl`, `TimeoutSeconds`, `MaxRetries`, `SendEmailUrl` (computed) |
| `ChatbotOptions` | `Features/Chat/` | `ChatbotService` | `ApiBaseUrl`, `TimeoutSeconds`, `ChatUrl` (computed) |
| `BookingServiceOptions` | `Features/Booking/` | `BookingService` | `ApiBaseUrl`, `TimeoutSeconds`, `BookAppointmentUrl` (computed) |
| `BlobStorageOptions` | `Common/Options/` | `BlobStorage` | `ResumeUrl`, `ContainerName` |

### Backend (Azure Functions)

| Class | Location | Section | Key Properties |
|-------|----------|---------|----------------|
| `RateLimitOptions` | `Api/Shared/Models/` | `RateLimiting` | `PermitLimit`, `WindowSeconds`, `QueueLimit`, `EnableCircuitBreaker` |
| `EmailSettings` | `Api/Features/Contact/` | `EmailSettings` | `FromEmail`, `CcEmail`, `ToEmail`, `FromName` |

---

## Troubleshooting

| Problem | Cause | Solution |
|---------|-------|----------|
| Wrong API URL in production | `appsettings.Production.json` missing/incorrect | Verify file has correct URL |
| Config not loading in WASM | File not in `wwwroot/` | Move to `wwwroot/` folder |
| Setting is null in Functions | Not in Azure Portal | Add to Configuration → App settings |
| Works locally, fails in Azure | `local.settings.json` not deployed | Add settings to Azure Portal |
| Settings not taking effect | Function not restarted | Restart the Function App after config changes |

---

## Related Docs

- [Vertical Slice Architecture](VERTICAL_SLICE_ARCHITECTURE.md) — Where options classes live per feature
- [API Endpoints](API_ENDPOINTS.md) — Endpoints that consume these config values
- [Azure Functions](AZURE_FUNCTIONS.md) — API Program.cs config binding and Key Vault setup
- [Component Architecture](COMPONENT_ARCHITECTURE.md) — How services inject IOptions in components

---

*Last Updated: March 2026*
