# Configuration Management Best Practices with IOptions Pattern

This document provides guidance on managing configuration in .NET applications using the **IOptions pattern**, with specific sections for **Blazor WebAssembly** and **Azure Functions** architectures.

## Table of Contents

### Part 1: Overview
1. [Introduction](#introduction)
2. [IOptions Pattern Variants](#ioptions-pattern-variants)
3. [Consistent Pattern Across Solution](#consistent-pattern-across-solution)

### Part 2: Blazor WebAssembly Configuration
4. [WASM Configuration Overview](#wasm-configuration-overview)
5. [WASM Options Classes](#wasm-options-classes)
6. [WASM Program.cs Setup](#wasm-programcs-setup)
7. [WASM Configuration Files](#wasm-configuration-files)
8. [WASM Security Considerations](#wasm-security-considerations)

### Part 3: Azure Functions Configuration
9. [Azure Functions Configuration Overview](#azure-functions-configuration-overview)
10. [Azure Functions Options Classes](#azure-functions-options-classes)
11. [Azure Functions Program.cs Setup](#azure-functions-programcs-setup)
12. [Azure Functions Configuration Files](#azure-functions-configuration-files)
13. [Azure Functions Secrets Management](#azure-functions-secrets-management)

### Part 4: Advanced Topics
14. [Configuration Validation](#configuration-validation)
15. [Testing with IOptions](#testing-with-ioptions)
16. [Migration Guide](#migration-guide)

---

# Part 1: Overview

## Introduction

### What is the IOptions Pattern?

The IOptions pattern provides a **strongly-typed** way to access groups of related configuration settings. Instead of accessing configuration values through string keys, you define classes that represent your configuration sections.

### Benefits

| Benefit | Description |
|---------|-------------|
| **Type Safety** | Compile-time checking of configuration access |
| **IntelliSense** | Full IDE support with auto-completion |
| **Validation** | Built-in support for validating configuration on startup |
| **Testability** | Easy to mock in unit tests |
| **Reloadable** | Support for configuration changes without restart (IOptionsMonitor) |
| **Documentation** | Self-documenting through property names and XML comments |

### Pattern Variants

```
IOptions<T>         → Singleton, read once at startup
IOptionsSnapshot<T> → Scoped, re-read per request (not for WASM)
IOptionsMonitor<T>  → Singleton with change notifications
```

---

## IOptions Pattern Variants

### IOptions<T> (Recommended for Both Projects)

- **Lifetime**: Singleton - value is computed once and cached
- **When to use**: Configuration that doesn't change during app lifetime
- **Best for**: Blazor WebAssembly, most Azure Functions scenarios

```csharp
public class MyService
{
    private readonly MyOptions _options;
    
    public MyService(IOptions<MyOptions> options)
    {
        _options = options.Value; // Read once, cached
    }
}
```

### IOptionsSnapshot<T> (Server-side only)

- **Lifetime**: Scoped - new instance per request
- **When to use**: Configuration that may change between requests
- **Note**: ⚠️ **Not available in Blazor WebAssembly** (no request scope)

```csharp
// Azure Functions or ASP.NET Core only
public class MyFunction
{
    private readonly MyOptions _options;
    
    public MyFunction(IOptionsSnapshot<MyOptions> options)
    {
        _options = options.Value; // Fresh value per request
    }
}
```

### IOptionsMonitor<T>

- **Lifetime**: Singleton with change tracking
- **When to use**: Long-running services that need to react to config changes
- **Best for**: Background services, Azure Functions with dynamic config

```csharp
public class MyBackgroundService : BackgroundService
{
    private readonly IOptionsMonitor<MyOptions> _optionsMonitor;
    
    public MyBackgroundService(IOptionsMonitor<MyOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
        
        // React to configuration changes
        _optionsMonitor.OnChange(options => 
        {
            Console.WriteLine($"Config changed: {options.SomeValue}");
        });
    }
}
```

---

## Consistent Pattern Across Solution

### CloudZen Solution Uses `AddOptions<T>().BindConfiguration()`

Both projects use the **same registration pattern** for consistency:

```csharp
// Works in BOTH Blazor WASM and Azure Functions
builder.Services.AddOptions<MyOptions>()
    .BindConfiguration(MyOptions.SectionName);
```

| Project | Pattern | Package Required |
|---------|---------|------------------|
| **CloudZen (Blazor WASM)** | `AddOptions<T>().BindConfiguration()` | `Microsoft.Extensions.Options.ConfigurationExtensions` 8.0.0 |
| **CloudZen.Api (Azure Functions)** | `AddOptions<T>().BindConfiguration()` | `Microsoft.Extensions.Options.ConfigurationExtensions` 10.0.0 |

### Why Use `BindConfiguration()` Over `Configure<T>()`?

| Feature | `BindConfiguration()` | `Configure<T>(section)` |
|---------|----------------------|------------------------|
| **Syntax** | Cleaner, chainable | Requires section parameter |
| **Validation** | Chainable with `.ValidateDataAnnotations()` | Separate registration |
| **Consistency** | Works same in WASM and server | Different overloads in WASM |
| **Discoverability** | Better IntelliSense | OK |

---

# Part 2: Blazor WebAssembly Configuration

## WASM Configuration Overview

### Key Characteristics

| Aspect | Description |
|--------|-------------|
| **Runtime** | Runs in browser (client-side) |
| **Config Location** | `wwwroot/appsettings.json` |
| **Security** | ⚠️ All configuration is PUBLIC |
| **Secrets** | ❌ NEVER store secrets here |
| **Format** | Standard JSON hierarchy |

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                   Blazor WebAssembly                         │
│                   (Browser Runtime)                          │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  wwwroot/appsettings.json                                   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  ✅ API endpoints (e.g., "/api")                    │   │
│  │  ✅ Timeouts, retry counts                          │   │
│  │  ✅ Feature flags                                   │   │
│  │  ✅ SAS token URLs (read-only blob access)          │   │
│  │  ❌ API keys (NEVER!)                               │   │
│  │  ❌ Connection strings (NEVER!)                     │   │
│  │  ❌ Passwords (NEVER!)                              │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                              │
│  IOptions<T> Registration                                    │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  EmailServiceOptions    → API base URL, timeouts    │   │
│  │  BlobStorageOptions     → SAS token URLs            │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## WASM Options Classes

### EmailServiceOptions

**File: `Models/Options/EmailServiceOptions.cs`**

```csharp
namespace CloudZen.Models.Options;

/// <summary>
/// Configuration options for the email service client.
/// </summary>
/// <remarks>
/// <para>
/// This class is used with the IOptions pattern to configure the 
/// <see cref="Services.ApiEmailService"/>. Settings are configured in 
/// <c>wwwroot/appsettings.json</c> under the <c>EmailService</c> section.
/// </para>
/// <para>
/// <b>Important:</b> In Blazor WebAssembly, do NOT store sensitive values here.
/// API keys should only exist in the Azure Functions backend.
/// </para>
/// </remarks>
public class EmailServiceOptions
{
    /// <summary>
    /// The configuration section name for binding.
    /// </summary>
    public const string SectionName = "EmailService";

    /// <summary>
    /// Gets or sets the base URL for the email API backend.
    /// </summary>
    /// <value>Defaults to "/api" for Azure Static Web Apps linked functions.</value>
    public string ApiBaseUrl { get; set; } = "/api";

    /// <summary>
    /// Gets or sets the HTTP request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the email endpoint path.
    /// </summary>
    public string SendEmailEndpoint { get; set; } = "send-email";

    /// <summary>
    /// Gets the full URL for the send email endpoint.
    /// </summary>
    public string SendEmailUrl => $"{ApiBaseUrl.TrimEnd('/')}/{SendEmailEndpoint}";
}
```

### BlobStorageOptions

**File: `Models/Options/BlobStorageOptions.cs`**

```csharp
namespace CloudZen.Models.Options;

/// <summary>
/// Configuration options for Azure Blob Storage access.
/// </summary>
/// <remarks>
/// <b>Security Note:</b> Only SAS token URLs should be stored here.
/// Never store connection strings or account keys in client-side configuration.
/// </remarks>
public class BlobStorageOptions
{
    /// <summary>
    /// The configuration section name for binding.
    /// </summary>
    public const string SectionName = "BlobStorage";

    /// <summary>
    /// Gets or sets the full URL (with SAS token) for the resume PDF.
    /// </summary>
    public string ResumeUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the blob container name.
    /// </summary>
    public string ContainerName { get; set; } = "documents";

    /// <summary>
    /// Gets or sets the storage account name (for logging only).
    /// </summary>
    public string? StorageAccountName { get; set; }
}
```

---

## WASM Program.cs Setup

**File: `Program.cs`**

```csharp
using CloudZen;
using CloudZen.Models.Options;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CloudZen.Services;
using CloudZen.Services.Abstractions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// =============================================================================
// IOPTIONS PATTERN CONFIGURATION (Blazor WebAssembly)
// =============================================================================
// Using AddOptions<T>().BindConfiguration() for consistency with Azure Functions
// Requires: Microsoft.Extensions.Options.ConfigurationExtensions package
// =============================================================================

// Configure Email Service options
// Section: "EmailService" in wwwroot/appsettings.json
builder.Services.AddOptions<EmailServiceOptions>()
    .BindConfiguration(EmailServiceOptions.SectionName);

// Configure Blob Storage options
// Section: "BlobStorage" in wwwroot/appsettings.json
builder.Services.AddOptions<BlobStorageOptions>()
    .BindConfiguration(BlobStorageOptions.SectionName);

// =============================================================================
// HTTP CLIENT
// =============================================================================

builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) 
});

// =============================================================================
// SERVICE REGISTRATIONS
// =============================================================================

// Email service using IOptions<EmailServiceOptions>
builder.Services.AddScoped<IEmailService, ApiEmailService>();

// Other services...
builder.Services.AddSingleton<GoogleCalendarUrlService>();
builder.Services.AddSingleton<ITicketService, TicketService>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<PersonalService>();

await builder.Build().RunAsync();
```

---

## WASM Configuration Files

### File Structure

```
wwwroot/
├── appsettings.json                 # Base configuration (committed to Git)
├── appsettings.Development.json     # Development overrides (git-ignored)
└── appsettings.Production.json      # Production overrides (optional)
```

### appsettings.json (Base - Committed)

**File: `wwwroot/appsettings.json`**

```json
{
  "EmailService": {
    "ApiBaseUrl": "/api",
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "SendEmailEndpoint": "send-email"
  },

  "BlobStorage": {
    "ResumeUrl": "https://cloudzenstorage.blob.core.windows.net/container/resume.pdf?sv=...",
    "ContainerName": "cloudzencontainer",
    "StorageAccountName": "cloudzenstorage"
  },

  "EmailSettings": {
    "Provider": "Brevo",
    "FromEmail": "cloudzen.inc@gmail.com",
    "CcEmail": "softevolutionsl@gmail.com"
  }
}
```

### appsettings.Development.json (Local - Git-ignored)

**File: `wwwroot/appsettings.Development.json`**

```json
{
  "EmailService": {
    "ApiBaseUrl": "http://localhost:7071/api",
    "TimeoutSeconds": 60
  }
}
```

### .gitignore Entries

```gitignore
# Environment-specific config files
**/wwwroot/appsettings.Development.json
**/wwwroot/appsettings.*.json
!**/wwwroot/appsettings.json
```

---

## WASM Security Considerations

### ⚠️ Critical: Everything is PUBLIC

```
┌─────────────────────────────────────────────────────────────┐
│  ⚠️  BLAZOR WEBASSEMBLY SECURITY WARNING                    │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Everything in wwwroot/ is downloadable by anyone!          │
│                                                              │
│  Browser DevTools → Network → appsettings.json              │
│  Result: ALL configuration is visible to users              │
│                                                              │
│  ❌ NEVER include:                                          │
│     • API keys                                               │
│     • Connection strings                                     │
│     • Passwords                                              │
│     • Private endpoints                                      │
│     • Tokens (except SAS with limited scope)                 │
│                                                              │
│  ✅ SAFE to include:                                         │
│     • Public API endpoints                                   │
│     • Timeouts and retry settings                           │
│     • Feature flags                                          │
│     • Read-only SAS URLs (limited expiry)                   │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### What Can/Cannot Be Done in WASM

| ❌ Cannot Do | ✅ Can Do |
|-------------|----------|
| Access Azure Key Vault directly | Call backend APIs that access Key Vault |
| Use connection strings | Use SAS tokens for Blob Storage |
| Store API keys in config | Store non-sensitive settings |
| Use DefaultAzureCredential | Use public endpoints with SAS |
| Send emails directly | Call Azure Function to send emails |

---

# Part 3: Azure Functions Configuration

## Azure Functions Configuration Overview

### Key Characteristics

| Aspect | Description |
|--------|-------------|
| **Runtime** | Server-side (Azure or local) |
| **Config Location** | `local.settings.json` + Environment Variables |
| **Security** | ✅ Can store secrets securely |
| **Secrets** | ✅ Use Key Vault or App Settings |
| **Format** | Flat key-value in `Values` section |

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                   Azure Functions                            │
│                   (Server Runtime)                           │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Configuration Sources (Priority Order):                     │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  1. local.settings.json (local development)         │   │
│  │  2. Environment Variables (Azure App Settings)      │   │
│  │  3. Azure Key Vault (secrets)                       │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                              │
│  IOptions<T> Registration                                    │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  RateLimitOptions   → Rate limiting configuration   │   │
│  │  EmailSettings      → Email addresses (not keys!)   │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                              │
│  Direct Configuration Access (for secrets)                   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  IConfiguration["BREVO_API_KEY"]                    │   │
│  │  IConfiguration["KEY_VAULT_ENDPOINT"]               │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## Azure Functions Options Classes

### RateLimitOptions

**File: `Api/Models/RateLimitOptions.cs`**

```csharp
namespace CloudZen.Api.Models;

/// <summary>
/// Configuration options for rate limiting and resilience policies.
/// </summary>
public class RateLimitOptions
{
    /// <summary>
    /// The configuration section name for binding.
    /// </summary>
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Gets or sets the number of requests allowed per time window.
    /// </summary>
    public int PermitLimit { get; set; } = 10;

    /// <summary>
    /// Gets or sets the time window duration in seconds.
    /// </summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the maximum queued requests.
    /// </summary>
    public int QueueLimit { get; set; } = 0;

    /// <summary>
    /// Gets or sets the inactivity timeout in minutes.
    /// </summary>
    public int InactivityTimeoutMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets whether circuit breaker is enabled.
    /// </summary>
    public bool EnableCircuitBreaker { get; set; } = false;

    /// <summary>
    /// Gets or sets the circuit breaker failure threshold.
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the circuit breaker duration in seconds.
    /// </summary>
    public int CircuitBreakerDurationSeconds { get; set; } = 30;
}
```

### EmailSettings

**File: `Api/Models/EmailSettings.cs`**

```csharp
namespace CloudZen.Api.Models;

/// <summary>
/// Configuration options for email sending functionality.
/// </summary>
/// <remarks>
/// <b>Note:</b> API keys should NOT be stored in this options class.
/// Use IConfiguration directly for secrets from Key Vault or environment variables.
/// </remarks>
public class EmailSettings
{
    /// <summary>
    /// The configuration section name for binding.
    /// </summary>
    public const string SectionName = "EmailSettings";

    /// <summary>
    /// Gets or sets the sender email address.
    /// </summary>
    public string FromEmail { get; set; } = "cloudzen.inc@gmail.com";

    /// <summary>
    /// Gets or sets the CC email address.
    /// </summary>
    public string? CcEmail { get; set; }

    /// <summary>
    /// Gets or sets the recipient email address.
    /// </summary>
    public string? ToEmail { get; set; }

    /// <summary>
    /// Gets or sets the sender display name.
    /// </summary>
    public string FromName { get; set; } = "CloudZen Contact";
}
```

---

## Azure Functions Program.cs Setup

**File: `Api/Program.cs`**

```csharp
using Azure.Identity;
using CloudZen.Api.Models;
using CloudZen.Api.Security;
using CloudZen.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

// =============================================================================
// CONFIGURATION SOURCES
// =============================================================================
// Priority order (last wins):
// 1. local.settings.json (local development)
// 2. Environment variables (Azure App Settings in production)
// 3. Azure Key Vault (secrets, if KEY_VAULT_ENDPOINT is set)
// =============================================================================

builder.Configuration
    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Add Azure Key Vault for secrets management
var keyVaultEndpoint = Environment.GetEnvironmentVariable("KEY_VAULT_ENDPOINT");
if (!string.IsNullOrEmpty(keyVaultEndpoint))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultEndpoint),
        new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ExcludeVisualStudioCredential = true,
            ExcludeVisualStudioCodeCredential = true,
            ExcludeInteractiveBrowserCredential = true,
            ExcludeEnvironmentCredential = false,
            ExcludeManagedIdentityCredential = false,
            ExcludeAzureCliCredential = false,
            ExcludeAzurePowerShellCredential = false
        }));
}

// =============================================================================
// IOPTIONS PATTERN CONFIGURATION (Azure Functions)
// =============================================================================
// Using AddOptions<T>().BindConfiguration() for consistency with Blazor WASM
// Requires: Microsoft.Extensions.Options.ConfigurationExtensions package
// =============================================================================

// Configure rate limiting options
// Section: "RateLimiting" in local.settings.json
builder.Services.AddOptions<RateLimitOptions>()
    .BindConfiguration(RateLimitOptions.SectionName);

// Configure email settings options
// Section: "EmailSettings" in local.settings.json
builder.Services.AddOptions<EmailSettings>()
    .BindConfiguration(EmailSettings.SectionName);

// =============================================================================
// CORS CONFIGURATION
// =============================================================================

var isDevelopment = builder.Environment.IsDevelopment() || 
    Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") == "Development";

string[] allowedOrigins;
var configuredOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();

if (configuredOrigins is not null && configuredOrigins.Length > 0)
{
    allowedOrigins = configuredOrigins;
}
else if (isDevelopment)
{
    allowedOrigins =
    [
        "https://localhost:5001",
        "https://localhost:7001",
        "http://localhost:5000",
        "https://localhost:44370",
        "https://localhost:7257"
    ];
}
else
{
    throw new InvalidOperationException(
        "CORS 'AllowedOrigins' must be configured in production.");
}

builder.Services.AddSingleton(new CorsSettings(allowedOrigins));

// =============================================================================
// SERVICE REGISTRATIONS
// =============================================================================

builder.Services.AddSingleton<IRateLimiterService, PollyRateLimiterService>();

builder.Services.AddHttpClient("SecureClient", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "CloudZen-Api/1.0");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// =============================================================================
// APPLICATION INSIGHTS
// =============================================================================

builder.Services
    .AddApplicationInsightsTelemetryWorkerService(options =>
    {
        options.EnableAdaptiveSampling = true;
        options.EnableQuickPulseMetricStream = true;
    })
    .ConfigureFunctionsApplicationInsights();

builder.ConfigureFunctionsWebApplication();

var app = builder.Build();
app.Run();
```

---

## Azure Functions Configuration Files

### File Structure

```
Api/
├── local.settings.json              # Local development (git-ignored)
├── host.json                        # Host configuration (committed)
└── (Azure App Settings)             # Production in Azure Portal
```

### local.settings.json (Local - Git-ignored)

**File: `Api/local.settings.json`**

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AZURE_FUNCTIONS_ENVIRONMENT": "Development",

    "BREVO_API_KEY": "your-api-key-here",

    "EmailSettings:FromEmail": "cloudzen.inc@gmail.com",
    "EmailSettings:CcEmail": "admin@example.com",

    "RateLimiting:PermitLimit": "10",
    "RateLimiting:WindowSeconds": "60",
    "RateLimiting:QueueLimit": "0",
    "RateLimiting:InactivityTimeoutMinutes": "5",
    "RateLimiting:EnableCircuitBreaker": "false",
    "RateLimiting:CircuitBreakerFailureThreshold": "5",
    "RateLimiting:CircuitBreakerDurationSeconds": "30"
  }
}
```

### host.json (Committed)

**File: `Api/host.json`**

```json
{
  "version": "2.0",
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true,
        "excludedTypes": "Request"
      },
      "enableLiveMetricsFilters": true
    }
  },
  "extensions": {
    "http": {
      "routePrefix": "api"
    }
  }
}
```

### Configuration Format Note

Azure Functions uses a **flat key-value format** in `local.settings.json`:

```json
{
  "Values": {
    "SectionName:PropertyName": "value"
  }
}
```

This maps to:
```csharp
public class SectionName
{
    public string PropertyName { get; set; }
}
```

---

## Azure Functions Secrets Management

### Secrets Strategy

```
┌─────────────────────────────────────────────────────────────┐
│              Azure Functions Secrets                         │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌─────────────────┐    ┌─────────────────────────────────┐│
│  │  IOptions<T>    │    │  IConfiguration (Direct)        ││
│  │  (Non-secrets)  │    │  (Secrets only)                 ││
│  ├─────────────────┤    ├─────────────────────────────────┤│
│  │ EmailSettings   │    │ BREVO_API_KEY                   ││
│  │ • FromEmail     │    │ KEY_VAULT_ENDPOINT              ││
│  │ • CcEmail       │    │ Connection strings              ││
│  │                 │    │                                 ││
│  │ RateLimitOptions│    │ Source:                         ││
│  │ • PermitLimit   │    │ • Environment variables         ││
│  │ • WindowSeconds │    │ • Azure Key Vault               ││
│  └─────────────────┘    └─────────────────────────────────┘│
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Why Secrets Use IConfiguration (Not IOptions)

```csharp
// ✅ CORRECT: API key from IConfiguration
var apiKey = _config["BREVO_API_KEY"];

// ❌ WRONG: Don't put secrets in IOptions classes
// They may have default values that could leak
public class BadOptions
{
    public string ApiKey { get; set; } = "default-key"; // DON'T DO THIS
}
```

### Accessing Secrets in Functions

```csharp
public class SendEmailFunction
{
    private readonly IConfiguration _config;
    private readonly EmailSettings _emailSettings;

    public SendEmailFunction(
        IConfiguration config,           // For secrets
        IOptions<EmailSettings> options) // For non-secrets
    {
        _config = config;
        _emailSettings = options.Value;
    }

    public async Task<IActionResult> Run(HttpRequest req)
    {
        // Secret from IConfiguration (comes from Key Vault or env var)
        var apiKey = _config["BREVO_API_KEY"];
        
        // Non-secret from IOptions
        var fromEmail = _emailSettings.FromEmail;
        
        // ...
    }
}
```

### Azure Key Vault Integration

```csharp
// In Program.cs
var keyVaultEndpoint = Environment.GetEnvironmentVariable("KEY_VAULT_ENDPOINT");
if (!string.IsNullOrEmpty(keyVaultEndpoint))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultEndpoint),
        new DefaultAzureCredential());
}

// Key Vault secret named "BREVO-API-KEY" becomes accessible as:
var apiKey = _config["BREVO-API-KEY"];
```

---

# Part 4: Advanced Topics

## Configuration Validation

### Data Annotations

```csharp
using System.ComponentModel.DataAnnotations;

public class EmailServiceOptions
{
    public const string SectionName = "EmailService";
    
    [Required]
    public string ApiBaseUrl { get; set; } = "/api";
    
    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;
    
    [Range(0, 10)]
    public int MaxRetries { get; set; } = 3;
}
```

### Registration with Validation

```csharp
// Blazor WASM (validation on first access)
builder.Services.AddOptions<EmailServiceOptions>()
    .BindConfiguration(EmailServiceOptions.SectionName)
    .ValidateDataAnnotations();

// Azure Functions (validation on startup - fail fast)
builder.Services.AddOptions<EmailServiceOptions>()
    .BindConfiguration(EmailServiceOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

### Custom Validator

```csharp
public class EmailServiceOptionsValidator : IValidateOptions<EmailServiceOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailServiceOptions options)
    {
        var errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(options.ApiBaseUrl))
            errors.Add("ApiBaseUrl is required");
        
        if (options.TimeoutSeconds <= 0)
            errors.Add("TimeoutSeconds must be positive");
        
        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}

// Register
builder.Services.AddSingleton<IValidateOptions<EmailServiceOptions>, 
    EmailServiceOptionsValidator>();
```

---

## Testing with IOptions

### Unit Test Helper

```csharp
using Microsoft.Extensions.Options;

// Create IOptions<T> for testing
var options = Options.Create(new EmailServiceOptions
{
    ApiBaseUrl = "https://test-api.example.com/api",
    TimeoutSeconds = 10,
    SendEmailEndpoint = "send-email"
});
```

### Full Test Example

```csharp
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

public class ApiEmailServiceTests
{
    [Fact]
    public async Task SendEmailAsync_UsesConfiguredEndpoint()
    {
        // Arrange
        var options = Options.Create(new EmailServiceOptions
        {
            ApiBaseUrl = "https://test-api.example.com/api",
            TimeoutSeconds = 10,
            SendEmailEndpoint = "send-email"
        });
        
        var mockHandler = new Mock<HttpMessageHandler>();
        // Setup mock...
        
        var httpClient = new HttpClient(mockHandler.Object);
        var logger = Mock.Of<ILogger<ApiEmailService>>();
        
        var service = new ApiEmailService(httpClient, options, logger);
        
        // Act
        var result = await service.SendEmailAsync(
            "Test", "Message", "Name", "test@example.com");
        
        // Assert
        Assert.True(result.Success);
    }
}
```

---

## Migration Guide

### From `Configure<T>()` to `AddOptions<T>().BindConfiguration()`

**Before:**
```csharp
builder.Services.Configure<RateLimitOptions>(
    builder.Configuration.GetSection(RateLimitOptions.SectionName));
```

**After:**
```csharp
builder.Services.AddOptions<RateLimitOptions>()
    .BindConfiguration(RateLimitOptions.SectionName);
```

### Migration Checklist

- [ ] Add `Microsoft.Extensions.Options.ConfigurationExtensions` package
- [ ] Update all `Configure<T>()` calls to `AddOptions<T>().BindConfiguration()`
- [ ] Add validation with `.ValidateDataAnnotations()` where needed
- [ ] Test configuration binding
- [ ] Update documentation

---

## Quick Reference

### Blazor WASM Quick Setup

```csharp
// 1. Install package
// dotnet add package Microsoft.Extensions.Options.ConfigurationExtensions

// 2. Create options class
public class MyOptions
{
    public const string SectionName = "MySection";
    public string MySetting { get; set; } = "default";
}

// 3. Add to wwwroot/appsettings.json
// { "MySection": { "MySetting": "value" } }

// 4. Register in Program.cs
builder.Services.AddOptions<MyOptions>()
    .BindConfiguration(MyOptions.SectionName);

// 5. Inject in service
public MyService(IOptions<MyOptions> options)
{
    var setting = options.Value.MySetting;
}
```

### Azure Functions Quick Setup

```csharp
// 1. Install package
// dotnet add package Microsoft.Extensions.Options.ConfigurationExtensions

// 2. Create options class (same as WASM)

// 3. Add to local.settings.json
// { "Values": { "MySection:MySetting": "value" } }

// 4. Register in Program.cs (same as WASM)
builder.Services.AddOptions<MyOptions>()
    .BindConfiguration(MyOptions.SectionName);

// 5. Inject in function (same as WASM)
public MyFunction(IOptions<MyOptions> options)
{
    var setting = options.Value.MySetting;
}
```

---

## References

- [Options pattern in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options)
- [Configuration in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Azure Functions Configuration](https://learn.microsoft.com/en-us/azure/azure-functions/functions-how-to-use-azure-function-app-settings)
- [Blazor WebAssembly Configuration](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/configuration)
- [Azure Key Vault Configuration Provider](https://learn.microsoft.com/en-us/aspnet/core/security/key-vault-configuration)
