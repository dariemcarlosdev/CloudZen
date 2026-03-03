# Configuration Management Guide

## Overview

This document explains the configuration architecture for the CloudZen application, which consists of a **Blazor WebAssembly frontend** and an **Azure Functions backend**. Understanding where and how configuration is managed is critical for successful local development and production deployment.

---

## Architecture Summary

```
???????????????????????????????????????????????????????????????????
?                    BLAZOR APP (Frontend)                        ?
?                                                                 ?
?   ?? Runs in the BROWSER (client-side)                         ?
?   ?? Config: wwwroot/appsettings.*.json (static files)         ?
?   ?? NO secrets allowed (publicly accessible)                   ?
?   ??  NO Azure Portal config needed                             ?
?                                                                 ?
?   Configuration is bundled with the app and downloaded          ?
?   by the browser when the application loads.                    ?
???????????????????????????????????????????????????????????????????
                              ?
                              ? HTTP Requests
                              ?
???????????????????????????????????????????????????????????????????
?                 AZURE FUNCTION (Backend)                        ?
?                                                                 ?
?   ???  Runs on the SERVER (server-side)                          ?
?   ?? Config: local.settings.json (dev) / Azure Portal (prod)   ?
?   ?? Secrets ARE allowed (secure server environment)            ?
?   ??  Azure Portal config IS required for production            ?
?                                                                 ?
?   Configuration is loaded from environment variables            ?
?   which are set in Azure Portal for production.                 ?
???????????????????????????????????????????????????????????????????
```

---

## Blazor WebAssembly Configuration

### How It Works

Blazor WebAssembly uses the standard ASP.NET Core configuration system, but with important differences:

1. **Configuration files live in `wwwroot/`** - They are static assets served to the browser
2. **Files are publicly accessible** - Anyone can view them in browser DevTools
3. **Environment-based loading** - Files are automatically loaded based on environment
4. **No server-side secrets** - Never store API keys or sensitive data here

### Configuration Files

| File | Purpose | Loaded When |
|------|---------|-------------|
| `wwwroot/appsettings.json` | Base configuration (defaults) | Always |
| `wwwroot/appsettings.Development.json` | Development overrides | Local development (`dotnet run`) |
| `wwwroot/appsettings.Production.json` | Production overrides | Deployed to Azure/production |

### Environment Detection

Blazor WebAssembly determines the environment automatically:

| Scenario | Environment | Config File Used |
|----------|-------------|------------------|
| `dotnet run` locally | `Development` | `appsettings.Development.json` |
| `dotnet run --configuration Release` | `Production` | `appsettings.Production.json` |
| Deployed to Azure Static Web Apps | `Production` | `appsettings.Production.json` |
| Deployed to any production host | `Production` | `appsettings.Production.json` |

### Configuration Loading Order

```
1. wwwroot/appsettings.json              ? Base settings (always loaded first)
         ?
2. wwwroot/appsettings.{Environment}.json ? Environment-specific overrides
         ?
3. Final merged configuration             ? Used by the application
```

Later files **override** earlier ones. For example, if both files define `EmailService.ApiBaseUrl`, the environment-specific value wins.

### Current Configuration Files

#### `wwwroot/appsettings.json` (Base)

```json
{
  "EmailService": {
    "ApiBaseUrl": "/api",
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "SendEmailEndpoint": "send-email"
  },
  "BlobStorage": {
    "ResumeUrl": "https://...",
    "ContainerName": "cloudzencontainer",
    "StorageAccountName": "cloudzenstorage"
  }
}
```

#### `wwwroot/appsettings.Development.json` (Local Development)

```json
{
  "EmailService": {
    "ApiBaseUrl": "http://localhost:7257/api",
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "SendEmailEndpoint": "send-email"
  }
}
```

**Purpose**: Points to the locally running Azure Function for development.

#### `wwwroot/appsettings.Production.json` (Production)

```json
{
  "EmailService": {
    "ApiBaseUrl": "https://cloudzen-api-func-e4gehdaef9ftdhbn.westus2-01.azurewebsites.net/api",
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "SendEmailEndpoint": "send-email"
  }
}
```

**Purpose**: Points to the deployed Azure Function in production.

### Why No Azure Portal Config for Blazor?

| Reason | Explanation |
|--------|-------------|
| **Static Files** | Config files are bundled into the app at build time |
| **Client-Side** | The app runs entirely in the browser |
| **No Server** | There's no server-side process to read environment variables |
| **SPA Hosting** | Azure Static Web Apps serves files as-is, no server processing |

### Security Warning ??

**NEVER put secrets in Blazor WebAssembly configuration files!**

```json
// ? WRONG - Never do this!
{
  "ApiKey": "sk-secret-key-12345",
  "ConnectionString": "Server=...;Password=secret"
}

// ? CORRECT - Only non-sensitive settings
{
  "ApiBaseUrl": "https://my-api.azurewebsites.net/api",
  "TimeoutSeconds": 30
}
```

Blazor WebAssembly config is **publicly accessible**. Anyone can:
- Open browser DevTools ? Network tab
- See all downloaded files including `appsettings.*.json`
- Read any values stored there

---

## Azure Functions Configuration

### How It Works

Azure Functions use the standard .NET configuration system with environment variables:

1. **Local development**: Uses `local.settings.json`
2. **Production**: Uses Azure Portal ? Configuration ? Application settings
3. **Secrets are safe**: Server-side code, not exposed to clients
4. **Environment variables**: All settings become environment variables at runtime

### Configuration Files

| Environment | Configuration Source |
|-------------|---------------------|
| Local Development | `Api/local.settings.json` |
| Production (Azure) | Azure Portal ? Function App ? Configuration |

### Local Development: `local.settings.json`

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AZURE_FUNCTIONS_ENVIRONMENT": "Development",
    
    "BREVO_SMTP_LOGIN": "<your-smtp-login>@smtp-brevo.com",
    "BREVO_SMTP_KEY": "xsmtpsib-<your-key>",
    
    "EmailSettings:FromEmail": "your-email@example.com",
    "EmailSettings:CcEmail": "cc-email@example.com",
    
    "RateLimiting:PermitLimit": "10",
    "RateLimiting:WindowSeconds": "60"
  }
}
```

> ?? **Security Note**: `local.settings.json` should be in `.gitignore` and never committed with real credentials.

### Production: Azure Portal Configuration

In **Azure Portal ? Function App ? Configuration ? Application settings**, add:

| Setting | Value | Required |
|---------|-------|----------|
| `BREVO_SMTP_LOGIN` | `<id>@smtp-brevo.com` | ? Yes |
| `BREVO_SMTP_KEY` | `xsmtpsib-<your-key>` | ? Yes |
| `EmailSettings:FromEmail` | `your-email@example.com` | ? Yes |
| `EmailSettings:CcEmail` | `cc-email@example.com` | Optional |

### Why Azure Portal Config for Functions?

| Reason | Explanation |
|--------|-------------|
| **Server-Side** | Code runs on Azure servers, can access secure config |
| **Environment Variables** | Azure injects settings as environment variables |
| **Secure Storage** | Settings encrypted at rest in Azure |
| **No Source Control** | Secrets never touch your Git repository |
| **Easy Updates** | Change settings without redeploying code |

### Accessing Configuration in Code

```csharp
public class SendEmailFunction
{
    private readonly IConfiguration _config;

    public SendEmailFunction(IConfiguration config)
    {
        _config = config;
    }

    public async Task Run(...)
    {
        // Read from configuration (works in both local and Azure)
        var smtpLogin = _config["BREVO_SMTP_LOGIN"];
        var fromEmail = _config["EmailSettings:FromEmail"];
        
        // Or use environment variables directly
        var smtpKey = Environment.GetEnvironmentVariable("BREVO_SMTP_KEY");
    }
}
```

---

## Configuration Comparison

| Aspect | Blazor WebAssembly | Azure Functions |
|--------|-------------------|-----------------|
| **Runs Where** | Browser (client) | Azure Server (server) |
| **Config Files** | `wwwroot/appsettings.*.json` | `local.settings.json` |
| **Production Config** | Bundled in app | Azure Portal |
| **Secrets Allowed** | ? No | ? Yes |
| **Publicly Visible** | ? Yes | ? No |
| **Azure Portal Needed** | ? No | ? Yes |
| **Environment Detection** | Automatic | Automatic |

---

## Complete Configuration Checklist

### Local Development Setup

#### Blazor App
- [ ] `wwwroot/appsettings.json` exists with base settings
- [ ] `wwwroot/appsettings.Development.json` exists with local API URL
- [ ] No secrets in any `wwwroot/*.json` files

#### Azure Function
- [ ] `Api/local.settings.json` exists (copy from template if needed)
- [ ] `BREVO_SMTP_LOGIN` configured
- [ ] `BREVO_SMTP_KEY` configured
- [ ] `EmailSettings:FromEmail` configured
- [ ] File is in `.gitignore`

### Production Deployment Setup

#### Blazor App
- [ ] `wwwroot/appsettings.Production.json` exists with production API URL
- [ ] No secrets in configuration files
- [ ] Deploy via `git push` (GitHub Actions handles the rest)

#### Azure Function
- [ ] Deploy code: `func azure functionapp publish <name>`
- [ ] Add `BREVO_SMTP_LOGIN` in Azure Portal
- [ ] Add `BREVO_SMTP_KEY` in Azure Portal
- [ ] Add `EmailSettings:FromEmail` in Azure Portal
- [ ] Add `EmailSettings:CcEmail` in Azure Portal (optional)
- [ ] Click **Save** and **Restart**

---

## IOptions Pattern

Both projects use the **IOptions pattern** for strongly-typed configuration access.

### Blazor App Example

**Configuration class** (`Models/Options/EmailServiceOptions.cs`):
```csharp
public class EmailServiceOptions
{
    public const string SectionName = "EmailService";
    
    public string ApiBaseUrl { get; set; } = "/api";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public string SendEmailEndpoint { get; set; } = "send-email";
    
    public string SendEmailUrl => $"{ApiBaseUrl.TrimEnd('/')}/{SendEmailEndpoint}";
}
```

**Registration** (`Program.cs`):
```csharp
builder.Services.AddOptions<EmailServiceOptions>()
    .BindConfiguration(EmailServiceOptions.SectionName);
```

**Usage** (in a service):
```csharp
public class ApiEmailService
{
    private readonly EmailServiceOptions _options;

    public ApiEmailService(IOptions<EmailServiceOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendEmailAsync(...)
    {
        var endpoint = _options.SendEmailUrl; // Uses configured values
    }
}
```

### Azure Function Example

**Configuration class** (`Models/EmailSettings.cs`):
```csharp
public class EmailSettings
{
    public string FromEmail { get; set; } = string.Empty;
    public string? FromName { get; set; }
    public string? CcEmail { get; set; }
}
```

**Registration** (`Program.cs`):
```csharp
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
```

**Usage** (in a function):
```csharp
public class SendEmailFunction
{
    private readonly EmailSettings _emailSettings;

    public SendEmailFunction(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }
}
```

---

## Troubleshooting

### Blazor App Issues

| Problem | Cause | Solution |
|---------|-------|----------|
| Wrong API URL in production | `appsettings.Production.json` missing or incorrect | Verify file exists and has correct URL |
| Config not loading | File not in `wwwroot/` | Move file to `wwwroot/` folder |
| Development config in production | Environment not detected | Check hosting configuration |
| `MethodNotAllowed` error | API URL pointing to wrong host | Verify `ApiBaseUrl` in production config |

### Azure Function Issues

| Problem | Cause | Solution |
|---------|-------|----------|
| `BREVO_SMTP_LOGIN` is null | Setting not in Azure Portal | Add to Configuration ? Application settings |
| Works locally, fails in Azure | `local.settings.json` not deployed | Add settings to Azure Portal |
| Settings not taking effect | Function not restarted | Restart the Function App |
| Can't find configuration section | Wrong section name | Verify `GetSection()` parameter matches JSON |

### How to Verify Configuration

**Blazor App** - Check browser DevTools:
1. Open DevTools (F12)
2. Go to Network tab
3. Refresh the page
4. Look for `appsettings.*.json` files
5. Verify correct values are loaded

**Azure Function** - Check Azure Portal:
1. Go to Function App ? Configuration
2. Verify all required settings exist
3. Check for typos in setting names
4. Ensure you clicked **Save**

---

## Best Practices

### DO ?

- Use environment-specific config files for different API URLs
- Store secrets only in Azure Portal or Key Vault
- Use the IOptions pattern for strongly-typed access
- Keep `local.settings.json` in `.gitignore`
- Validate configuration at startup when possible

### DON'T ?

- Store API keys in `wwwroot/appsettings.*.json`
- Commit `local.settings.json` with real credentials
- Hardcode URLs that change between environments
- Mix client and server configuration concerns
- Forget to restart Azure Function after config changes

---

## Related Documentation

- [ASP.NET Core Configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Blazor WebAssembly Configuration](https://docs.microsoft.com/en-us/aspnet/core/blazor/fundamentals/configuration)
- [Azure Functions Configuration](https://docs.microsoft.com/en-us/azure/azure-functions/functions-how-to-use-azure-function-app-settings)
- [IOptions Pattern](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options)
- [Azure Key Vault References](https://docs.microsoft.com/en-us/azure/app-service/app-service-key-vault-references)

---

## Version History

| Date | Version | Changes |
|------|---------|---------|
| 2026-03-03 | 1.0 | Initial documentation |
