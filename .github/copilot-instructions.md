# Copilot Instructions for CloudZen

## Build & Run

```bash
# Frontend (Blazor WASM) — from repo root
dotnet build
dotnet run

# Backend (Azure Functions) — from Api/
cd Api
dotnet build
func start
```

No test projects or linters are configured.

## Architecture

**Blazor WebAssembly (.NET 8)** portfolio site with an **Azure Functions (Isolated Worker, .NET 8)** backend. Deployed as an **Azure Static Web App** with the Functions app linked — `/api` routes are automatically proxied to Functions in production.

### Frontend → Backend Communication

The WASM client calls two Azure Functions endpoints via `HttpClient`:

- `POST /api/send-email` — contact form emails (Brevo SMTP via MailKit)
- `POST /api/chat` — AI chatbot (proxies to Anthropic Claude API)

API keys and secrets live **only** in the Functions backend (Azure Key Vault). The WASM client never holds secrets.

### Key Directories

- **`Services/`** — Client-side services injected via DI. Services that call the backend (`ApiEmailService`, `ChatbotService`) are async and return result types (`EmailResult`, `ChatResult`). Data-only services (`ProjectService`, `PersonalService`, `ToolService`) are synchronous with in-memory data.
- **`Api/Functions/`** — Azure Functions HTTP triggers. Each function validates input (`Api/Security/InputValidator`), applies rate limiting (`Api/Services/RateLimiterService` using Polly), and adds security headers + correlation IDs.
- **`Shared/`** — Razor components organized by feature: `Landing/`, `Profile/`, `Projects/`, `Chatbot/`, `Common/`.
- **`Models/Options/`** — Strongly-typed configuration classes used with the `IOptions<T>` pattern.

### Pages

The app has two pages (`Pages/Index.razor` at `/`, `Pages/Contact.razor` at `/contact`) plus a routable component (`Shared/Profile/WhoIAm.razor` at `/whoiam`). Pages are thin orchestrators that compose `Shared/` components.

## Conventions

### Service Pattern

Services use constructor-injected `HttpClient`, `IOptions<T>`, and `ILogger<T>`. Backend-calling services return result objects with factory methods instead of throwing exceptions:

```csharp
public async Task<EmailResult> SendEmailAsync(...) {
    // ... returns EmailResult.Ok() or EmailResult.Fail(errorMessage)
}
```

### Configuration (IOptions)

All configuration uses `IOptions<T>` bound in `Program.cs` via `.BindConfiguration()`. Options classes define a `const string SectionName` and computed URL properties:

```csharp
public class ChatbotOptions {
    public const string SectionName = "ChatbotService";
    public string ApiBaseUrl { get; set; } = "/api";
    public string ChatEndpoint { get; set; } = "chat";
    public string ChatUrl => $"{ApiBaseUrl.TrimEnd('/')}/{ChatEndpoint}";
}
```

In local development, `Program.cs` overrides API base URLs to `http://localhost:7257/api` because Blazor WASM can't reliably load `appsettings.Development.json`.

### Component Architecture

Components follow a parent/child composition model — parent orchestrator components hold state and pass data down via `[Parameter]` properties, children communicate up via `EventCallback<T>`. No centralized state management library is used.

### NuGet Versioning

NuGet package versions are managed centrally in `Directory.Packages.props` (Central Package Management). Don't add `Version` attributes in `.csproj` files.

### Styling

- **Tailwind CSS v4** loaded via CDN (no npm/PostCSS build pipeline).
- Custom brand colors and fonts are configured inline in `wwwroot/index.html` via `tailwind.config`.
- Key brand colors: `cloudzen-teal` (#61C2C8), `cloudzen-blue` (#1b6ec2), `cloudzen-steel` (#2c194d), plus a full `teal-cyan-aqua-{50-950}` scale.
- Custom fonts: `font-ibm-plex` (headings/CTAs), `font-helvetica` (body).
- **Bootstrap Icons** via CDN for iconography.

### Models

- Records for simple immutable data (`ServiceInfo`, `ToolInfo`).
- Classes with data annotation validation for form models (`ContactFormModel`, `BookingFormModel`).
- Factory methods on message types (`ChatMessage.User()`, `ChatMessage.Assistant()`).

### Naming

- Services: `<Domain>Service.cs` with interface `I<Domain>Service.cs` in `Services/Abstractions/`
- Options: `<Service>Options.cs` in `Models/Options/`
- Components: `<Feature><Role>.razor` (e.g., `ProjectCard`, `ProfileHeader`)

### Security (API Layer)

The Functions backend applies input validation (XSS pattern detection via `InputValidator`), per-client rate limiting (Polly fixed-window, 10 req/60s default), CORS origin checks, and security headers on all responses.

### Documentation (AI-Model-Ready)

All documentation in `docs/` follows the AI-Model-Ready pattern defined in `.github/skills/ai-ready-docs/SKILL.md`. Key rules: metadata block at top, table of contents, quick reference table, scope boundaries, no emoji in headings, ASCII-safe characters, tables for structured data. Use the `ai-ready-docs` skill when creating or reviewing documentation.
