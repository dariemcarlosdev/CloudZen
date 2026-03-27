# API Endpoints Reference

All endpoints are Azure Functions (Isolated Worker, .NET 8) behind `/api/`. The WASM client never holds secrets — all external service calls happen server-side.

> **Pattern doc:** See [Azure Functions Proxy Pattern](../06-patterns/01_azure_functions_proxy_api.md) for architecture diagrams and implementation guide.

---

## Endpoint Summary

| Endpoint | Method | External Service | Secret(s) | Max Body |
|----------|--------|-----------------|-----------|----------|
| `/api/send-email` | POST | Brevo SMTP | `BREVO_SMTP_LOGIN`, `BREVO_SMTP_KEY` | 10 KB |
| `/api/chat` | POST | Anthropic Claude | `ANTHROPIC_API_KEY` | 15 KB |
| `/api/book-appointment` | POST | n8n Webhook | `N8N_WEBHOOK_URL` | 5 KB |

All endpoints also accept `OPTIONS` for CORS preflight (returns `204`).

---

## 1. Send Email — `/api/send-email`

**File:** `Api/Functions/SendEmailFunction.cs`
**Flow:** Browser → Azure Function → Brevo SMTP (`smtp-relay.brevo.com:587`)

### Request

```json
{
  "subject":   "string — required, max 200 chars",
  "message":   "string — required, max 5000 chars",
  "fromName":  "string — required, max 100 chars",
  "fromEmail": "string — required, valid email"
}
```

### Success Response (200)

```json
{ "success": true, "message": "Email sent successfully.", "messageId": "guid@cloudzen.com" }
```

### Email Delivery Details

- **From:** `cloudzen.inc@gmail.com` (configurable via `EmailSettings`)
- **CC:** `softevolutionsl@gmail.com` (configurable)
- **Format:** Multipart MIME (HTML + plain text), all user content HTML-encoded
- **Transport:** MailKit SMTP with StartTLS

### Secrets

| Key | Source | Purpose |
|-----|--------|---------|
| `BREVO_SMTP_LOGIN` | Key Vault / env var | SMTP username |
| `BREVO_SMTP_KEY` | Key Vault / env var | SMTP password |
| `BREVO_API_KEY` | Key Vault / env var | Fallback if SMTP key absent |

---

## 2. Chat — `/api/chat`

**File:** `Api/Functions/ChatFunction.cs`
**Flow:** Browser → Azure Function → Anthropic API (`https://api.anthropic.com/v1/messages`)

### Request

```json
{
  "messages": [
    { "role": "user|assistant", "content": "string — max 500 chars for user" }
  ]
}
```

- **Max messages:** 10 per request
- **History sent to API:** Last 6 messages only (token cost control)

### Success Response (200)

```json
{ "success": true, "reply": "string — max 500 chars, truncated at sentence boundary" }
```

### Anthropic Configuration

| Setting | Value |
|---------|-------|
| Model | `claude-sonnet-4-20250514` |
| API version | `2023-06-01` |
| Max tokens | 200 per response |
| Reply hard limit | 500 characters |

### System Prompt

Embedded server-side (~800 lines). Contains brand identity, services, pricing, case studies, tone guidelines. **Never sent to the client.**

### Secrets

| Key | Source | Purpose |
|-----|--------|---------|
| `ANTHROPIC_API_KEY` | Key Vault / env var | Claude API authentication |

---

## 3. Book Appointment — `/api/book-appointment`

**File:** `Api/Functions/BookAppointmentFunction.cs`
**Flow:** Browser → Azure Function → n8n Webhook

### Request

```json
{
  "name":         "string — required, max 100 chars",
  "email":        "string — required, valid email",
  "phone":        "string — required, max 20 chars, must start with +",
  "businessName": "string — required, max 200 chars",
  "date":         "string — required, YYYY-MM-DD",
  "time":         "string — required, HH:mm (24h)",
  "endTime":      "string — required, HH:mm (24h)",
  "action":       "string — defaults to 'book'",
  "reason":       "string — defaults to 'CloudZen Virtual Meeting'"
}
```

### Success Response (200)

```json
{ "success": true, "bookingId": "string", "message": "string" }
```

### Secrets

| Key | Source | Purpose |
|-----|--------|---------|
| `N8N_WEBHOOK_URL` | Key Vault / env var | Webhook target URL |

---

## Shared Infrastructure

### Rate Limiting (all endpoints)

**Implementation:** Polly Fixed Window per `{clientIp}:{endpoint}`
**Config section:** `RateLimiting`

| Setting | Default |
|---------|---------|
| Permit limit | 10 requests |
| Window | 60 seconds |
| Queue | 0 (immediate rejection) |
| Inactivity cleanup | 5 minutes |

Exceeded → `429` with `Retry-After` header.

### Input Validation (`Api/Security/InputValidator.cs`)

**Text fields:** XSS pattern detection (`<script`, `javascript:`, `onerror=`, `eval(`, etc.) and SQL injection detection (`UNION SELECT`, `'; DROP`, `1=1`, etc.).

**Email fields:** RFC 5321 format, max 254 chars, dangerous pattern scan.

**Sanitization:**
- `SanitizeHtml()` — HTML-encodes user content before embedding in responses/emails
- `SanitizeForLogging()` — Masks emails → `[email]`, tokens → `[token]`, truncates at 200 chars

### Security Headers (all responses)

```
X-Frame-Options: DENY
X-Content-Type-Options: nosniff
X-XSS-Protection: 1; mode=block
Referrer-Policy: strict-origin-when-cross-origin
Content-Security-Policy: default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'
Permissions-Policy: geolocation=(), microphone=(), camera=()
Cache-Control: no-store, no-cache, must-revalidate, proxy-revalidate
```

### CORS

**Allowed origins (local dev):** `https://localhost:7243`, `http://localhost:5054`
**Production:** Configured via `AllowedOrigins` array or `ProductionOrigin`
**Allowed methods:** GET, POST, OPTIONS
**Max age:** 600 seconds (10 min)

### Correlation & Tracking

Every request gets:
- **Correlation ID:** From `X-Correlation-Id` header or auto-generated GUID
- **Client IP:** Extracted from `X-Forwarded-For` → `X-Azure-ClientIP` → direct connection
- **Structured logging** with correlation context

### HTTP Client ("SecureClient")

Shared `HttpClientFactory` config: User-Agent `CloudZen-Api/1.0`, 30-second timeout.

---

## Error Response Patterns

All endpoints follow consistent error shapes:

| Status | Trigger | Pattern |
|--------|---------|---------|
| `204` | OPTIONS preflight | No body |
| `400` | Empty body, oversized body, validation failure, bad JSON | `{ "success": false, "message": "..." }` or `{ "error": "..." }` |
| `429` | Rate limit exceeded | `Retry-After` header + message |
| `500` | Missing secrets, unexpected errors | User-friendly message, details logged server-side |
| `502` | External service unavailable | Endpoint-specific gateway error |
| `503` | Anthropic billing/quota | Chat only |
| `504` | External service timeout | Endpoint-specific timeout message |

---

## Related Docs

- [Azure Functions](AZURE_FUNCTIONS.md) — Hosting model, Program.cs setup, Key Vault integration
- [Configuration](CONFIGURATION.md) — Secrets strategy, IOptions pattern, options class inventory
- [Vertical Slice Architecture](VERTICAL_SLICE_ARCHITECTURE.md) — Where function files live (`Api/Features/{Feature}/`)
- [Azure Functions Proxy Pattern](../06-patterns/01_azure_functions_proxy_api.md) — Architecture diagram and "add new endpoint" guide
