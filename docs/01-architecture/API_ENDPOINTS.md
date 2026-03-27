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

**File:** `Api/Features/Contact/SendEmailFunction.cs`
**Flow:** Browser → Azure Function → Brevo SMTP (`smtp-relay.brevo.com:587`)

### Request

```json
{
  "subject":   "Project Inquiry",
  "message":   "Hi, I'm interested in your cloud consulting services...",
  "fromName":  "John Doe",
  "fromEmail": "john@example.com"
}
```

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| `subject` | string | Yes | Max 200 chars |
| `message` | string | Yes | Max 5000 chars |
| `fromName` | string | Yes | Max 100 chars |
| `fromEmail` | string | Yes | Valid email, max 254 chars |

### Success Response (200)

```json
{
  "success": true,
  "message": "Email sent successfully.",
  "messageId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890@cloudzen.com"
}
```

### Validation Error Response (400)

```json
{
  "success": false,
  "message": "Please provide a valid email address."
}
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

**File:** `Api/Features/Chat/ChatFunction.cs`
**Flow:** Browser → Azure Function → Anthropic API (`https://api.anthropic.com/v1/messages`)

### Request

```json
{
  "messages": [
    { "role": "user", "content": "What services does CloudZen offer?" },
    { "role": "assistant", "content": "CloudZen specializes in cloud consulting..." },
    { "role": "user", "content": "Tell me more about Azure migrations." }
  ]
}
```

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| `messages` | array | Yes | Max 10 messages |
| `messages[].role` | string | Yes | Must be `"user"` or `"assistant"` |
| `messages[].content` | string | Yes | Max 500 chars for user messages |

- **Max messages:** 10 per request
- **History sent to API:** Last 6 messages only (token cost control)

### Success Response (200)

```json
{
  "success": true,
  "reply": "Azure migrations involve assessing your current infrastructure, planning the migration strategy, and executing the move to Azure cloud services. CloudZen offers end-to-end support including..."
}
```

### Validation Error Response (400)

```json
{
  "success": false,
  "message": "Messages cannot be empty."
}
```

### Rate Limit Response (429)

```json
{
  "success": false,
  "message": "Too many requests. Please wait a moment before trying again."
}
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

**File:** `Api/Features/Booking/BookAppointmentFunction.cs`
**Flow:** Browser → Azure Function → n8n Webhook

This single endpoint handles three actions via the `action` field: **book**, **cancel**, and **reschedule**.

---

### 3.1 Book Action

Creates a new appointment.

#### Request

```json
{
  "action":       "book",
  "name":         "John Doe",
  "email":        "john@example.com",
  "phone":        "+15551234567",
  "businessName": "Acme Corp",
  "date":         "2025-02-15",
  "time":         "14:00",
  "endTime":      "14:30",
  "reason":       "CloudZen Virtual Meeting"
}
```

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| `action` | string | Yes | Must be `"book"` |
| `name` | string | Yes | Max 100 chars |
| `email` | string | Yes | Valid email, max 254 chars |
| `phone` | string | Yes | Max 20 chars, must start with `+` (E.164) |
| `businessName` | string | Yes | Max 200 chars |
| `date` | string | Yes | `YYYY-MM-DD` format |
| `time` | string | Yes | `HH:mm` 24-hour format |
| `endTime` | string | Yes | `HH:mm` 24-hour format |
| `reason` | string | No | Defaults to `"CloudZen Virtual Meeting"` |

#### Success Response (200)

```json
{
  "success": true,
  "action": "book",
  "bookingId": "APT-MN7O3825-TMVP",
  "message": "Your appointment has been confirmed."
}
```

#### Slot Taken Response (200)

```json
{
  "success": false,
  "action": "book",
  "message": "This time slot is no longer available. Please select another time."
}
```

---

### 3.2 Cancel Action

Cancels an existing appointment.

#### Request

```json
{
  "action":    "cancel",
  "bookingId": "APT-MN7O3825-TMVP",
  "email":     "john@example.com"
}
```

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| `action` | string | Yes | Must be `"cancel"` |
| `bookingId` | string | Yes | Existing booking ID (e.g., `APT-XXXXXXXX-XXXX`) |
| `email` | string | Yes | Must match original booking email |

#### Success Response (200)

```json
{
  "success": true,
  "action": "cancel",
  "message": "Your appointment has been cancelled."
}
```

#### Not Found Response (200)

```json
{
  "success": false,
  "action": "cancel",
  "message": "No appointment found with that booking ID and email."
}
```

---

### 3.3 Reschedule Action

Moves an existing appointment to a new date/time.

#### Request

```json
{
  "action":     "reschedule",
  "bookingId":  "APT-MN7O3825-TMVP",
  "email":      "john@example.com",
  "newDate":    "2025-02-20",
  "newTime":    "10:00",
  "newEndTime": "10:30"
}
```

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| `action` | string | Yes | Must be `"reschedule"` |
| `bookingId` | string | Yes | Existing booking ID |
| `email` | string | Yes | Must match original booking email |
| `newDate` | string | Yes | `YYYY-MM-DD` format |
| `newTime` | string | Yes | `HH:mm` 24-hour format |
| `newEndTime` | string | Yes | `HH:mm` 24-hour format |

#### Success Response (200)

```json
{
  "success": true,
  "action": "reschedule",
  "bookingId": "APT-MN7O3825-TMVP",
  "message": "Your appointment has been rescheduled."
}
```

#### Not Found Response (200)

```json
{
  "success": false,
  "action": "reschedule",
  "message": "No appointment found with that booking ID and email."
}
```

#### Slot Taken Response (200)

```json
{
  "success": false,
  "action": "reschedule",
  "message": "The new time slot is no longer available. Please select another time."
}
```

---

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
