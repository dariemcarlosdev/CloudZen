using CloudZen;
using CloudZen.Models.Options;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CloudZen.Services;
using CloudZen.Services.Abstractions;
using Microsoft.Extensions.Options;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// =============================================================================
// CONFIGURATION WITH IOPTIONS PATTERN
// =============================================================================
// The IOptions pattern provides:
// - Strongly-typed access to configuration settings
// - Compile-time checking of configuration access
// - Easy unit testing through mocking
// - Support for configuration validation
//
// Note: Blazor WebAssembly uses BindConfiguration() extension method
// =============================================================================

// Configure Email Service options from appsettings.json
// Section: "EmailService"
builder.Services.AddOptions<EmailServiceOptions>()
    .BindConfiguration(EmailServiceOptions.SectionName);

// Configure Blob Storage options from appsettings.json
// Section: "BlobStorage"
builder.Services.AddOptions<BlobStorageOptions>()
    .BindConfiguration(BlobStorageOptions.SectionName);

// =============================================================================
// HTTP CLIENT REGISTRATION
// =============================================================================

// Register HttpClient with base address for API calls
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) 
});

// =============================================================================
// SECURITY NOTES FOR BLAZOR WEBASSEMBLY
// =============================================================================
// - Blazor WebAssembly cannot use DefaultAzureCredential() - requires secure server environment
// - Cannot access secrets.json directly - use Azure Key Vault via backend API
// - All configuration in wwwroot/appsettings.json is publicly accessible
// - NEVER store API keys, connection strings, or secrets in client-side config
// - Use Azure Functions backend for sensitive operations (email, storage writes)
// =============================================================================

// Legacy blob storage endpoint (consider migrating to BlobStorageOptions)
var blobServiceEndpoint = builder.Configuration["CloudZenBlobStorageConnection:blobServiceUri"];

// =============================================================================
// SERVICE REGISTRATIONS
// =============================================================================

// Register GoogleCalendarUrlService as a singleton
builder.Services.AddSingleton<GoogleCalendarUrlService>();

// Register TicketService as the implementation for ITicketService
builder.Services.AddSingleton<ITicketService, TicketService>();

// Register ResumeService after HttpClient
// TODO: Refactor to use IOptions<BlobStorageOptions> pattern
builder.Services.AddScoped(sp => new ResumeService(
    sp.GetRequiredService<HttpClient>(),
    blobServiceEndpoint,
    sp.GetRequiredService<ILogger<ResumeService>>()
));

// Register ApiEmailService as the implementation for IEmailService
// Uses IOptions<EmailServiceOptions> for configuration
// This sends emails through the Azure Functions API backend (secure for WebAssembly)
builder.Services.AddScoped<IEmailService, ApiEmailService>();

// Register ProjectService for managing portfolio projects
builder.Services.AddScoped<ProjectService>();

// Register PersonalService for managing service offerings
builder.Services.AddScoped<PersonalService>();

await builder.Build().RunAsync();
