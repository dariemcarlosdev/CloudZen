using CloudZen;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Azure;
using Azure.Identity;
using CloudZen.Services;
using CloudZen.Services.Abstractions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// // Register HttpClient first
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });



// Blazor webassembly cannot use DefaultAzureCredential() because it requires a secure server environment to work properly. Instead, you can use ManagedIdentityCredential or InteractiveBrowserCredential for local development and testing.
// However, for production scenarios, it's recommended to use a secure server environment to handle authentication and token acquisition.

// If you are using Azure Static Web Apps, you can use the ManagedIdentityCredential to authenticate to Azure services.


// Blazor WebAssembly apps cannot access secrets.json directly, Instead, you can use Azure App Configuration or Azure Key Vault to manage your secrets securely.
// Using builder.Configuration to access configuration settings.
// For local development, you can use appsettings.json or user secrets to expose configuration settings.

var blobServiceEndpoint = builder.Configuration["CloudZenBlobStorageConnection:blobServiceUri"];

// Register GoogleCalendarUrlService as a singleton
builder.Services.AddSingleton<GoogleCalendarUrlService>();

// Register TicketService as the implementation for ITicketService
builder.Services.AddSingleton<ITicketService, TicketService>();

// Register ResumeService after HttpClient
builder.Services.AddScoped(sp => new ResumeService(
    sp.GetRequiredService<HttpClient>(),
    blobServiceEndpoint,
    sp.GetRequiredService<ILogger<ResumeService>>()
));

// Register EmailServiceFactory as the implementation for IEmailProvider
builder.Services.AddScoped<IEmailProvider, EmailServiceFactory>();

// Register ProjectService for managing portfolio projects
builder.Services.AddScoped<ProjectService>();

// Register PersonalService for managing service offerings
builder.Services.AddScoped<PersonalService>();


await builder.Build().RunAsync();
