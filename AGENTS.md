# AGENTS.md — CloudZen AI Instructions

> Repository-specific rules for AI agents working in CloudZen.

## 1. Project Context

CloudZen is a **Blazor WebAssembly** frontend (`CloudZen.csproj`) plus an **Azure Functions Isolated Worker** API backend under `Api/`.

- Frontend: .NET 8 WASM SPA
- Backend: Azure Functions v4 (.NET 8, isolated)
- Deployment: Azure Static Web Apps + Azure Functions
- Styling: Tailwind (CDN) + component-scoped CSS

## 2. Architecture Style

CloudZen uses **vertical slice architecture by feature**, not classic layer-first folders.

### Frontend (WASM)

```
Features/
  Booking/   Contact/   Chat/   Landing/   Profile/   Projects/   Tickets/
Common/
Layout/
Pages/
```

Each feature owns its `Components/`, `Models/`, and `Services/` folders.

### Backend (API)

```
Api/
  Features/Booking
  Features/Contact
  Features/Chat
  Shared/Security
  Shared/Services
  Shared/Models
```

Use the **WASM -> Azure Function -> external provider** proxy pattern for sensitive operations.

## 3. Feature Classification

### Full-stack slices (WASM + API)

- Booking
- Contact
- Chat

### Frontend-only slices

- Landing
- Profile
- Projects
- Tickets

Only full-stack slices should add/modify Azure Function endpoints.

## 4. Component Rules (MANDATORY)

Use code-behind for component logic.

```
ComponentName.razor
ComponentName.razor.cs
ComponentName.razor.css   (when needed)
```

Rules:

1. Keep `.razor` focused on markup.
2. Put state, lifecycle, handlers, and service calls in `.razor.cs`.
3. Prefer Tailwind utilities first; use `.razor.css` for advanced/isolated styling.
4. Use `[Parameter]` for parent-to-child data and `EventCallback<T>` for child-to-parent events.
5. Keep Pages thin orchestration shells.

## 5. Service & DI Rules

- Register frontend feature services in root `Program.cs`.
- Use interfaces for injectable services (`IEmailService`, `IChatbotService`, `IBookingService`, etc.).
- Backend-calling services use `HttpClient` + options classes.
- Data-only services remain in feature service layer; avoid UI/business coupling.
- Do not place secrets in WASM client configuration.

## 6. Configuration Rules

Use strongly typed options classes:

- `EmailServiceOptions`
- `ChatbotOptions`
- `BookingServiceOptions`
- `BlobStorageOptions`

Bind via `AddOptions<T>().BindConfiguration(...)`.

In development, local API base URL overrides in `Program.cs` are allowed for local Functions ports.

## 7. API & Security Rules (CRITICAL)

All API endpoints are under `/api/*` and must preserve the existing security baseline:

1. Input validation and sanitization via shared validators.
2. Rate limiting (Polly-based) per endpoint/client context.
3. Security headers and proper CORS behavior.
4. Correlation IDs in logs.
5. Secrets from environment/Key Vault only.
6. No secret, token, or PII leakage in logs or client responses.

## 8. Endpoint Ownership

Current API feature endpoints:

- `/api/send-email`
- `/api/chat`
- `/api/book-appointment`

If you add endpoints, place them under the matching `Api/Features/{Feature}` folder and document request/response contracts.

## 9. Model Ownership Across Projects

WASM and API are separate projects. DTO duplication is acceptable when needed.

- Do not force direct project references between WASM and API to share request models.
- Keep transformation logic in API proxy functions when external systems require different field names.

## 10. Namespace & Naming Conventions

- Namespace root: `CloudZen.*`
- Mirror folder paths in namespaces (feature-first).
- File names should reflect role (`*Service`, `I*Service`, `*Options`, `*Function`).
- Prefer explicit, intention-revealing names.

## 11. Documentation Synchronization (MANDATORY)

When behavior changes, update docs in `docs/` alongside code.

Primary architecture docs:

- `docs/01-architecture/VERTICAL_SLICE_ARCHITECTURE.md`
- `docs/01-architecture/COMPONENT_ARCHITECTURE.md`
- `docs/01-architecture/AZURE_FUNCTIONS.md`
- `docs/01-architecture/API_ENDPOINTS.md`

Also keep feature and security docs in sync under:

- `docs/03-features/`
- `docs/04-security/`
- `docs/05-troubleshooting/`
- `docs/06-patterns/`

## 12. Quality and Safety Guardrails

1. Preserve existing feature isolation (avoid unnecessary cross-feature coupling).
2. Keep frontend secrets-free; route privileged operations through API.
3. Prefer incremental, low-blast-radius changes over broad rewrites.
4. Maintain nullable-enabled, compile-safe C#.
5. Follow existing patterns before introducing new abstractions.

## 13. Source-of-Truth Files to Check Before Major Changes

- `README.md`
- `Program.cs`
- `docs/01-architecture/*`
- `Api/Program.cs`
- `.github/copilot-instructions.md`
