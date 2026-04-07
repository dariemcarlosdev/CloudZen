---
paths:
  - "**/*.cs"
  - "**/*.razor"
description: OWASP Top 10 security rules for a fintech escrow platform
---

# OWASP Top 10 Security

> Auto-loaded by Claude Code when working with matching files.
> Full reference: `.github/instructions/security/owasp-top10.instructions.md`

## A01 — Broken Access Control

- `[Authorize]` on **every** page and endpoint — default deny-all posture
- Policy-based authorization (`[Authorize(Policy = "CanReleaseFunds")]`) — never inline role strings
- Define all policies in a centralized `AuthorizationPolicies` class
- Resource-based authorization for entity-level checks (`IAuthorizationService.AuthorizeAsync`)
- Never rely on UI hiding alone — always enforce server-side

## A02 — Cryptographic Failures

- Never store secrets in `appsettings.json` or source code
- Use Azure Key Vault + Managed Identity for production secrets
- `dotnet user-secrets` for local development only
- Stripe keys via `IOptions<StripeSettings>` sourced from Key Vault
- Enforce HTTPS everywhere — `UseHsts()` + `UseHttpsRedirection()`
- Never log tokens, API keys, connection strings, or PII

## A03 — Injection

- Always use EF Core parameterized queries — never string-concatenate user input
- Raw SQL: `FromSqlInterpolated` only — never `FromSqlRaw` with concatenation
- FluentValidation on **every** MediatR command — validate all input at the boundary
- Blazor encodes output by default — never use `@((MarkupString)untrustedContent)`

## A05 — Security Misconfiguration

- Security headers: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, CSP, `Referrer-Policy`
- Never enable Swagger in production
- Use `IsDevelopment()` guards for debug features
- Disable detailed error pages in production

## A07 — Authentication Failures

- Microsoft Entra ID or Duende IdentityServer — never custom auth
- Never store plaintext passwords
- Enforce MFA for privileged operations (fund releases, disputes)
- Blazor Server: `RevalidatingServerAuthenticationStateProvider`

## Fintech-Specific

- Never store raw card numbers or CVVs — Stripe tokenization only
- Store only Stripe Payment Intent IDs and charge references
- Audit log all payment operations with timestamps and actor identity
- Validate Stripe webhook signatures on every incoming event
- Rotate API keys on schedule; use restricted keys with minimum permissions

## Mass Assignment Prevention

- Never bind request data directly to domain entities
- Use DTOs with explicit properties for all API/command inputs
- Payment commands must include `IdempotencyKey` to prevent duplicate charges

## Forbidden Patterns

- ❌ `[AllowAnonymous]` on financial pages
- ❌ Hardcoded API keys or connection strings
- ❌ `FromSqlRaw` with string concatenation
- ❌ Logging emails, tokens, or card data
- ❌ Binding domain entities in endpoints
- ❌ Missing FluentValidation on commands
- ❌ `@((MarkupString)userInput)` in Razor

---

*Deep-dive: Read `.github/instructions/security/owasp-top10.instructions.md` for complete patterns and examples.*
