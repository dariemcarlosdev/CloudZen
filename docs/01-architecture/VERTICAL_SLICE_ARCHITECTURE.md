# Vertical Slice Architecture

CloudZen organizes code **by feature, not by layer**. Each feature owns its components, models, services, and configuration — everything needed to understand or modify a feature lives in one folder.

---

## Structure

```
Features/
├── Booking/           ← Full-stack: WASM + API
│   ├── Components/    7 Razor components (calendar, form, confirmation, etc.)
│   ├── Models/        BookingFormModel, BookingAppointmentRequest
│   ├── Services/      IBookingService, IAppointmentService, IGoogleCalendarUrlService + implementations
│   └── BookingServiceOptions.cs
│
├── Contact/           ← Full-stack: WASM + API
│   ├── Components/    ContactForm
│   ├── Models/        ContactFormModel, EmailApiRequest/Response/ErrorResponse
│   ├── Services/      IEmailService, ApiEmailService
│   └── EmailServiceOptions.cs
│
├── Chat/              ← Full-stack: WASM + API
│   ├── Components/    CloudZenChatbot (.razor, .razor.cs, .razor.css)
│   ├── Models/        ChatMessage
│   ├── Services/      IChatbotService, ChatbotService
│   └── ChatbotOptions.cs
│
├── Landing/           ← Frontend-only
│   ├── Components/    Hero, CTA, Services, Mission, CaseStudies, Testimonials, FeaturesShowcase, ToolsOverview + cards
│   ├── Models/        ServiceInfo, StandardInfo, FeatureHighlight, ToolInfo
│   └── Services/      IPersonalService, IMissionService, ICaseStudyService, IFeatureHighlightService, IToolService + implementations
│
├── Profile/           ← Frontend-only
│   ├── Components/    WhoIAm, ProfileHeader, ProfileApproach, ProfileHighlights, SDLCProcess
│   ├── Models/        SDLCStage
│   └── Services/      ResumeService
│
├── Projects/          ← Frontend-only
│   ├── Components/    ProjectCard, ProjectFilter
│   ├── Models/        ProjectInfo, ProjectParticipant
│   └── Services/      IProjectService, ProjectService
│
└── Tickets/           ← Frontend-only
    ├── Components/    Tickets
    ├── Models/        TicketDto
    └── Services/      ITicketService, TicketService

Common/
├── Components/        AnimatedCounterCircle, ScrollToTopButton
└── Options/           BlobStorageOptions

Layout/                MainLayout, Header, Footer (Blazor convention — stays at root)
Pages/                 Index.razor (/), Contact.razor (/contact)
```

### API Project (Azure Functions)

```
Api/
├── Features/
│   ├── Booking/       BookAppointmentFunction, BookAppointmentRequest
│   ├── Contact/       SendEmailFunction, EmailRequest, EmailSettings
│   └── Chat/          ChatFunction, ChatRequest, ChatResponse
├── Shared/
│   ├── Security/      InputValidator
│   ├── Services/      IRateLimiterService, PollyRateLimiterService
│   └── Models/        RateLimitOptions, RateLimitResult, RateLimitRejectionReason
└── Program.cs
```

---

## Namespace Convention

Namespaces mirror folder paths:

```
CloudZen.Features.{Feature}.Components   → Razor components
CloudZen.Features.{Feature}.Models       → Data models, DTOs
CloudZen.Features.{Feature}.Services     → Interfaces + implementations
CloudZen.Features.{Feature}              → Options classes (feature root)
CloudZen.Common.Components               → Shared UI components
CloudZen.Common.Options                  → Shared configuration
CloudZen.Api.Features.{Feature}          → API functions + models
CloudZen.Api.Shared.{Concern}            → Cross-cutting API infrastructure
```

All feature namespaces are registered globally in `_Imports.razor` — no per-component `@using` needed in Razor files.

---

## Feature Categories

| Category | Features | Has API Backend |
|----------|----------|:---:|
| **Full-stack** | Booking, Contact, Chat | ✅ |
| **Frontend-only** | Landing, Profile, Projects, Tickets | — |
| **Cross-cutting** | Common (WASM), Shared (API) | — |

Full-stack features follow the [Azure Functions Proxy Pattern](../06-patterns/01_azure_functions_proxy_api.md) — the WASM client calls `/api/*`, the Functions backend holds secrets and forwards to external services.

---

## Cross-Feature References

Most files only reference their own feature's namespaces. Known cross-feature dependencies:

| File | References | Reason |
|------|-----------|--------|
| `WhoIAm.razor.cs` (Profile) | Projects.Services, Projects.Models | Displays portfolio projects |
| `CaseStudies.razor.cs` (Landing) | Projects.Services, Projects.Models | Shows project case studies |
| `CTA.razor.cs` (Landing) | Booking.Services | Uses GoogleCalendarUrlService |
| `ResumeService.cs` (Profile) | Common.Options | Uses BlobStorageOptions |

---

## Adding a New Feature

1. Create `Features/{FeatureName}/` with `Components/`, `Models/`, `Services/` subfolders
2. Add Options class at feature root if configuration is needed
3. Register services in `Program.cs` with appropriate `using` statement
4. Add `@using CloudZen.Features.{FeatureName}.*` entries to `_Imports.razor`
5. If full-stack: create matching `Api/Features/{FeatureName}/` with Function + request model

---

## Cross-Project Model Duplication

Full-stack features (Booking, Contact, Chat) have **request models in both projects**:

| WASM Model | API Model | Why Both Exist |
|------------|-----------|----------------|
| `BookingAppointmentRequest` | `BookAppointmentRequest` | Same data, separate assemblies |
| `EmailApiRequest` | `EmailRequest` | Same pattern |
| *(inline anonymous object)* | `ChatRequest` | Chat builds payload inline |

**Why they can't be shared:**

- WASM compiles to **WebAssembly** (`net8.0-browser`), API runs on **.NET server** (`net8.0`). They are separate .NET projects with incompatible target frameworks — one cannot reference the other.
- A **shared class library** (`CloudZen.Shared`) targeting `netstandard2.1` or `net8.0` could hold DTOs both projects reference. This is the standard .NET solution but adds a third project to maintain.
- Current approach: each project owns its copy of the request model. The duplication is small (< 50 lines per model) and keeps each project self-contained.

**Important**: WASM models use user-friendly field names (`name`, `email`, `date`). External services (e.g., N8N) may expect different names (`userName`, `userEmail`, `appointmentDate`). The **Azure Function proxy is responsible for transforming** WASM field names to the external service's expected schema. See [Proxy Pattern — Model Ownership](../06-patterns/01_azure_functions_proxy_api.md#model-ownership--transformation).

---

## Rules

- **Feature isolation**: A feature should not depend on another feature's services. Use `Common/` for shared concerns. Profile→Projects and Landing→Projects are documented exceptions.
- **Options at feature root**: Each feature's `*Options.cs` lives at the feature folder root (not in a subfolder), since there's typically one per feature.
- **Layout/Pages stay at root**: Blazor routing requires `Pages/` and `Layout/` at the project root.
- **API mirrors WASM slices**: The 3 full-stack features use identical slice names in both projects for navigability.

---

## Related Docs

- [Component Architecture](COMPONENT_ARCHITECTURE.md) — Communication patterns, service layer, data models, naming conventions
- [Configuration](CONFIGURATION.md) — IOptions pattern, secrets strategy, options class inventory with file locations
- [API Endpoints](API_ENDPOINTS.md) — Full specs for the 3 API endpoints (Booking, Contact, Chat)
- [Azure Functions](AZURE_FUNCTIONS.md) — Hosting model, Program.cs setup, troubleshooting
- [Azure Functions Proxy Pattern](../06-patterns/01_azure_functions_proxy_api.md) — How full-stack features communicate (WASM → API → external service)
