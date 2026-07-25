---
applyTo: "**/*.cs, **/*.razor"
---

# OWASP Top 10 Security — Project Conventions

> Every code change must be evaluated through a security-first lens. When in doubt, choose the more restrictive option.

---

## A01 — Broken Access Control

The #1 web application security risk. Default posture: **deny all, allow explicitly.**

### Mandatory Practices

Apply `[Authorize]` on **every** Blazor page and API endpoint — no anonymous defaults. Use **policy-based authorization** (never inline role strings). Define all policies in `AuthorizationPolicies` class: constant names, registered via `AddPolicy`. Use **resource-based authorization** for entity-level checks via `AuthorizationService.AuthorizeAsync()`. **Never** rely on UI hiding alone — always enforce server-side.

---

## A02 — Cryptographic Failures

### Secrets Management

Never store secrets in `appsettings.json`, source code, or production environment variables. Use **Azure Key Vault** with **Managed Identity** for production. Use `dotnet user-secrets` for local development. Store Stripe API keys in Key Vault, inject via `IOptions<StripeSettings>` Options pattern.

### Data Protection

Enforce **HTTPS everywhere** via `app.UseHsts()` and `app.UseHttpsRedirection()`. Encrypt sensitive fields at rest (PII, financial data). Never log tokens, API keys, connection strings, or PII — log only correlation IDs.

---

## A03 — Injection

### SQL Injection Prevention

**Always** use EF Core parameterized queries — never string-concatenate user input. If raw SQL required, use `FromSqlInterpolated` (never `FromSqlRaw` with concatenation).

### Input Validation

Validate **all** input at application boundary using FluentValidation. Every MediatR command must have corresponding validator.

```csharp
public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(1_000_000);
    }
}
```

### XSS Prevention

Blazor encodes output by default — never use `@((MarkupString)untrustedContent)`. Sanitize any user-provided HTML before rendering.

---

## A05 — Security Misconfiguration

### Secure Headers

Configure in `Program.cs` or middleware: `app.UseHsts()`, `app.UseHttpsRedirection()`, set `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, CSP headers.

### Environment Configuration

Never enable Swagger/OpenAPI in production. Use `builder.Environment.IsDevelopment()` guards for debug features. Disable detailed error pages in production — use `UseExceptionHandler`.

---

## A07 — Identification and Authentication Failures

### Authentication Strategy

Use **Microsoft Entra ID** (primary) or **Duende IdentityServer** for authentication. Never implement custom auth or store plaintext passwords. Enforce MFA for privileged operations. Use `Microsoft.Identity.Web` for Entra integration. For Blazor Server: use `RevalidatingServerAuthenticationStateProvider`. Configure reasonable session timeout for workflows.

---

## Data Security Standards

### Sensitive Data Handling

Never store raw card numbers, CVVs, or full magnetic stripe. Delegate payment processing to PCI-compliant provider (Stripe) — use tokenized references only. Store only external references (PaymentIntent IDs) in Order — never raw credentials. Audit log all sensitive operations with timestamps and user identity.

### Third-Party API Key Management

Keys injected via Options pattern, sourced from Key Vault. Register Stripe client with DI. Rotate keys on schedule. Use restricted keys with minimum permissions. Validate Stripe webhook signatures on every event.

### Idempotency Keys

All state-changing commands **must** include `IdempotencyKey` to prevent duplicates. Generate client-side (GUID v7 recommended). Store and check server-side before processing. Return cached results for duplicates.

---

## Mass Assignment Prevention

Never bind request data directly to domain entities. Use DTOs with explicit properties instead.

---

## Anti-Pattern Summary

| Anti-Pattern | Risk | Fix |
|---|---|---|
| `[AllowAnonymous]` on protected pages | Unauthorized access | `[Authorize(Policy = "...")]` |
| Hardcoded API keys or connection strings | Credential leak | Key Vault + Options pattern |
| `FromSqlRaw` with string concatenation | SQL injection | `FromSqlInterpolated` or LINQ |
| Logging user emails, tokens, card data | Data exposure | Log correlation IDs only |
| Binding domain entities in endpoints | Mass assignment | DTOs with explicit properties |
| Missing FluentValidation on commands | Invalid state / injection | Validator per command |
| Custom password hashing | Broken authentication | Entra ID / IdentityServer |
| Missing `[Authorize]` on new pages | Access control bypass | Default deny-all posture |
| `@((MarkupString)userInput)` in Razor | XSS | Never render untrusted HTML |
| Storing raw card numbers | PCI-DSS violation | Tokenized payment references only |
