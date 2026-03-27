# Component Architecture

## Overview

CloudZen is a Blazor WebAssembly app using a component-based architecture. Parent components orchestrate state and layout; children receive data via `[Parameter]` and communicate upward via `EventCallback<T>`. No centralized state management library is used.

---

## Directory Structure

> For the full folder layout, see [Vertical Slice Architecture](VERTICAL_SLICE_ARCHITECTURE.md).

Code is organized by feature — each feature owns its components, models, and services:

```
Features/
├── Booking/Components/        # Calendar, form, confirmation flow
├── Contact/Components/        # ContactForm
├── Chat/Components/           # CloudZenChatbot (FAB + panel)
├── Landing/Components/        # Hero, CTA, Services, Mission, CaseStudies, etc.
├── Profile/Components/        # ProfileHeader, ProfileApproach, ProfileHighlights
├── Projects/Components/       # ProjectCard, ProjectFilter
└── Tickets/Components/        # Tickets overview

Common/Components/             # AnimatedCounterCircle, ScrollToTopButton
Layout/                        # MainLayout, Header, Footer
Pages/                         # Thin orchestrators: Index.razor, Contact.razor
```

---

## Component Communication

### Parent → Child: `[Parameter]`

```razor
<!-- Parent passes data down -->
<ProfileHeader Title="Who I Am" AvatarUrl="/images/avatar.png" />
```

```csharp
// Child declares parameters
[Parameter] public string Title { get; set; } = string.Empty;
[Parameter] public string AvatarUrl { get; set; } = string.Empty;
```

### Child → Parent: `EventCallback<T>`

```razor
<!-- Parent binds handler -->
<ProjectFilter OnFilterChange="HandleFilterChange" />

@code {
    private void HandleFilterChange((string Status, string ProjectType) filters)
    {
        FilteredProjects = Projects
            .Where(p => string.IsNullOrEmpty(filters.Status) || p.Status == filters.Status)
            .Where(p => string.IsNullOrEmpty(filters.ProjectType) || p.ProjectType == filters.ProjectType)
            .ToList();
    }
}
```

```csharp
// Child invokes callback
[Parameter] public EventCallback<(string Status, string ProjectType)> OnFilterChange { get; set; }

private async Task OnFilterChanged()
{
    await OnFilterChange.InvokeAsync((SelectedStatus, SelectedProjectType));
}
```

### Key Principles
- **Type safety**: Compile-time checking via generic `EventCallback<T>`
- **Loose coupling**: Child doesn't know parent's implementation
- **No shared state service** needed for parent/child communication
- **Sibling communication**: Use a shared injected service when needed

---

## Service Layer

### Two Types of Services

| Type | Examples | Pattern |
|------|----------|---------|
| **Backend-calling** (async) | `ApiEmailService`, `ChatbotService`, `AppointmentService` | `HttpClient` + `IOptions<T>` → returns result type with `Ok()`/`Fail()` |
| **Data-only** (sync) | `ProjectService`, `PersonalService`, `ToolService` | In-memory data, synchronous methods, no HTTP |

### Backend-Calling Service Pattern

```csharp
public class ApiEmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly EmailServiceOptions _options;
    private readonly ILogger<ApiEmailService> _logger;

    public ApiEmailService(HttpClient httpClient, IOptions<EmailServiceOptions> options,
        ILogger<ApiEmailService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<EmailResult> SendEmailAsync(string subject, string message, string fromName, string fromEmail)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(_options.SendEmailUrl, request);
            return response.IsSuccessStatusCode
                ? EmailResult.Ok("Email sent successfully.")
                : EmailResult.Fail(errorMessage);
        }
        catch (HttpRequestException) { return EmailResult.Fail("Network error."); }
        catch (TaskCanceledException) { return EmailResult.Fail("Request timed out."); }
    }
}
```

### DI Registration (Program.cs)

```csharp
// Backend-calling services (scoped — new per circuit)
builder.Services.AddScoped<IEmailService, ApiEmailService>();
builder.Services.AddScoped<IChatbotService, ChatbotService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();

// Data-only services
builder.Services.AddScoped<ProjectService>();
builder.Services.AddSingleton<PersonalService>();
```

---

## Data Models

### Records for Immutable Data

```csharp
public record ServiceInfo(string Title, string Description, string Icon);
public record ToolInfo(string Name, string Category, string IconClass);
```

### Classes with Validation for Forms

```csharp
public class ContactFormModel
{
    [Required, StringLength(100)] public string Name { get; set; }
    [Required, EmailAddress] public string Email { get; set; }
    [Required, StringLength(5000)] public string Message { get; set; }
}
```

### Factory Methods on Message Types

```csharp
public class ChatMessage
{
    public string Role { get; set; }
    public string Content { get; set; }

    public static ChatMessage User(string content) => new() { Role = "user", Content = content };
    public static ChatMessage Assistant(string content) => new() { Role = "assistant", Content = content };
}
```

---

## Naming Conventions

| Category | Pattern | Examples |
|----------|---------|---------|
| Components | `<Feature><Role>.razor` | `ProfileHeader`, `ProjectCard`, `BookingCalendar` |
| Services | `<Domain>Service.cs` | `ApiEmailService`, `ProjectService` |
| Interfaces | `I<Domain>Service.cs` in feature's `Services/` | `IEmailService`, `IChatbotService` |
| Options | `<Service>Options.cs` at feature root | `EmailServiceOptions`, `ChatbotOptions` |
| Parameters | PascalCase | `AvatarUrl`, `OnFilterChange` |
| CSS | Tailwind utility classes (kebab-case) | `bg-cloudzen-teal`, `font-ibm-plex` |

---

## Styling

- **Tailwind CSS v4** via CDN (no build pipeline)
- Brand colors: `cloudzen-teal` (#61C2C8), `cloudzen-blue` (#1b6ec2), `cloudzen-steel` (#2c194d)
- Custom fonts: `font-ibm-plex` (headings), `font-helvetica` (body)
- **Bootstrap Icons** via CDN
- Component-scoped CSS via `.razor.css` files where needed

> For the full color system, button hierarchy, and component styling patterns, see [UI Color & Design System](../06-patterns/02_ui_color_design_system.md).

---

## Component Guidelines

1. **Single responsibility** — one component, one purpose
2. **Parameters for data** — accept via `[Parameter]`, don't fetch internally
3. **EventCallback for events** — child notifies parent, parent owns state
4. **Keep pages thin** — pages are orchestrators, not implementors
5. **Services for data** — inject services for data access, not inline `@code`
6. **Responsive first** — mobile-first Tailwind classes

---

## Related Docs

- [Vertical Slice Architecture](VERTICAL_SLICE_ARCHITECTURE.md) — Feature folder structure and namespace conventions
- [Configuration](CONFIGURATION.md) — IOptions pattern, secrets strategy, local dev override
- [API Endpoints](API_ENDPOINTS.md) — Backend endpoints that services call
- [Azure Functions](AZURE_FUNCTIONS.md) — API backend architecture
- [UI Color & Design System](../06-patterns/02_ui_color_design_system.md) — Color palette, button hierarchy, styling patterns
- [Azure Functions Proxy Pattern](../06-patterns/01_azure_functions_proxy_api.md) — How WASM ↔ API communication works

---

*Last Updated: March 2026*
