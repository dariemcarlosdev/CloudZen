# Copilot Instructions — Project Conventions

> Master project-level instructions for GitHub Copilot and all AI coding assistants.
> This file defines coding conventions, architecture patterns, and best practices
> for .NET / Blazor projects. Customize the examples to match your domain.

## Project

This file defines the **coding conventions and architecture standards** for this .NET / Blazor project. All AI assistants working on this codebase should follow these patterns.

**Tech Stack:**
- .NET 10, Blazor Server (interactive SSR)
- PostgreSQL with EF Core (Npgsql)
- MediatR (CQRS vertical slices)
- Bootstrap 5 (enterprise LOB UI)
- IStringLocalizer with .resx files (en-US, es-MX)

---

## Architecture

**Clean Architecture** with **CQRS** organized as vertical slices.

### Layer Map

```
Presentation     Components/              Blazor pages, layouts, scoped CSS
Application      Features/{Domain}/       MediatR command/query handlers
Domain           Models/                  Entities, value objects, enums
                 Events/                  DomainEvent, IEventBus, domain event classes
                 Services/Strategies/     Strategy interfaces (IChargeable, IRefundable, etc.)
Infrastructure   Data/                    AppDbContext, repository implementations
                 Services/               External service integrations
                 Infrastructure/Auth/     Authentication handlers
                 Infrastructure/Middleware/ Exception handling, logging middleware
```

### Dependency Direction — MANDATORY

```
Components/ ──→ Features/ ──→ Models/    ←── Data/
                              Events/    ←── Services/
                              Strategies/ ←── Infrastructure/
```

Inner layers (Models, Events, Strategies) **never** reference outer layers. Infrastructure implements domain interfaces.

---

## Design Patterns

| Pattern | Where | Purpose |
|---|---|---|
| **Strategy** | `Services/Strategies/` | External provider abstraction (e.g., payment, notification) |
| **Repository** | `Data/Repositories/` | Data access abstraction — EF Core hidden from business logic |
| **Factory** | `IStrategyFactory` | Runtime resolution of strategy implementation by provider name |
| **Event Bus** | `Events/IEventBus` | Decouple side effects from business operations |
| **MediatR/CQRS** | `Features/{Domain}/` | Separate command (write) and query (read) paths |
| **Vertical Slice** | `Features/{Domain}/*/` | Each feature is a self-contained slice: command + handler + (optional validator) |

### Strategy Interfaces (ISP-Compliant)

```csharp
IPaymentProcessor               // Marker — every provider implements this
├── IChargeable                  // ChargeAsync(amount, paymentMethodId, idempotencyKey)
├── IRefundable                  // RefundAsync(transactionReference, idempotencyKey)
└── ICancellable                 // CancelAsync(transactionReference, idempotencyKey)
```

Providers implement only the capabilities they support. One provider may implement all three; another might only implement `IChargeable`.

---

## Blazor Rules — MANDATORY

### Code-Behind Pattern (Always)

Every component produces **three files**:

```
ComponentName.razor       ← Markup only. No @code {} blocks. Ever.
ComponentName.razor.cs    ← sealed partial class. All logic here.
ComponentName.razor.css   ← Scoped CSS. Bootstrap 5 + custom overrides.
```

### Component Conventions

- Inject services via `[Inject]` in code-behind — not `@inject` in markup (markup `@inject` is acceptable for `IStringLocalizer` only).
- Use `IStringLocalizer<SharedResource>` for all user-facing text.
- Use `IMediator` for all data operations — never call repositories or services directly from components.
- Use `[CascadingParameter] Task<AuthenticationState>` for auth state.
- Implement `IDisposable` / `IAsyncDisposable` when using event handlers or JS interop.
- Override `OnInitializedAsync` for data loading — not the constructor.

---

## Security — OWASP Top 10

| Category | Requirement |
|---|---|
| **Broken Access Control** | `[Authorize]` on every endpoint. Policy-based auth (`"ApiAccess"`). Default deny. |
| **Cryptographic Failures** | Secrets via env vars or Key Vault. Never in source or `appsettings.json`. |
| **Injection** | Parameterized queries only (EF Core). No raw SQL string concatenation. |
| **Insecure Design** | Strategy Pattern enforces external provider boundaries. |
| **Security Misconfiguration** | HTTPS + HSTS enforced. Antiforgery tokens. Swagger only in Development. |
| **Vulnerable Components** | Keep NuGet packages updated. Monitor for CVEs. |
| **Auth Failures** | Validate credentials on every request. Use policy-based auth. |
| **Logging Failures** | Structured logging. Correlation IDs. **Never log PII, tokens, or secrets.** |

---

## Business Operation Rules — MANDATORY

1. **Idempotency keys** on every write operation that calls external services. All strategy methods require an `idempotencyKey` parameter.
2. **Domain events after persistence** — publish domain events (e.g., `OrderCreatedEvent`, `OrderCompletedEvent`) only after `SaveChangesAsync`.
3. **State machine integrity** — enforce valid status transitions in the domain model. Invalid transitions throw domain exceptions.
4. **External references** — store provider-specific IDs (e.g., payment intent ID, tracking number) on the entity for reconciliation.
5. **Never modify monetary amounts** after initial creation. Amounts flow from the domain model to external providers — no manual arithmetic.
6. **Audit trail** — every state transition must be traceable via domain events.

---

## CQRS Flow

All business operations go through MediatR:

```
UI/API  ──→  IMediator.Send(Command/Query)
               │
               ▼
         Handler (Features/{Domain}/*/Handler.cs)
               │
               ├──→ Validate input
               ├──→ Resolve strategy (IStrategyFactory)
               ├──→ Execute operation (IChargeable, etc.)
               ├──→ Persist via repository interface
               ├──→ Publish domain event (IEventBus)
               └──→ Return result
```

### Example Slices

| Slice | Type | Purpose |
|---|---|---|
| `CreateOrder/` | Command | Create a new order with initial validation |
| `CompleteOrder/` | Command | Mark order as completed and trigger side effects |
| `CancelOrder/` | Command | Cancel an order and initiate reversal if needed |
| `GetOrder/` | Query | Read single order by ID |
| `ListOrders/` | Query | List orders with filtering and pagination |

---

## Code Conventions

| Convention | Rule |
|---|---|
| Namespaces | File-scoped (`namespace ProjectName.X;`) |
| Nullability | Enabled — use `string?` for nullable |
| Inheritance | `sealed` by default on concrete classes |
| DTOs | `record` types with `init` properties |
| Async | `async Task` / `async Task<T>` with `CancellationToken` |
| Naming | Intention-revealing. No abbreviations except DTO, ID, HTTP. |
| Guard clauses | Fail fast at method entry — no deep nesting |
| Constants | No magic strings or numbers — use `const` or `enum` |

---

## Localization

- **Resource files:** `Resources/SharedResource.resx` (en-US default), `SharedResource.es.resx` (es-MX)
- **Component resources:** `Resources/Components/` for component-specific strings
- **Injection:** `IStringLocalizer<SharedResource>` in code-behind files
- **Markup:** `@L["KeyName"]` for localized strings
- **Culture switch:** `GET /culture/set?culture={code}&redirectUri={path}` — cookie-based
- **All user-facing strings must be localized** — no hardcoded text in `.razor` or `.razor.cs` files

---

## Data Model (Example)

```
Order
├── Id                  int (PK, auto-increment)
├── CustomerId          string (required)
├── Amount              decimal (required)
├── Description         string (required)
├── Status              string — "Pending" | "Processing" | "Completed" | "Cancelled"
├── ExternalReference   string? — external provider transaction ID
├── ExternalProvider    string? — e.g., "Stripe", "PayPal"
└── CreatedAt           DateTime (UTC)
```

Replace `Order` with your domain's aggregate root entity. Add fields as needed for your domain.

---

## Documentation — MANDATORY

Update `docs/` when features change. Maintain numbered documentation files:

```
00-Architecture-Overview    Cross-cutting architecture
01-Feature-Name             Feature-specific workflow docs
02-Feature-Name             ...
```

Each feature should have a corresponding doc. New features without a matching doc → create the next numbered file (e.g., `03-Feature-Name`).

---

## DI Registration (Program.cs)

When adding new services, register them in `Program.cs` following existing patterns:

```csharp
// Repository
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Strategy (new provider implementation)
builder.Services.AddScoped<IPaymentProcessor, StripePaymentProcessor>();

// Event handler
// (auto-discovered by MediatR if implementing INotificationHandler<T>)

// New service
builder.Services.AddScoped<INewService, NewService>();
```

MediatR handlers are auto-discovered — no manual registration needed.

---

## Agent Orchestration — MANDATORY

When delegating work to sub-agents (parallel or serial):

1. **ALWAYS present the delegation plan to the user** before spawning any agent.
2. **Use `ask_user`** to show: agent count, agent types, task descriptions, blast radius, estimated tokens.
3. **Wait for explicit approval** — do not assume approval from silence or prior permissions.
4. **Never spawn agents without the user seeing and approving the plan first.**

See `.github/skills/agent-orchestrator/SKILL.md` (Step 3) for the full approval gate workflow.

---

## Skills Catalog

See **AGENTS.md → Skills Catalog** for the complete skill loading instructions, categories,
and usage examples. Skills are universal across all models.

**Quick start:** Read `.github/skills/CATALOG.md` to browse all 36 skills across 11 categories.
