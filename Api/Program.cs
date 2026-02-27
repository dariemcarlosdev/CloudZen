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

// Add configuration sources
builder.Configuration
    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Add Azure Key Vault configuration for secrets management
var keyVaultEndpoint = Environment.GetEnvironmentVariable("KEY_VAULT_ENDPOINT");
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

// Determine if we're in development or production
var isDevelopment = builder.Environment.IsDevelopment() || 
    Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") == "Development";

// Configure allowed origins for CORS
string[] allowedOrigins;
var configuredOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
var productionOrigin = builder.Configuration["ProductionOrigin"];

if (configuredOrigins is not null && configuredOrigins.Length > 0)
{
    allowedOrigins = configuredOrigins;
}
else if (isDevelopment)
{
    // Local development defaults - include common Visual Studio ports
    allowedOrigins = new[]
    {
        "https://localhost:5001",
        "https://localhost:7001",
        "http://localhost:5000",
        "https://localhost:44370",  // Visual Studio IIS Express HTTPS
        "http://localhost:44370",
        "https://localhost:7257",   // Visual Studio Kestrel
        "http://localhost:7257"
    };
}
else
{
    // Production: require explicit configuration
    throw new InvalidOperationException(
        "CORS 'AllowedOrigins' must be configured in production. " +
        "Set the 'AllowedOrigins' configuration section with allowed domains.");
}

// Add production origin if specified
if (!string.IsNullOrEmpty(productionOrigin) && !allowedOrigins.Contains(productionOrigin))
{
    allowedOrigins = allowedOrigins.Append(productionOrigin).ToArray();
}

// Register CORS settings as a service for use in functions (from CloudZen.Api.Security namespace)
builder.Services.AddSingleton(new CorsSettings(allowedOrigins));

// Configure rate limiting options from configuration
builder.Services.Configure<RateLimitOptions>(
    builder.Configuration.GetSection(RateLimitOptions.SectionName));

// Register Polly-based rate limiter service
builder.Services.AddSingleton<IRateLimiterService, PollyRateLimiterService>();

// Add HTTP client factory for secure outbound calls
builder.Services.AddHttpClient("SecureClient", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "CloudZen-Api/1.0");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Application Insights with security-conscious settings
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
