---
applyTo: "**/*.cs, **/*.razor"
---

# OWASP Top 10 Security — Project Conventions

> Every code change must be evaluated through a security-first lens. When in doubt, choose the more restrictive option.

---

## A01 — Broken Access Control

The #1 web application security risk. Default posture: **deny all, allow explicitly.**

### Mandatory Practices

- Apply `[Authorize]` on **every** Blazor page and API endpoint — no anonymous defaults
- Use **policy-based authorization** — never inline role strings

```csharp
// ✅ Policy-based — centralized, testable
[Authorize(Policy = "CanApproveOrder")]
public sealed partial class ApproveOrderPage : ComponentBase { }

// ❌ Role string scattered across codebase
[Authorize(Roles = "Admin,Manager")] // VIOLATION — use policies
```

- Define all policies in a single `AuthorizationPolicies` class:

```csharp
public static class AuthorizationPolicies
{
    public const string CanApproveOrder = nameof(CanApproveOrder);
    public const string CanCancelOrder = nameof(CanCancelOrder);
    public const string CanViewOrders = nameof(CanViewOrders);

    public static void Register(AuthorizationOptions options)
    {
        options.AddPolicy(CanApproveOrder, policy =>
            policy.RequireClaim("app_role", "manager", "admin"));

        options.AddPolicy(CanCancelOrder, policy =>
            policy.RequireClaim("app_role", "manager", "admin", "support"));
    }
}
```

- Use **resource-based authorization** for entity-level checks:

```csharp
var authResult = await AuthorizationService.AuthorizeAsync(
    user, order, "OrderOwnerPolicy");
if (!authResult.Succeeded)
    return Forbid();
```

- **Never** rely on UI hiding alone — always enforce server-side

---

## A02 — Cryptographic Failures

### Secrets Management

- **Never** store secrets in `appsettings.json`, source code, or environment variables in production
- Use **Azure Key Vault** with **Managed Identity** for all production secrets
- Use `dotnet user-secrets` for local development only
- Stripe API keys: store in Key Vault, inject via `IOptions<StripeSettings>`
```csharp
// ✅ Options pattern — secret from Key Vault
public sealed class StripeSettings
{
    public string SecretKey { get; init; } = string.Empty;
    public string WebhookSecret { get; init; } = string.Empty;
}

// ❌ CRITICAL VIOLATION — hardcoded secret
var stripe = new StripeClient("sk_live_abc123..."); // NEVER DO THIS
```

### Data Protection

- Enforce **HTTPS everywhere** — `app.UseHsts()` and `app.UseHttpsRedirection()`
- Encrypt sensitive fields at rest in the database (PII, financial data)
- Never log tokens, API keys, connection strings, or PII

```csharp
// ❌ VIOLATION — logging PII / secrets
_logger.LogInformation("Processing request for {Email} with key {ApiKey}", user.Email, apiKey);

// ✅ Log only correlation identifiers
_logger.LogInformation("Processing request for order {OrderId}", order.Id);
```

---

## A03 — Injection

### SQL Injection Prevention

- **Always** use EF Core parameterized queries — never string-concatenate user input
- If raw SQL is required, use `FromSqlInterpolated` (never `FromSqlRaw` with concatenation)

```csharp
// ✅ EF Core — parameterized by default
var orders = await context.Orders
    .Where(o => o.CustomerId == customerId && o.Status == status)
    .ToListAsync(cancellationToken);

// ✅ Raw SQL — interpolated (parameterized)
var result = await context.Orders
    .FromSqlInterpolated($"SELECT * FROM orders WHERE customer_id = {customerId}")
    .ToListAsync(cancellationToken);

// ❌ CRITICAL VIOLATION — SQL injection vector
var sql = $"SELECT * FROM orders WHERE customer_id = '{request.CustomerId}'";
var result = await context.Orders.FromSqlRaw(sql).ToListAsync();
```

### Input Validation

- Validate **all** input at the application boundary using FluentValidation
- Every MediatR command must have a corresponding validator

```csharp
public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(1_000_000);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.IdempotencyKey).NotEmpty();
    }
}
```

### XSS Prevention

- Blazor encodes output by default — never use `@((MarkupString)untrustedContent)`
- Sanitize any user-provided HTML before rendering

---

## A05 — Security Misconfiguration

### Secure Headers

Configure security headers in `Program.cs` or middleware:

```csharp
app.UseHsts();
app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append(
        "Content-Security-Policy",
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline';");
    await next();
});
```

### Environment Configuration

- Never enable Swagger/OpenAPI in production
- Use `builder.Environment.IsDevelopment()` guards for debug-only features
- Disable detailed error pages in production — use `UseExceptionHandler`

---

## A07 — Identification and Authentication Failures

### Authentication Strategy

- Use **Microsoft Entra ID** (primary) or **Duende IdentityServer** for authentication
- **Never** implement custom authentication or store plaintext passwords
- Enforce MFA for privileged operations (approvals, administrative actions)
- Use `Microsoft.Identity.Web` for Entra ID integration

```csharp
builder.Services.AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
```

- For Blazor Server: use `RevalidatingServerAuthenticationStateProvider`
- Session timeout: configure reasonable expiration for application workflows

---

## Data Security Standards

### Sensitive Data Handling

- **Never** store raw card numbers, CVVs, or full magnetic stripe data
- Delegate payment processing to a PCI-compliant provider (e.g., **Stripe**) — use tokenized references only
- Store only external payment references (e.g., PaymentIntent IDs) in `Order` — never raw credentials
- Audit log all sensitive operations with timestamps and user identity

### Third-Party API Key Management

```csharp
// ✅ Keys injected via Options pattern, sourced from Key Vault
builder.Services.Configure<StripeSettings>(
    builder.Configuration.GetSection("Stripe"));

// Register Stripe client with DI
builder.Services.AddSingleton<IStripeClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<StripeSettings>>().Value;
    return new StripeClient(settings.SecretKey);
});
```

- Rotate API keys on a schedule
- Use restricted keys with minimum required permissions
- Validate Stripe webhook signatures on every incoming event

### Idempotency Keys

All state-changing commands **must** include an `IdempotencyKey` to prevent duplicate operations:

```csharp
public sealed record CreateOrderCommand(
    Guid OrderId,
    decimal Amount,
    string Currency,
    string IdempotencyKey) : IRequest<CreateOrderResult>;
```

- Generate idempotency keys client-side (GUID v7 recommended)
- Store and check idempotency keys server-side before processing
- Return cached results for duplicate requests

---

## Mass Assignment Prevention

**Never** bind request data directly to domain entities.

```csharp
// ❌ CRITICAL VIOLATION — mass assignment
[HttpPost]
public async Task<IActionResult> CreateOrder(
    [FromBody] Order order) // Domain entity bound directly!
{
    await repository.AddAsync(order);
}

// ✅ Use a DTO with explicit properties
public sealed record CreateOrderRequest(
    Guid CustomerId,
    decimal Amount,
    string Currency);
```

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
