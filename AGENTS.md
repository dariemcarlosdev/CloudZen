# AGENTS.md — CloudZen AI Instructions (Merged)

> Consolidated from AGENTS.md + CLAUDE.md + GEMINI.md. Original files archived.

## 1. Project Context

CloudZen is a **Blazor WebAssembly** frontend plus an **Azure Functions Isolated Worker** API backend under `Api/`.

- Frontend: .NET 8 WASM SPA
- Backend: Azure Functions v4 (.NET 8, isolated)
- Deployment: Azure Static Web Apps + Azure Functions
- Styling: Tailwind (CDN) + component-scoped CSS

---

## 2. Architecture — Vertical Slices by Feature

```
Frontend:  Features/{Booking|Contact|Chat|Landing|Profile|Projects|Tickets}/
           Common/  Layout/  Pages/

Backend:   Api/Features/{Booking|Contact|Chat}/
           Api/Shared/{Security|Services|Models}/
```

Each feature owns its `Components/`, `Models/`, and `Services/`. Use **WASM → Azure Function → external provider** proxy pattern for sensitive operations.

### Feature Classification

**Full-stack slices (WASM + API):** Booking, Contact, Chat  
**Frontend-only slices:** Landing, Profile, Projects, Tickets

Only full-stack slices add/modify Azure Function endpoints.

---

## 3. Component Rules (MANDATORY)

**Always generate three files per component:**

```
ComponentName.razor       ← Markup only. No @code {} blocks.
ComponentName.razor.cs    ← sealed partial class. All logic.
ComponentName.razor.css   ← Scoped CSS (when needed)
```

**Template:**

```csharp
// .razor — Markup only
@page "/route"
<div class="wrapper"><h1>@L["Title"]</h1></div>

// .razor.cs — Logic
namespace CloudZen.Features.{Feature}.Components;
public sealed partial class ComponentName
{
    [Inject] private HttpClient Http { get; set; } = default!;
    protected override async Task OnInitializedAsync() { }
}
```

**Rules:**
1. Keep `.razor` markup-only
2. All logic in `.razor.cs`
3. Tailwind utilities first, `.razor.css` for advanced styling
4. `[Parameter]` for parent-to-child, `EventCallback<T>` for child-to-parent
5. Pages are thin orchestration shells
6. Localize all user text: `@L["Key"]`

---

## 4. Service & DI Rules

- Register services in `Program.cs`
- Use interfaces: `IEmailService`, `IChatbotService`, `IBookingService`
- Backend services use `HttpClient` + options classes
- Strongly typed options: `EmailServiceOptions`, `ChatbotOptions`, `BookingServiceOptions`
- Bind via `AddOptions<T>().BindConfiguration(...)`
- **Never place secrets in WASM client config**

---

## 5. API & Security (CRITICAL)

All `/api/*` endpoints must preserve:
1. Input validation via shared validators
2. Rate limiting (Polly-based) per endpoint
3. Security headers + proper CORS
4. Correlation IDs in logs
5. Secrets from environment/Key Vault only
6. **No PII, tokens, or secrets in logs or responses**

### OWASP Top 10 Checklist

| # | Check |
|---|---|
| A01 | Authorization on every endpoint? Policy-based? |
| A02 | Secrets in code? PII in logs? TLS enforced? |
| A03 | Parameterized queries? No SQL concatenation? |
| A04 | Threat model reviewed? Business logic bypasses? |
| A05 | HTTPS? HSTS? Debug disabled in prod? |
| A06 | NuGet packages up to date? CVEs? |
| A07 | Token validation? Brute-force protection? |
| A08 | Deserialization safe? Pipeline integrity? |
| A09 | Audit trail? Correlation IDs? No secrets logged? |
| A10 | External URL validation? Allowlisting? |

**Report format:** Severity (Critical/High/Medium/Low), Location, Issue, Fix.

---

## 6. Endpoint Ownership

**Current API endpoints:**
- `/api/send-email`
- `/api/chat`
- `/api/book-appointment`

Place new endpoints under `Api/Features/{Feature}/` and document request/response contracts.

---

## 7. Model Ownership

WASM and API are separate projects. DTO duplication is acceptable.
- No forced project references between WASM and API
- Transformation logic in API proxy functions

---

## 8. Namespace & Naming

- Root: `CloudZen.*`
- Mirror folder paths (feature-first)
- File names reflect role: `*Service`, `I*Service`, `*Options`, `*Function`
- Explicit, intention-revealing names

---

## 9. Code Style

**C# conventions:**
- Explicit types for domain objects: `BookingRequest request` (not `var`)
- File-scoped namespaces: `namespace CloudZen.Features.Booking;`
- Nullable enabled: `string?` for nullable
- `sealed` by default on concrete classes
- `record` types for DTOs with `init` properties
- Primary constructors for DI
- `CancellationToken` in all async methods
- Guard clauses — fail fast

**Immutability:**
- `record` over `class` for DTOs
- `readonly` fields in services
- `init` properties where mutation not needed
- `IReadOnlyCollection<T>` for returns
- Expression-bodied members for single-line logic

---

## 10. Reasoning & Exploration

**Before making changes:**
1. Trace inbound/outbound references
2. Identify feature slice ownership
3. Check pattern consistency in same directory
4. Verify interface contracts + all implementations
5. Map dependencies

**Cross-reference checklist:**

| Question | Where |
|---|---|
| DI wired? | `Program.cs`, `Api/Program.cs` |
| Services? | `Features/{Feature}/Services/` |
| Azure Functions? | `Api/Features/{Feature}/` |
| Components? | `Features/{Feature}/Components/` |
| Models? | `Features/{Feature}/Models/`, `Api/Shared/Models/` |

**When refactoring:**
1. Identify smell
2. Trace dependencies
3. Evaluate SOLID impact
4. Plan migration (backward compatibility)
5. Verify invariants (no PII logging, secrets safe)

---

## 11. Component Analysis

**When working with Blazor components:**
1. Check all three files (`.razor`, `.razor.cs`, `.razor.css`)
2. Verify `[Parameter]` and `EventCallback<T>` usage
3. Confirm localization: `@L["Key"]` or `L["Key"]`
4. Scoped CSS only — no global overrides
5. Tailwind consistency

**Component inventory:**

| Type | Location |
|---|---|
| Pages | `Features/{Feature}/Components/` (with `@page`) |
| Layouts | `Layout/` |
| Common | `Common/` |

---

## 12. Feature Workflow

**When adding/modifying features:**
1. Identify vertical slice in `Features/{Feature}/`
2. Check `docs/03-features/`
3. Map dependencies (services, models, API)
4. Follow existing patterns
5. Update docs
6. Verify DI in `Program.cs`
7. Update `Api/Features/{Feature}/` if API changes

---

## 13. Business Rules

**Non-negotiable invariants:**

| Rule | Rationale |
|---|---|
| Validate input at API boundaries | Prevents invalid state |
| Never log PII/tokens/secrets | GDPR compliance |
| Idempotency on external calls | Prevents duplicates on retry |
| Authorization on every API endpoint | Default deny |
| Rate limiting on API | Prevents abuse/DoS |
| Secrets from environment only | Never in code/WASM config |

---

## 14. Error Handling

- Domain-specific exceptions for business violations
- API catches infrastructure exceptions → meaningful HTTP responses
- Never swallow silently — log with context + correlation IDs
- HTTP status codes: 400 (validation), 404 (not found), 409 (conflict), 500 (unexpected)

---

## 15. Documentation (MANDATORY)

Update `docs/` when behavior changes.

**Primary docs:**
- `docs/01-architecture/VERTICAL_SLICE_ARCHITECTURE.md`
- `docs/01-architecture/COMPONENT_ARCHITECTURE.md`
- `docs/01-architecture/AZURE_FUNCTIONS.md`
- `docs/01-architecture/API_ENDPOINTS.md`
- `docs/03-features/`, `docs/04-security/`, `docs/05-troubleshooting/`, `docs/06-patterns/`

**Update matrix:**

| Change | Doc |
|---|---|
| Architecture | `docs/01-architecture/*` |
| Feature logic | `docs/03-features/{Feature}.md` |
| Security | `docs/04-security/*` |
| API endpoints | `API_ENDPOINTS.md` |
| Component patterns | `COMPONENT_PATTERNS.md` |

---

## 16. Quality Guardrails

1. Preserve feature isolation — avoid cross-feature coupling
2. Frontend secrets-free — backend handles sensitive ops
3. Incremental changes over broad rewrites
4. Nullable-enabled, compile-safe C#
5. Follow existing patterns before new abstractions

---

## 17. Source-of-Truth Files

Check before major changes:
- `README.md`
- `Program.cs` (frontend)
- `Api/Program.cs` (backend)
- `docs/01-architecture/*`
- `.github/copilot-instructions.md`

---

## 18. Skills Catalog

All skills in `.github/skills/`. Browse: `.github/skills/CATALOG.md`

**Claude integration:** Skills registered in `.claude/skills/` (bridge files) → redirect to `.github/skills/` (source of truth).

**Quick reference:**

| Invoke | Path |
|---|---|
| `/code-reviewer` | `.github/skills/code-reviewer/SKILL.md` |
| `/owasp-audit` | `.github/skills/owasp-audit/SKILL.md` |
| `/test-generator` | `.github/skills/test-generator/SKILL.md` |
| `/architecture-reviewer` | `.github/skills/architecture-reviewer/SKILL.md` |

---

**This merged guide consolidates CloudZen project conventions, code generation standards, security baseline, exploration strategies, and skills integration for all AI agents.**
