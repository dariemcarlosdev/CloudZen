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
// LOCAL DEVELOPMENT: API URL OVERRIDE
// =============================================================================
// In production (Azure Static Web Apps), "/api" routes are proxied to the linked
// Azure Functions app automatically. In local development, the Blazor app and the
// Functions API run on separate ports, so API calls must use the absolute URL.
//
// NOTE: appsettings.Development.json can silently fail to load in Blazor WASM
// (the client fetches it via HTTP and skips it on any error). This programmatic
// override is more reliable for local development.
// =============================================================================

if (builder.HostEnvironment.IsDevelopment())
{
    const string functionsLocalUrl = "http://localhost:7257/api"; // update with your local Functions URL and port
    builder.Configuration["ChatbotService:ApiBaseUrl"] = functionsLocalUrl;
    builder.Configuration["EmailService:ApiBaseUrl"] = functionsLocalUrl;
}

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

// Configure Chatbot Service options from appsettings.json
// Section: "ChatbotService"
builder.Services.AddOptions<ChatbotOptions>()
    .BindConfiguration(ChatbotOptions.SectionName);

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

// =============================================================================
// SERVICE REGISTRATIONS
// =============================================================================

// Register GoogleCalendarUrlService
builder.Services.AddScoped<GoogleCalendarUrlService>();

// Register TicketService as the implementation for ITicketService
builder.Services.AddScoped<ITicketService, TicketService>();

// Register ResumeService (uses IOptions<BlobStorageOptions> for configuration)
builder.Services.AddScoped<ResumeService>();

// Register ApiEmailService as the implementation for IEmailService
// Uses IOptions<EmailServiceOptions> for configuration
// This sends emails through the Azure Functions API backend (secure for WebAssembly)
builder.Services.AddScoped<IEmailService, ApiEmailService>();

// Register ChatbotService as the implementation for IChatbotService
// Uses IOptions<ChatbotOptions> for configuration
// This sends chat messages through the Azure Functions API backend (API key stays server-side)
builder.Services.AddScoped<IChatbotService, ChatbotService>();

// Register ProjectService for managing portfolio projects
builder.Services.AddScoped<ProjectService>();

// Register PersonalService for managing service offerings
builder.Services.AddScoped<PersonalService>();

// Register ToolService for the Tools Overview section
builder.Services.AddScoped<ToolService>();

// Register FeatureHighlightService for the Features Showcase section
builder.Services.AddScoped<FeatureHighlightService>();

// Register MissionService for the About Us / Mission / Standards section
builder.Services.AddScoped<MissionService>();

await builder.Build().RunAsync();
