# CLAUDE.md — Claude-Specific AI Instructions

> These instructions extend AGENTS.md with guidance optimized for Claude's reasoning capabilities.

## Project Context

Read **AGENTS.md** first for full project context. This file adds Claude-specific guidance for:

- Structured reasoning about architecture decisions
- Step-by-step SOLID analysis during refactoring
- Chain-of-thought for complex handler design
- Security review methodology

---

## Reasoning Approach

### Architectural Decisions

When making architectural decisions, reason through this checklist:

1. **Which layer does this belong to?** Map the change to Presentation / Application / Domain / Infrastructure.
2. **Does it violate dependency direction?** Inner layers must never reference outer layers.
3. **Which pattern applies?** Strategy (external providers), Repository (data access), MediatR (business operations), Event Bus (side effects).
4. **What are the SOLID implications?**
   - SRP: Does this class have one reason to change?
   - OCP: Can this be extended without modifying existing code?
   - LSP: Are subtypes substitutable?
   - ISP: Is the interface focused (like `IChargeable` vs a god interface)?
   - DIP: Are we depending on abstractions?

### Refactoring

When refactoring existing code, think step-by-step:

1. **Identify the smell.** Name the specific code smell or violation.
2. **Trace dependencies.** Map what depends on the code being changed.
3. **Evaluate SOLID impact.** Which principles are violated? Which will the refactoring satisfy?
4. **Plan the migration.** Backward compatibility matters — ensure existing consumers are not broken.
5. **Verify the invariants.** After refactoring, do business rules still hold? (domain constraints, audit trail, no PII logging)

---

## MediatR Handler Design

When designing or modifying MediatR handlers, use chain-of-thought through this flow:

```
1. Define the Command/Query record
   → What data does the caller provide?
   → Use records with init properties for immutability

2. Define the Response
   → Success case: what does the caller need back?
   → Failure case: use Result<T> pattern or throw domain exceptions?

3. Implement the Handler
   → Validate input (FluentValidation or guard clauses)
   → Resolve strategy via factory if needed
   → Execute operation via strategy interface
   → Persist state change via repository interface
   → Publish domain event via IEventBus
   → Return response

4. Register (automatic via assembly scanning in Program.cs)
```

**Example thought process for a new "CancelOrder" handler:**

> The cancel operation needs: order ID, cancellation reason, and the actor performing it.
> It should verify the order is in a cancellable state (e.g., "Pending" or "InProgress").
> The strategy must implement `ICancellable` (ISP — don't add to existing interfaces).
> After cancellation, publish an `OrderCancelledEvent` via `IEventBus`.
> Update the order status and persist.
> Return the updated order state.

---

## Code Generation Rules

### C# Code

- **Always use explicit type annotations.** Prefer `Order order` over `var order` for domain types. `var` is acceptable for obvious types (`var list = new List<string>()`).
- **File-scoped namespaces.** Always `namespace ProjectName.Features.Orders;` — never block-scoped.
- **Nullable enabled.** Use `string?` for nullable, never `string` for potentially null values.
- **Sealed by default.** Add `sealed` to classes not designed for inheritance.
- **Records for DTOs.** Commands, queries, and response models should be `record` types.
- **Primary constructors** for simple DI injection in handlers.
- **Cancellation tokens.** Every async method accepts and propagates `CancellationToken`.

### Blazor Components

**Always generate all three files** for every component:

```csharp
// ComponentName.razor — Markup only
@page "/route"
@using Microsoft.Extensions.Localization
@inject IStringLocalizer<SharedResource> L

<div class="component-wrapper">
    <h1>@L["PageTitle"]</h1>
</div>

// ComponentName.razor.cs — Logic
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace ProjectName.Components.Pages;

public sealed partial class ComponentName
{
    [Inject] private IStringLocalizer<SharedResource> L { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        // Load data via IMediator
    }
}

// ComponentName.razor.css — Scoped styles
.component-wrapper {
    /* Bootstrap 5 utilities + custom overrides */
}
```

---

## Security Review Methodology

When reviewing code for security, systematically evaluate each OWASP Top 10 category:

| # | Category | What to Check |
|---|---|---|
| A01 | Broken Access Control | Is `[Authorize]` on every endpoint? Policy-based, not role strings? |
| A02 | Cryptographic Failures | Secrets in code? PII in logs? TLS enforced? |
| A03 | Injection | Parameterized queries? No string concatenation in SQL/commands? |
| A04 | Insecure Design | Threat model reviewed? Business logic bypasses? |
| A05 | Security Misconfiguration | HTTPS? HSTS? Antiforgery? Debug disabled in prod? |
| A06 | Vulnerable Components | NuGet packages up to date? Known CVEs? |
| A07 | Auth Failures | Token validation? Brute-force protection? Session management? |
| A08 | Data Integrity Failures | Deserialization safe? Pipeline integrity? |
| A09 | Logging Failures | Audit trail present? Correlation IDs? No secrets in logs? |
| A10 | SSRF | External URL validation? Allowlisting? |

For each finding, provide:
- **Severity:** Critical / High / Medium / Low
- **Location:** File and line reference
- **Issue:** What's wrong
- **Fix:** Specific code change

---

## Immutability Preferences

Claude should favor immutable constructs wherever possible:

- `record` over `class` for data transfer objects
- `readonly` fields in services and handlers
- `init` properties on models where mutation is not required
- `IReadOnlyCollection<T>` and `IReadOnlyList<T>` for collection returns
- `sealed` classes to prevent unintended inheritance
- Expression-bodied members for single-line logic

---

## Documentation Updates

When modifying features, check and update the corresponding doc in `docs/`:

| Feature Area | Doc to Update |
|---|---|
| Cross-cutting / architecture | `00-Architecture-Overview` |
| Feature-specific logic | `NN-Feature-Name` (matching doc) |
| New external provider | Strategy pattern documentation |
| Identity / auth changes | Authentication/authorization docs |
| Event bus changes | Event bus / domain events docs |
| Localization changes | Localization docs |
| UI components | UI component docs |
| API endpoints | API integration docs |

If no doc exists for a new feature, create one following the `NN-Feature-Name` convention.

---

## Domain Model Reference

Define your project's key entities and their relationships here. Example structure:

```
Order (Aggregate Root)
├── Id (Guid, PK)
├── CustomerId (Guid, required)         — the buyer
├── Amount (Money, required)            — order total as value object
├── Status (OrderStatus)                — Pending → InProgress → Completed | Cancelled
├── Description (string)                — what the order is for
├── CreatedAt (DateTimeOffset)          — UTC timestamp
└── CompletedAt (DateTimeOffset?)       — set when Status = Completed

Money (Value Object)
├── Amount (decimal)
└── Currency (string)

OrderStatus (Enum)
├── Pending
├── InProgress
├── Completed
└── Cancelled
```

---

## Error Handling Guidance

- Use domain-specific exceptions for business rule violations (e.g., `InvalidOrderStateException`).
- Handlers catch infrastructure exceptions and translate to meaningful domain errors.
- Global exception middleware handles unhandled exceptions for API endpoints.
- Never swallow exceptions silently — log with context and correlation IDs.
- Return appropriate HTTP status codes: 400 for validation, 404 for not found, 409 for conflicts, 500 for unexpected.

---

## Skills Catalog

See **AGENTS.md → Skills Catalog** for the complete skill loading instructions, categories,
and usage examples. Skills are universal across all models.

### Claude Code Integration (`/skills`)

All skills are registered as **Claude Code skills** in `.claude/skills/`. They appear in
`/skills` and can be invoked via `/skill-name` (e.g., `/owasp-audit`, `/code-reviewer`).

Each `.claude/skills/{name}/SKILL.md` is a **bridge file** — it registers the skill with
Claude's discovery system and redirects to the full universal definition in `.github/skills/`.

**How it works:**
1. User types `/owasp-audit` → Claude loads `.claude/skills/owasp-audit/SKILL.md`
2. Bridge tells Claude to read `.github/skills/owasp-audit/SKILL.md`
3. Claude follows the Core Workflow + loads references on demand

**Architecture:** `.claude/skills/` = Claude registration layer → `.github/skills/` = universal source of truth

### Quick Reference

| Invoke | Full Skill Path |
|--------|----------------|
| `/code-reviewer` | `.github/skills/code-reviewer/SKILL.md` |
| `/owasp-audit` | `.github/skills/owasp-audit/SKILL.md` |
| `/test-generator` | `.github/skills/test-generator/SKILL.md` |
| `/architecture-reviewer` | `.github/skills/architecture-reviewer/SKILL.md` |
| `/authentication` | `.github/skills/authentication/SKILL.md` |
| `/agent-orchestrator` | `.github/skills/agent-orchestrator/SKILL.md` |
| Full catalog | `.github/skills/CATALOG.md` |
