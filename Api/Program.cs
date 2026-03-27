using Azure.Identity;
using CloudZen.Api.Shared.Models;
using CloudZen.Api.Shared.Security;
using CloudZen.Api.Shared.Services;
using CloudZen.Api.Features.Contact;
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

// Add Azure Key Vault configuration for secrets management
var keyVaultEndpoint = builder.Configuration["KEY_VAULT_ENDPOINT"];
if (!string.IsNullOrEmpty(keyVaultEndpoint))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultEndpoint),
        new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            // Limit credential types for better security and faster auth
            ExcludeVisualStudioCredential = true,
            ExcludeVisualStudioCodeCredential = true,
            ExcludeInteractiveBrowserCredential = true,
            // Keep these enabled for local dev and Azure
            ExcludeEnvironmentCredential = false,
            ExcludeManagedIdentityCredential = false,
            ExcludeAzureCliCredential = false,
            ExcludeAzurePowerShellCredential = false
        }));
}

// =============================================================================
// ENVIRONMENT DETECTION
// =============================================================================

var isDevelopment = builder.Environment.IsDevelopment() || 
    Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") == "Development";

// =============================================================================
// IOPTIONS PATTERN CONFIGURATION
// =============================================================================
// Using AddOptions<T>().BindConfiguration() for consistency with Blazor WASM project
// This pattern provides:
// - Strongly-typed configuration access
// - Support for validation with ValidateDataAnnotations()
// - Consistent pattern across the solution
// =============================================================================

// Configure rate limiting options
builder.Services.AddOptions<RateLimitOptions>()
    .BindConfiguration(RateLimitOptions.SectionName);

// Configure email settings options
builder.Services.AddOptions<EmailSettings>()
    .BindConfiguration(EmailSettings.SectionName);

// =============================================================================
// CORS CONFIGURATION
// =============================================================================
// Loaded from configuration with sensible defaults for local development
// =============================================================================

string[] allowedOrigins;
var configuredOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
var productionOrigin = builder.Configuration["ProductionOrigin"];

if (configuredOrigins is not null && configuredOrigins.Length > 0)
{
    allowedOrigins = configuredOrigins;
}
else if (!string.IsNullOrEmpty(productionOrigin))
{
    // Fallback: use ProductionOrigin as the sole allowed origin
    allowedOrigins = [productionOrigin];
}
else if (isDevelopment)
{
    // Local development defaults matching Blazor WASM launchSettings.json
    allowedOrigins = new[]
    {
        "https://localhost:7243",   // Blazor WASM Kestrel HTTPS
        "http://localhost:5054"     // Blazor WASM Kestrel HTTP
    };
}
else
{
    // Production: require explicit configuration
    throw new InvalidOperationException(
        "CORS 'AllowedOrigins' or 'ProductionOrigin' must be configured in production. " +
        "Set the 'AllowedOrigins' configuration section with allowed domains, " +
        "or set 'ProductionOrigin' with the primary allowed origin.");
}

// Add production origin if specified and not already included
if (!string.IsNullOrEmpty(productionOrigin) && !allowedOrigins.Contains(productionOrigin))
{
    allowedOrigins = [.. allowedOrigins, productionOrigin];
}

// Register CORS settings as a service for use in functions
builder.Services.AddSingleton(new CorsSettings(allowedOrigins));

// =============================================================================
// SERVICE REGISTRATIONS
// =============================================================================

// Register Polly-based rate limiter service
builder.Services.AddSingleton<IRateLimiterService, PollyRateLimiterService>();

// Add HTTP client factory for secure outbound calls
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

// Build and run the app
var app = builder.Build();
app.Run();
