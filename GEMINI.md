# GEMINI.md — Gemini-Specific AI Instructions

> These instructions extend AGENTS.md with guidance optimized for Gemini's analysis and code generation capabilities.

## Project Context

Read **AGENTS.md** first for full project context. This file adds Gemini-specific guidance for:

- Dependency graph analysis before changes
- Pattern matching against existing codebase conventions
- Efficient code search and cross-referencing
- Database and EF Core query generation

---

## Exploration Strategy

### Before Making Changes — Map Dependencies First

When asked to modify any code, analyze the dependency graph before writing:

1. **Trace inbound references.** What calls/imports the file being changed?
2. **Trace outbound references.** What does the file depend on?
3. **Identify the layer.** Presentation → Application → Domain ← Infrastructure.
4. **Check for pattern consistency.** How do similar files in the same directory handle this?
5. **Verify interface contracts.** If changing an interface, identify all implementations and consumers.

**Example:** Before modifying `IOrderRepository`:
- Find all classes implementing it (e.g., `OrderRepository`)
- Find all consumers (e.g., `CreateOrderHandler`, `CompleteOrderHandler`, etc.)
- Verify the change doesn't break the Repository Pattern boundary
- Check if `AppDbContext` needs a corresponding migration

### Cross-Referencing Checklist

When exploring the codebase:

| Question | Where to Look |
|---|---|
| How is DI wired? | `Program.cs` — service registration section |
| What strategies exist? | `Services/Strategies/` — `IPaymentProcessor` implementations |
| What MediatR slices exist? | `Features/{Domain}/` — each subdirectory is a vertical slice |
| What domain events exist? | `Events/` — `DomainEvent` subclasses |
| What's the DB schema? | `Models/` entities, `Data/AppDbContext.cs` |
| What API endpoints exist? | `Features/{Domain}/Api/` or `Api/` — controller classes |
| What localization keys exist? | `Resources/SharedResource.resx` and locale-specific `.resx` files |

---

## Code Generation Guidelines

### Match Existing Patterns

Before generating code, find and match the project's established patterns:

**MediatR Handler Pattern** (reference: any `Features/{Domain}/{Slice}/` directory):
```
Command record → Handler class → injects repository + strategy factory + IEventBus
```

**Strategy Pattern** (reference: `Services/Strategies/`):
```
IPaymentProcessor (marker) + capability interfaces (IChargeable, IRefundable, ICancellable)
```

**Blazor Component Pattern** (reference: any `Components/Pages/{Component}.*`):
```
.razor  — markup with @inject IStringLocalizer<SharedResource> L
.razor.cs — sealed partial class with [Inject] properties
.razor.css — scoped styles using Bootstrap 5
```

**Repository Pattern** (reference: `Data/Repositories/`):
```
Interface in Data/Repositories/ → Implementation uses DbContext internally
```

### Code Style Rules

- File-scoped namespaces: `namespace ProjectName.Features.Orders;`
- Nullable reference types enabled throughout
- `sealed` on concrete classes not designed for inheritance
- `record` types for commands, queries, and DTOs
- Async/await with `CancellationToken` propagation
- Guard clauses at method entry — fail fast
- No `var` for domain types — use explicit types for clarity

---

## Database & EF Core Guidance

### Before Writing Queries

1. **Check existing repository methods.** Repository interfaces define available data operations (e.g., `GetByIdAsync`, `AddAsync`, `UpdateAsync`).
2. **Examine `AppDbContext`** for configured relationships, indexes, and conventions.
3. **Match existing query patterns.** Use `AsNoTracking()` for read-only queries. Use projections to avoid loading full entities.
4. **Check for existing migrations** in `Migrations/` before creating new ones.

### Query Rules

- Always use EF Core parameterized queries — never raw SQL string concatenation.
- Read queries: `AsNoTracking()` for performance.
- Writes: load entity → modify → `SaveChangesAsync()` inside the repository.
- New columns or tables: create a migration with `dotnet ef migrations add MigrationName`.
- Database-specific: check `Program.cs` for the configured provider (e.g., `UseNpgsql()`, `UseSqlServer()`).

---

## Feature Modification Workflow

When adding or modifying a feature:

```
1. Identify the vertical slice in Features/{Domain}/
2. Check the corresponding doc in docs/
3. Map dependencies (repository, strategy, events)
4. Make changes following existing patterns
5. Update the docs/ entry
6. Verify DI registration in Program.cs if new services are added
7. Add/update localization keys in Resources/ if UI text changes
```

---

## UI Component Analysis

When working with Blazor components:

1. **Inspect the component triad.** Always check all three files (`.razor`, `.razor.cs`, `.razor.css`).
2. **Check parent-child relationships.** Look at `[Parameter]` and `EventCallback<T>` usage.
3. **Verify localization.** All user-facing strings should use `@L["Key"]` in markup or `L["Key"]` in code-behind.
4. **Check scoped CSS.** Styles must be in the `.razor.css` file — no global overrides for component-specific elements.
5. **Bootstrap 5 consistency.** Match existing component patterns for layout (containers, rows, cols) and utilities.

### Component Inventory

When onboarding to a project, catalog existing components:

| What to Find | Where |
|---|---|
| Page components | `Components/Pages/` — routable components with `@page` |
| Layout components | `Layout/` — `MainLayout`, `NavMenu`, etc. |
| Shared components | `Components/Shared/` — reusable building blocks |
| Feature components | `Components/Features/` — domain-specific UI |

---

## Business Rules — Quick Reference

> **Define your domain-specific business rules per project.** These are non-negotiable invariants
> that every code change must respect. Verify compliance on every change.

Example rules to verify:

| Rule | Rationale |
|---|---|
| Domain events after persistence | Events must reflect committed state, not intent |
| Validate all input at boundaries | Prevents invalid state from entering the domain |
| Never log PII/tokens/secrets | Regulatory compliance (GDPR, etc.) |
| Idempotency on external calls | Prevents duplicate operations on retry |
| State machine transitions enforced | Aggregates reject invalid state changes |
| Authorization on every endpoint | Default deny — no anonymous business operations |

---

## Documentation Maintenance

When features change, update the corresponding doc in `docs/`:

```
docs/
├── 00-Architecture-Overview    ← cross-cutting changes
├── 01-Feature-Name             ← feature-specific changes
├── 02-Feature-Name             ← one doc per feature area
└── ...                         ← follow numbering convention
```

New features that don't fit existing docs: create the next numbered doc (e.g., `NN-Feature-Name`).

---

## Program.cs Service Registration Reference

Key DI registrations (keep in sync when adding services):

```csharp
// Data Layer
services.AddDbContext<AppDbContext>(/* database provider */);
services.AddScoped<IOrderRepository, OrderRepository>();

// Event Bus
services.AddScoped<IEventBus, InMemoryEventBus>();

// Strategies
services.AddScoped<IPaymentProcessor, StripePaymentProcessor>();
services.AddScoped<IPaymentProcessorFactory, PaymentProcessorFactory>();

// MediatR (auto-discovers handlers)
services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
```

When adding a new service or strategy, register it in `Program.cs` following this pattern.

---

## Skills Catalog

See **AGENTS.md → Skills Catalog** for the complete skill loading instructions, categories,
and usage examples. Skills are universal across all models.

**Quick start:** `cat .github/skills/CATALOG.md` to browse all available skills.
