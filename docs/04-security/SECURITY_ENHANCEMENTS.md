# CloudZen API Security Enhancements Documentation

This document provides comprehensive documentation of all security enhancements implemented in the CloudZen Azure Functions API.

---

## Table of Contents

1. [OWASP Top 10 Compliance](#owasp-top-10-compliance)
2. [Overview](#overview)
3. [Rate Limiting](#1-rate-limiting)
4. [Input Validation & Sanitization](#2-input-validation--sanitization)
5. [Security Headers](#3-security-headers)
6. [CORS Configuration](#4-cors-configuration)
7. [Azure Key Vault Integration](#5-azure-key-vault-integration)
8. [HTTP Security](#6-http-security)
9. [Logging Security](#7-logging-security)
10. [Request Throttling](#8-request-throttling)
11. [Configuration Summary](#configuration-summary)
12. [Security Checklist](#security-checklist)

---

## OWASP Top 10 Compliance

The table below maps each OWASP Top 10 (2021) category to the controls implemented in this application.

| # | OWASP Category | Status | Controls |
|---|---------------|--------|----------|
| A01 | **Broken Access Control** | ✅ Mitigated | Rate limiting per client IP; path traversal detection (`InputValidator.ContainsPathTraversal`); CORS strict origin validation; functions accept only expected HTTP methods |
| A02 | **Cryptographic Failures** | ✅ Mitigated | HSTS enforced via `host.json` and `Strict-Transport-Security` response header; secrets stored in Azure Key Vault; TLS (STARTTLS) for SMTP; SSL certificate validation restricted to revocation-only bypasses |
| A03 | **Injection** | ✅ Mitigated | XSS pattern detection covering 35+ attack vectors (script tags, event handlers, data URIs, dangerous HTML elements, SSTI); SQL injection pattern detection; HTML output encoded with `WebUtility.HtmlEncode` |
| A04 | **Insecure Design** | ✅ Mitigated | Defense-in-depth: rate limiting + input validation + security headers + CORS; request body size limits; JSON deserialization depth limits; AI response token budget and truncation |
| A05 | **Security Misconfiguration** | ✅ Mitigated | Security headers on all responses (X-Frame-Options, CSP, HSTS, Referrer-Policy, Permissions-Policy, X-Content-Type-Options); CORS requires explicit origin configuration in production; Key Vault credentials limited to required types only |
| A06 | **Vulnerable and Outdated Components** | ⚠️ Recommended | Enable Dependabot or GitHub Advanced Security for automated dependency vulnerability scanning; review NuGet package versions regularly |
| A07 | **Identification and Authentication Failures** | ✅ Mitigated | Per-client rate limiting with circuit breaker prevents brute-force and credential stuffing; correlation ID tracking for anomaly detection |
| A08 | **Software and Data Integrity Failures** | ✅ Mitigated | JSON deserialization depth limits (`MaxDepth = 10`); request body size enforcement; content validated before processing |
| A09 | **Security Logging and Monitoring Failures** | ✅ Mitigated | Structured logging with correlation IDs; PII masking (emails → `[email]`, tokens → `[token]`); Application Insights with adaptive sampling; health monitoring in `host.json` |
| A10 | **Server-Side Request Forgery (SSRF)** | ✅ Mitigated | Outbound HTTP calls use only hardcoded, trusted URLs (Anthropic API); `InputValidator.ValidateUrl` enforces HTTPS-only scheme and blocks private/loopback IP ranges; `IHttpClientFactory` used for all HTTP clients |

---

## Overview

The CloudZen API implements multiple layers of security to protect against common attack vectors including:

- **DDoS & Abuse Prevention** - Rate limiting per client IP
- **Injection Attacks** - XSS, SQL injection, and script injection protection
- **Data Leakage** - Secure logging that masks sensitive information
- **MITM Attacks** - HTTPS enforcement with HSTS
- **Clickjacking** - X-Frame-Options and CSP headers
- **CSRF/CORS Attacks** - Strict origin validation

---

## 1. Rate Limiting

### Location
- `Api/Services/RateLimiterService.cs`
- `Api/Functions/SendEmailFunction.cs`
- `Api/Functions/ChatFunction.cs`

### Implementation

```csharp
public interface IRateLimiterService
{
    Task<RateLimitResult> TryAcquireAsync(string clientIdentifier, string endpoint);
}
```

### Configuration

| Parameter | Value | Description |
|-----------|-------|-------------|
| `PermitLimit` | 10 | Maximum requests per window |
| `Window` | 1 minute | Time window for rate limiting |
| `QueueLimit` | 0 | No queuing of excess requests |

### Features

- **Fixed Window Algorithm**: Uses `FixedWindowRateLimiter` for predictable rate limiting
- **Per-Client Tracking**: Rate limits are applied per unique client IP address
- **Per-Endpoint Tracking**: Each endpoint can have independent rate limits
- **Automatic Cleanup**: Inactive rate limiters are disposed after 5 minutes of inactivity
- **Retry-After Header**: Returns proper `Retry-After` header when rate limited

### Response When Rate Limited

```json
{
    "error": "Rate limit exceeded. Try again in 60 seconds."
}
```

**HTTP Status Code**: `429 Too Many Requests`

---

## 2. Input Validation & Sanitization

### Location
- `Api/Security/InputValidator.cs`

### XSS Protection

The following dangerous patterns are blocked:

| Pattern | Attack Type |
|---------|-------------|
| `<script` | Script injection |
| `javascript:` | JavaScript protocol handler |
| `onerror=` | Event handler injection |
| `onclick=` | Event handler injection |
| `onload=` | Event handler injection |
| `eval(` | JavaScript code execution |
| `expression(` | CSS expression injection |
| `vbscript:` | VBScript protocol handler |
| `data:text/html` | Data URI injection |

### SQL Injection Protection

The following SQL patterns are blocked:

| Pattern | Attack Type |
|---------|-------------|
| `'; --` | Comment injection |
| `'; DROP` | Table dropping |
| `1=1` | Tautology attack |
| `1' OR '1'='1` | Boolean injection |
| `' OR ''='` | Empty string bypass |
| `UNION SELECT` | Union-based injection |
| `UNION ALL SELECT` | Union-based injection |
| `INSERT INTO` | Data insertion |
| `DELETE FROM` | Data deletion |
| `UPDATE SET` | Data modification |

### Validation Methods

#### Email Validation
```csharp
public static ValidationResult ValidateEmail(string? email)
```
- Validates email format using `System.Net.Mail.MailAddress`
- Enforces maximum length of 254 characters
- Checks for dangerous content patterns

#### Text Input Validation
```csharp
public static ValidationResult ValidateTextInput(
    string? input, 
    string fieldName, 
    int maxLength = 1000, 
    bool required = true)
```
- Configurable maximum length
- Optional/required field support
- XSS and SQL injection pattern detection

### Sanitization Methods

#### HTML Sanitization
```csharp
public static string SanitizeHtml(string? input)
```
- Encodes all HTML special characters using `WebUtility.HtmlEncode`
- Prevents XSS when rendering user content in emails

#### Logging Sanitization
```csharp
public static string SanitizeForLogging(string? input)
```
- Masks email addresses as `[email]`
- Masks potential API keys/tokens (32+ alphanumeric chars) as `[token]`
- Truncates long strings to 200 characters

---

## 3. Security Headers

### Location
- `Api/Security/InputValidator.cs` ? `SecurityHeadersExtensions.AddSecurityHeaders()`

### Headers Applied

| Header | Value | Purpose |
|--------|-------|---------|
| `X-Frame-Options` | `DENY` | Prevents clickjacking by denying all framing |
| `X-Content-Type-Options` | `nosniff` | Prevents MIME type sniffing |
| `X-XSS-Protection` | `1; mode=block` | Enables XSS filter in older browsers |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Controls referrer information leakage |
| `Content-Security-Policy` | `default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'` | Restricts resource loading |
| `Permissions-Policy` | `geolocation=(), microphone=(), camera=()` | Disables unnecessary browser features |
| `Cache-Control` | `no-store, no-cache, must-revalidate, proxy-revalidate` | Prevents caching of sensitive responses |
| `Pragma` | `no-cache` | HTTP/1.0 cache control |

### Usage

```csharp
[Function("SendEmail")]
public async Task<IActionResult> Run(HttpRequest req)
{
    // Add security headers to response
    req.HttpContext.Response.AddSecurityHeaders();
    // ...
}
```

---

## 4. CORS Configuration

### Location
- `Api/Program.cs`

### Configuration

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .SetIsOriginAllowedToAllowWildcardSubdomains()
            .WithHeaders(
                "Content-Type",
                "Authorization",
                "X-Requested-With",
                "X-Correlation-Id")
            .WithMethods("GET", "POST", "OPTIONS")
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});
```

### Allowed Origins (Default)

| Origin | Environment |
|--------|-------------|
| `https://localhost:5001` | Local development (HTTPS) |
| `https://localhost:7001` | Local development (HTTPS alt) |
| `http://localhost:5000` | Local development (HTTP) |
| `[ProductionOrigin]` | Production (from config) |

### Allowed Headers

- `Content-Type` - Request body format
- `Authorization` - Authentication tokens
- `X-Requested-With` - AJAX request indicator
- `X-Correlation-Id` - Request tracing

### Allowed Methods

- `GET` - Read operations
- `POST` - Create/send operations
- `OPTIONS` - Preflight requests

### Preflight Caching

Preflight responses are cached for **10 minutes** to reduce OPTIONS requests.

---

## 5. Azure Key Vault Integration

### Location
- `Api/Program.cs`

### Configuration

```csharp
var keyVaultEndpoint = Environment.GetEnvironmentVariable("KEY_VAULT_ENDPOINT");
if (!string.IsNullOrEmpty(keyVaultEndpoint))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultEndpoint),
        new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            // Security-optimized credential options
            ExcludeVisualStudioCredential = true,
            ExcludeVisualStudioCodeCredential = true,
            ExcludeInteractiveBrowserCredential = true,
            // Enabled for deployment scenarios
            ExcludeEnvironmentCredential = false,
            ExcludeManagedIdentityCredential = false,
            ExcludeAzureCliCredential = false,
            ExcludeAzurePowerShellCredential = false
        }));
}
```

### Credential Options

| Credential Type | Enabled | Use Case |
|-----------------|---------|----------|
| Environment Credential | ? Yes | CI/CD pipelines |
| Managed Identity | ? Yes | Azure-hosted apps |
| Azure CLI | ? Yes | Local development |
| Azure PowerShell | ? Yes | Local development |
| Visual Studio | ? No | Not needed, slower auth |
| VS Code | ? No | Not needed, slower auth |
| Interactive Browser | ? No | Not appropriate for APIs |

### Benefits

- Secrets (API keys, connection strings) stored in Azure Key Vault
- No secrets in code or configuration files
- Automatic secret rotation support
- Audit logging of secret access

---

## 6. HTTP Security

### Location
- `Api/host.json`

### HSTS (HTTP Strict Transport Security)

```json
{
    "extensions": {
        "http": {
            "hsts": {
                "isEnabled": true,
                "maxAge": "31536000",
                "includeSubDomains": true
            }
        }
    }
}
```

| Setting | Value | Description |
|---------|-------|-------------|
| `isEnabled` | `true` | HSTS is active |
| `maxAge` | `31536000` | 1 year in seconds |
| `includeSubDomains` | `true` | Applies to all subdomains |

### Benefits

- Forces HTTPS connections
- Prevents SSL stripping attacks
- Browsers remember to always use HTTPS

---

## 7. Logging Security

### Secure Logging Practices

#### Correlation ID Tracking

```csharp
var correlationId = req.Headers["X-Correlation-Id"].FirstOrDefault() 
    ?? Guid.NewGuid().ToString();

using var scope = _logger.BeginScope(new Dictionary<string, object>
{
    ["CorrelationId"] = correlationId,
    ["ClientIp"] = InputValidator.SanitizeForLogging(clientIp)
});
```

#### Sanitized Log Output

```csharp
_logger.LogInformation("SendEmail function triggered from {ClientIp}", 
    InputValidator.SanitizeForLogging(clientIp));
```

### What Gets Masked

| Data Type | Original | Masked |
|-----------|----------|--------|
| Email | `user@example.com` | `[email]` |
| API Key | `sk_live_abc123...xyz789` | `[token]` |
| Long strings | `(200+ chars)` | `...[truncated]` |

### Application Insights Configuration

```json
{
    "logging": {
        "applicationInsights": {
            "samplingSettings": {
                "isEnabled": true,
                "excludedTypes": "Request"
            },
            "enableLiveMetricsFilters": true
        }
    }
}
```

---

## 8. Request Throttling

### Location
- `Api/host.json`

### Configuration

```json
{
    "extensions": {
        "http": {
            "maxConcurrentRequests": 100,
            "maxOutstandingRequests": 200,
            "dynamicThrottlesEnabled": true
        }
    }
}
```

| Setting | Value | Description |
|---------|-------|-------------|
| `maxConcurrentRequests` | 100 | Max simultaneous requests |
| `maxOutstandingRequests` | 200 | Max queued requests |
| `dynamicThrottlesEnabled` | `true` | Auto-adjust based on load |

### Health Monitoring

```json
{
    "healthMonitor": {
        "enabled": true,
        "healthCheckInterval": "00:00:10",
        "healthCheckWindow": "00:02:00",
        "healthCheckThreshold": 6,
        "counterThreshold": 0.80
    }
}
```

---

## Configuration Summary

### Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `KEY_VAULT_ENDPOINT` | Optional | Azure Key Vault URI |
| `BREVO_SMTP_KEY` | Required | Brevo SMTP relay key (email delivery) |
| `BREVO_SMTP_LOGIN` | Required | Brevo SMTP login (email delivery) |
| `ANTHROPIC_API_KEY` | Required | Anthropic Claude API key (AI chatbot) |
| `ProductionOrigin` | Optional | Production CORS origin |
| `AllowedOrigins` | Optional | Additional CORS origins |

### Files Modified/Created

| File | Purpose |
|------|---------|
| `Api/Program.cs` | Main configuration with security settings, CORS, Key Vault, service registrations |
| `Api/host.json` | Host-level security configuration (HSTS, throttling, health monitor, custom headers) |
| `Api/Security/InputValidator.cs` | Validation, sanitization, security headers, and CORS utilities |
| `Api/Services/RateLimiterService.cs` | Polly-based per-client rate limiting with circuit breaker support |
| `Api/Functions/SendEmailFunction.cs` | Secured email function (rate limiting, input validation, CORS, security headers) |
| `Api/Functions/ChatFunction.cs` | Secured AI chatbot function (rate limiting, input validation, token controls, response truncation) |
| `Shared/Chatbot/CloudZenChatbot.razor` | Client-side chatbot widget with 5-message conversation cap |

---

## Security Checklist

### ? Implemented

- [x] **Rate limiting per client IP**
  - `Api/Services/RateLimiterService.cs` → `PollyRateLimiterService` class — per-client fixed window via Polly `FixedWindowRateLimiter`
  - `Api/Functions/SendEmailFunction.cs:113` → `_rateLimiter.TryAcquireAsync(clientIp, "send-email")`
  - `Api/Functions/ChatFunction.cs` → `_rateLimiter.TryAcquireAsync(clientIp, "chat")`
  - `Api/Models/Options/RateLimitOptions.cs` → configurable `PermitLimit`, `WindowSeconds`, `QueueLimit`

- [x] **XSS protection (input validation)**
  - `Api/Security/InputValidator.cs` → `DangerousPatterns` HashSet — 35+ patterns covering script tags (`<script`, `</script`), protocol handlers (`javascript:`, `vbscript:`), data URIs (`data:text/html`, `data:application/javascript`), dangerous HTML elements (`<iframe`, `<frame`, `<object`, `<embed`), event handlers (`onerror=`, `onclick=`, `onload=`, `onmouseover=`, `onfocus=`, `onblur=`, `onchange=`, `onsubmit=`, `onkeydown=` and more), code execution (`eval(`, `expression(`), SSTI patterns (`${`, `#{`, `<%=`).
  - `Api/Security/InputValidator.cs` → `ContainsDangerousContent()` checked by `ValidateTextInput()` and `ValidateEmail()`
  - `Api/Functions/ChatFunction.cs` → message role and content validation in `Run()` foreach loop

- [x] **SQL injection protection (pattern detection)**
  - `Api/Security/InputValidator.cs` → `SqlInjectionPatterns` HashSet (`'; DROP`, `UNION SELECT`, `1=1`, `DELETE FROM`, etc.)
  - `Api/Security/InputValidator.cs` → `ContainsSqlInjection()` checked by `ValidateTextInput()`

- [x] **HTML sanitization for output**
  - `Api/Security/InputValidator.cs` → `SanitizeHtml()` using `WebUtility.HtmlEncode()` before rendering user content in emails

- [x] **Security headers on all responses**
  - `Api/Security/InputValidator.cs` → `SecurityHeadersExtensions.AddSecurityHeaders()` extension method (X-Frame-Options, X-Content-Type-Options, X-XSS-Protection, Referrer-Policy, CSP, Permissions-Policy, **Strict-Transport-Security**, Cache-Control)
  - `Api/Functions/SendEmailFunction.cs:96` → `req.HttpContext.Response.AddSecurityHeaders()`
  - `Api/Functions/ChatFunction.cs` → `req.HttpContext.Response.AddSecurityHeaders()`
  - `Api/host.json:43-49` → `customHeaders` block (X-Content-Type-Options, X-Frame-Options, X-XSS-Protection, Referrer-Policy, Permissions-Policy)

- [x] **CORS with strict origin validation**
  - `Api/Security/InputValidator.cs` → `CorsSettings` record with `IsOriginAllowed()` method
  - `Api/Security/InputValidator.cs` → `AddCorsHeaders()` extension method (validates request origin against allowed list)
  - `Api/Program.cs:78-110` → `CorsSettings` built from config with dev defaults and production origin
  - `Api/host.json:26-36` → `cors.allowedOrigins` array
  - `Api/Functions/SendEmailFunction.cs:87` → `req.HttpContext.Response.AddCorsHeaders(req, _corsSettings)`
  - `Api/Functions/ChatFunction.cs` → `req.HttpContext.Response.AddCorsHeaders(req, _corsSettings)`

- [x] **HSTS enabled**
  - `Api/host.json:37-42` → `hsts` block (`isEnabled: true`, `maxAge: 365 days`, `includeSubDomains: true`, `preload: true`)
  - `Api/Security/InputValidator.cs` → `AddSecurityHeaders()` → `Strict-Transport-Security: max-age=31536000; includeSubDomains; preload` response header on all function responses

- [x] **Azure Key Vault integration**
  - `Api/Program.cs:28-45` → `AddAzureKeyVault()` with `DefaultAzureCredential` (Managed Identity, Azure CLI, environment credentials enabled; Visual Studio, VS Code, Interactive Browser excluded)

- [x] **Secure logging (PII masking)**
  - `Api/Security/InputValidator.cs` → `SanitizeForLogging()` (masks emails → `[email]`, tokens → `[token]`, truncates at 200 chars)
  - `Api/Functions/SendEmailFunction.cs:100-108` → correlation ID scope with `SanitizeForLogging(clientIp)`
  - `Api/Functions/ChatFunction.cs` → correlation ID scope with `InputValidator.SanitizeForLogging(clientIp)`

- [x] **Request throttling**
  - `Api/host.json:22-24` → `maxConcurrentRequests: 100`, `maxOutstandingRequests: 200`, `dynamicThrottlesEnabled: true`

- [x] **Request body size limits**
  - `Api/Functions/SendEmailFunction.cs:136` → `requestBody.Length > 10000` (10 KB limit for email requests)
  - `Api/Functions/ChatFunction.cs` → `requestBody.Length > MaxRequestBodySize` where `MaxRequestBodySize = 15000` (15 KB limit for chat)
  - `Api/Functions/ChatFunction.cs` → `MaxMessageContentLength = 500` (per user message character cap)
  - `Api/Functions/ChatFunction.cs` → `MaxMessages = 10` (max messages per request)

- [x] **JSON deserialization depth limits**
  - `Api/Functions/SendEmailFunction.cs:64` → `EmailRequestJsonOptions` with `MaxDepth = 10`
  - `Api/Functions/ChatFunction.cs` → `ChatJsonOptions` with `MaxDepth = 10`

- [x] **Correlation ID tracking**
  - `Api/Functions/SendEmailFunction.cs:100-106` → reads `X-Correlation-Id` header or generates `Guid.NewGuid()`, used in `ILogger.BeginScope()`
  - `Api/Functions/ChatFunction.cs` → same pattern with `correlationId` in logger scope dictionary

- [x] **Path traversal protection (OWASP A01: Broken Access Control)**
  - `Api/Security/InputValidator.cs` → `ContainsPathTraversal()` — detects `../`, `..\`, `/../`, `\..`, URL-encoded variants (`%2e%2e`, `%2e%2e%2f`)
  - `Api/Security/InputValidator.cs` → `ValidateTextInput()` — calls `ContainsPathTraversal()` on all text inputs

- [x] **SSRF protection (OWASP A10: Server-Side Request Forgery)**
  - `Api/Security/InputValidator.cs` → `ValidateUrl()` — enforces HTTPS-only scheme, checks against a host allowlist, and blocks private/loopback IP address ranges (10.x, 172.16-31.x, 192.168.x, 169.254.x, loopback, IPv6 ULA/link-local)
  - `Api/Functions/ChatFunction.cs` → outbound Anthropic API URL is a hardcoded constant (`AnthropicApiUrl`), never constructed from user input

- [x] **SSL/TLS certificate validation (OWASP A02: Cryptographic Failures)**
  - `Api/Functions/SendEmailFunction.cs` → `ServerCertificateValidationCallback` now rejects all SSL errors except revocation-related chain errors (since `CheckCertificateRevocation = false`), preventing MITM attacks

- [x] **Health monitoring**
  - `Api/host.json:53-59` → `healthMonitor` block (`enabled: true`, `healthCheckInterval: 10s`, `healthCheckWindow: 2min`, `counterThreshold: 0.80`)

- [x] **AI chatbot abuse prevention (multi-layer)**
  - `Api/Functions/ChatFunction.cs` → `MaxTokens = 200` (Anthropic token budget), `MaxReplyLength = 500` (server-side response truncation at sentence boundary)
  - `Api/Functions/ChatFunction.cs` → conversation history trimming `MaxConversationHistoryMessages = 6` (only sends recent context to Anthropic)
  - `Api/Functions/ChatFunction.cs` → system prompt hardening (off-topic rejection, no roleplay, character limit instruction, consultation redirect)
  - `Shared/Chatbot/CloudZenChatbot.razor` → client-side `MaxUserMessages = 5` cap with CTA replacement after limit

- [x] **Anthropic API error classification**
  - `Api/Functions/ChatFunction.cs` → billing error detection → `503 Service Unavailable`
  - `Api/Functions/ChatFunction.cs` → rate limit detection → `429 Too Many Requests`
  - `Api/Functions/ChatFunction.cs` → timeout detection, generic HTTP error, JSON parse error — each with specific status codes and user messages

### ⚠️ Recommended Additional Measures

- [ ] API key authentication for sensitive endpoints
- [ ] Azure AD/Entra ID integration for user auth
- [ ] WAF (Web Application Firewall) via Azure Front Door
- [ ] IP allowlisting for admin functions
- [ ] Penetration testing
- [ ] Security audit logging to Azure Sentinel
- [ ] Automated vulnerability scanning in CI/CD (A06: Vulnerable and Outdated Components)

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2024 | Initial security implementation (rate limiting, input validation, CORS, Key Vault, security headers) |
| 1.1.0 | March 2026 | Added ChatFunction security (AI chatbot abuse prevention, token controls, response truncation, Anthropic error classification) |
| 1.2.0 | March 2026 | OWASP Top 10 compliance review: expanded XSS patterns (35+ vectors), path traversal detection (A01), SSRF prevention with private IP blocking (A10), SSL certificate validation hardened (A02), HSTS header added to all function responses (A02/A05) |

---

## References

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [Azure Functions Security Best Practices](https://learn.microsoft.com/en-us/azure/azure-functions/security-concepts)
- [Azure Key Vault Documentation](https://learn.microsoft.com/en-us/azure/key-vault/)
- [Content Security Policy](https://developer.mozilla.org/en-US/docs/Web/HTTP/CSP)
