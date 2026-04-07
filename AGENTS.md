# AGENTS.md — Project AI Instructions

> Universal instructions for all AI coding agents working on this repository.
> Adapt to your project's domain — replace generic examples with your actual entities, features, and business rules.

## Architecture Overview

This project follows **Clean Architecture** with **CQRS via MediatR** and **DDD**.

### Layer Map

```
Presentation    →  Components/           Blazor pages, layouts, scoped CSS
Application     →  Features/{Domain}/    MediatR command/query handlers (vertical slices)
Domain          →  Models/               Entities, value objects, enums
                   Events/               Domain events (DomainEvent, IEventBus)
                   Services/Strategies/  Strategy interfaces (e.g., IChargeable, IRefundable)
Infrastructure  →  Data/                 EF Core DbContext, repositories
                   Services/             External service implementations
                   Infrastructure/       Auth handlers, middleware
```

### Dependency Rules — MANDATORY

- **Inner layers NEVER reference outer layers.** Models/ and Events/ must not import from Data/, Services/, or Components/.
- Domain entities must not depend on EF Core, third-party SDKs, or ASP.NET Core types.
- Infrastructure implements domain interfaces — domain defines contracts, infrastructure fulfills them.
- Application layer (Features/) orchestrates via interfaces only — never instantiate infrastructure directly.

---

## CQRS & MediatR — MANDATORY

All business operations go through MediatR handlers in `Features/{Domain}/`:

| Slice | Command/Query | Purpose |
|---|---|---|
| `CreateOrder/` | `CreateOrderCommand` | Create a new aggregate instance |
| `CompleteOrder/` | `CompleteOrderCommand` | Transition aggregate to completed state |
| `CancelOrder/` | `CancelOrderCommand` | Cancel an in-progress aggregate |
| `GetOrder/` | `GetOrderQuery` | Read single aggregate by ID |
| `ListOrders/` | `ListOrdersQuery` | Read aggregate list with filtering |

**Rules:**
- UI components and API controllers dispatch commands/queries via `IMediator` — never call services directly.
- Handlers orchestrate: validate → execute strategy → persist via repository → publish domain events.
- Prefer MediatR vertical slices for all new work — avoid monolithic service classes.

---

## Design Patterns

### Strategy Pattern — External Providers

Business operations use ISP-compliant strategy interfaces in `Services/Strategies/`:

- `IPaymentProcessor` — marker interface; every provider implements this
- `IChargeable` — create charges (`ChargeAsync`)
- `IRefundable` — process refunds (`RefundAsync`)
- `ICancellable` — cancel pending operations (`CancelAsync`)
- `IPaymentProcessorFactory` — resolves the correct strategy at runtime

**OCP:** Adding a new provider means registering a new `IPaymentProcessor` implementation. Zero changes to existing code.

### Repository Pattern

- All data access goes through domain repository interfaces (e.g., `IOrderRepository`) in `Data/Repositories/`.
- **Never inject `DbContext` into Features/, Services/, or Components/ directly.**
- Repositories live in Infrastructure; interfaces live adjacent to domain.

### Event Bus

- `IEventBus` (in `Events/`) publishes `DomainEvent` subclasses (e.g., `OrderCreatedEvent`, `OrderCompletedEvent`).
- Current implementation: `InMemoryEventBus`. Swappable for MassTransit, Azure Service Bus, or other transports.
- Handlers publish events after successful state changes — never before persistence.

---

## Blazor Component Rules — MANDATORY

Every Blazor component **must** use the code-behind pattern with three files:

```
ComponentName.razor       — Markup only (no @code blocks)
ComponentName.razor.cs    — Partial class with logic
ComponentName.razor.css   — Scoped styles (Bootstrap 5 utilities + custom)
```

- **NEVER** use inline `@code {}` blocks in `.razor` files.
- **ALWAYS** create scoped CSS — never use global styles for component-specific elements.
- Use `[CascadingParameter] Task<AuthenticationState>` for auth — not `IHttpContextAccessor`.
- Use `IStringLocalizer<SharedResource>` for all user-facing strings.

---

## Localization

- Resource files: `Resources/SharedResource.resx` (default locale) and additional `.{culture}.resx` files.
- Inject `IStringLocalizer<SharedResource>` in code-behind files.
- All user-facing strings must be localized — no hardcoded UI text.
- Culture switch endpoint: `/culture/set?culture={code}&redirectUri={path}`.

---

## Business Rules

> **Define your domain-specific business rules here.** Every project has non-negotiable invariants.
> Document them in this section so all AI agents enforce them consistently.

Example rules to define per project:

1. **Data integrity:** Which state transitions are valid? Document the aggregate's state machine.
2. **Idempotency:** Which operations must be idempotent? Define idempotency key strategies.
3. **Audit trail:** Which state changes must emit domain events for traceability?
4. **Logging safety:** Never log PII, tokens, or secrets in any environment.
5. **Authorization:** Which operations require specific policies or claims?

---

## Security — OWASP Top 10 Mindset

- **A01 Broken Access Control:** Policy-based auth (`[Authorize(Policy = "...")]`). Default deny.
- **A02 Cryptographic Failures:** Secrets in environment variables or Azure Key Vault — never in code/config.
- **A03 Injection:** Parameterized queries via EF Core. Never concatenate user input into SQL.
- **A05 Security Misconfiguration:** HTTPS enforced, HSTS enabled, antiforgery tokens on state-changing requests.
- **A07 Auth Failures:** Validate authentication on every request. Use established identity providers.
- **A09 Logging Failures:** Structured logging with correlation IDs. Never log secrets or PII.

---

## Documentation — MANDATORY

Maintain a `docs/` directory with architectural documentation. Organize by feature area using a numbered convention:

```
docs/
├── 00-Architecture-Overview    ← Cross-cutting architecture and design decisions
├── 01-Feature-Name             ← Document each major feature
├── 02-Feature-Name             ← One doc per feature area
└── ...
```

**When you add or change a feature, update the corresponding doc.** If no doc exists for a new feature, create one following the numbering convention.

---

## Code Conventions

- File-scoped namespaces (`namespace X;`)
- Nullable reference types enabled
- `sealed` on classes not designed for inheritance
- `record` types for DTOs and command/query models
- Async/await everywhere — propagate `CancellationToken`
- Guard clauses over nested conditionals
- No magic strings — use constants or enums
- Intention-revealing names — no abbreviations except well-known acronyms (DTO, ID, HTTP)

---

## Skills Catalog — Universal (All Models)

> **These are markdown instruction files, not tool invocations.** Read them with your file
> tools (`cat`, `Read`, `view`, `Grep`) and follow the workflow steps inside. Do NOT try
> to "invoke", "call", or use any built-in Skill/Tool mechanism — just read the SKILL.md
> file and execute its Core Workflow as your action plan.

Reusable AI skills at `.github/skills/` — 41 skills across 11 categories.
These skills work identically across **GitHub Copilot CLI, Claude Code, Gemini, and any
AI assistant** that can read files.

### How to Use a Skill (Any Model)

```bash
# Step 1: Find the right skill
cat .github/skills/CATALOG.md

# Step 2: Read the skill core file
cat .github/skills/{skill-name}/SKILL.md

# Step 3: Follow the Core Workflow inside — it has numbered steps + checkpoints

# Step 4: When the Reference Guide table says to load a deep-dive:
cat .github/skills/{skill-name}/references/{topic}.md
# Load ONLY the reference matching your current sub-task — never all at once
```

### Example — Security Review

```bash
# User asks: "Review this code for security issues"

# 1. Read the skill
cat .github/skills/owasp-audit/SKILL.md          # ← 5 KB core

# 2. Follow Core Workflow steps 1-5

# 3. Reference Guide table says:
#    "Injection Prevention → references/injection-prevention.md → Load when doing SQL review"
#    "Broken Auth → references/broken-auth.md → Load when doing auth review"

# 4. You're reviewing SQL code, so load ONLY:
cat .github/skills/owasp-audit/references/injection-prevention.md

# 5. Continue the workflow with that knowledge loaded
```

### Skills (41) — Flat Structure

All skills live directly under `.github/skills/{skill-name}/SKILL.md`. Use `/skills` in Copilot CLI to list them, or invoke with `/skill-name`.

| Category | Skills |
|----------|--------|
| Code Quality | code-reviewer, refactor-planner, code-documenter, debugging-wizard, quality-analyzer, smart-refactor, tech-debt-tracker |
| Security | owasp-audit, secret-scanner, threat-modeler, authentication, authorization |
| Architecture | architecture-reviewer, design-pattern-advisor, dependency-analyzer, legacy-modernizer, polyglot-analyzer |
| Testing | test-generator, tdd-coach, test-coverage-analyzer |
| Database | schema-reviewer, query-optimizer |
| DevOps | ci-cd-builder, deployment-preflight, monitoring-expert, chaos-engineer |
| Documentation | readme-generator, adr-creator, api-documenter |
| Research | codebase-explorer, tech-spike-planner, spec-miner, deep-context-generator |
| Project Mgmt | spec-writer, issue-creator, feature-forge |
| AI | mcp-developer, prompt-engineer, agent-orchestrator |
| Language | dotnet-core-expert, csharp-developer |

### Rules

- **Read, don't invoke** — skills are files, not tools. Use `cat`/`Read`/`view`.
- **One skill at a time** — only read the skill matching the current task
- **Progressive disclosure** — never load all references; use the Reference Guide table to pick one
- **Follow checkpoints** — each Core Workflow step has a ✅ checkpoint; verify before proceeding
