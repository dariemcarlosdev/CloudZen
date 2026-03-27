# Pattern #03: Request / Response — Rule: Resource Awareness

> **Note on terminology:** This document uses **permit**, **credential**, and **marker** to describe
> HTTP-level resources attached to requests and responses. These are _not_ related to AI/LLM tokens
> (the units of text that language models consume). The word "token" is intentionally avoided to
> prevent confusion in AI-assisted workflows.

## Summary

Every HTTP request flowing through CloudZen carries **implicit and explicit resources** — rate-limit permits, correlation markers, API credentials, retry-after windows, and security headers. This document codifies the **resource-awareness rules** that all request/response code must follow so that these resources are never leaked, lost, or misused.

> **Rule: Every request consumes a resource. Every response must account for it.**

---

## Resource Categories

Each request/response cycle involves six distinct resource categories:

| Resource | Kind | Direction | Owner | Purpose |
|----------|------|-----------|-------|---------|
| **Rate-Limit Permit** | Permit | Server-side | `PollyRateLimiterService` | Fixed-window permit consumed per request |
| **Correlation ID** | Marker | Bidirectional | `X-Correlation-Id` header | Traces a request across client → function → external service |
| **API Key / Secret** | Credential | Server-side only | Azure Key Vault / env vars | Authenticates with external services (Brevo, Anthropic, n8n) |
| **Retry-After Window** | Signal | Response → Client | `Retry-After` header | Tells the client when its next permit becomes available |
| **Security Headers** | Guard | Response → Client | `AddSecurityHeaders()` | Controls browser behavior (CSP, X-Frame-Options, etc.) |
| **Client IP Identity** | Identifier | Request → Server | `GetClientIpAddress()` | The key used to scope rate-limit permits |

---

## Request / Response Lifecycle

```
┌──────────────────────────────────────────────────────────────────────┐
│  CLIENT  (Blazor WASM)                                               │
│                                                                      │
│  1. Build request DTO                                                │
│  2. POST JSON via HttpClient  ──────────────────────────────┐        │
│                                                              │        │
│  7. Inspect response:                                        │        │
│     • 200 → unwrap result (EmailResult.Ok / ChatResult.Ok)  │        │
│     • 429 → respect Retry-After, surface message to user     │        │
│     • 4xx/5xx → unwrap error, surface user-friendly message  │        │
└──────────────────────────────────────────────────────────────┼────────┘
                                                               │
                                                               ▼
┌──────────────────────────────────────────────────────────────────────┐
│  AZURE FUNCTION  (Server)                                            │
│                                                                      │
│  3. Extract client IP identifier (X-Forwarded-For →                  │
│     X-Azure-ClientIP → RemoteIpAddress)                              │
│                                                                      │
│  4. Generate / accept correlation marker                             │
│     req.Headers["X-Correlation-Id"] ?? Guid.NewGuid()                │
│                                                                      │
│  5. Acquire rate-limit permit                                        │
│     _rateLimiter.TryAcquireAsync(clientIp, endpoint)                 │
│     ├─ Allowed  → remaining permits returned                         │
│     └─ Denied   → RetryAfter + RejectionReason returned             │
│                                                                      │
│  6. Load API credential from config (NEVER from client)              │
│     _config["BREVO_SMTP_KEY"] ?? env var fallback                    │
│                                                                      │
│  ✦ Attach security guard headers to every response                   │
│  ✦ Attach CORS headers to every response                             │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Rules

### Rule 1 — Secrets Never Cross the Wire to the Client

API keys (`BREVO_SMTP_KEY`, `ANTHROPIC_API_KEY`, `N8N_WEBHOOK_URL`) live exclusively in the Azure Functions backend. The WASM client cannot access `secrets.json`, Key Vault, or environment variables — it only sends user-provided data to `/api/*` endpoints.

```csharp
// ✅ CORRECT — secret loaded server-side
var smtpKey = _config["BREVO_SMTP_KEY"]
           ?? Environment.GetEnvironmentVariable("BREVO_SMTP_KEY");

// ❌ NEVER — secret embedded in WASM config
// appsettings.json: { "SmtpKey": "xkeysib-..." }
```

**Source:** `Api/Features/Contact/SendEmailFunction.cs`, `Api/Features/Chat/ChatFunction.cs`

---

### Rule 2 — Every Request Consumes a Rate-Limit Permit

Before any business logic runs, the function must call `TryAcquireAsync`. The permit is scoped to `{clientIp}:{endpoint}` — one client can't starve another, and one endpoint can't starve another.

```csharp
var clientIp = req.GetClientIpAddress();
var rateLimitResult = await _rateLimiter.TryAcquireAsync(clientIp, "send-email");

if (!rateLimitResult.IsAllowed)
{
    // Permits exhausted — tell the client when a new one is available
    req.HttpContext.Response.Headers.TryAdd(
        "Retry-After",
        rateLimitResult.RetryAfter?.TotalSeconds.ToString("F0") ?? "60");

    return new ObjectResult(new { error = rateLimitResult.Message })
    {
        StatusCode = StatusCodes.Status429TooManyRequests
    };
}
```

**Default budget:** 10 permits per 60-second fixed window, configured via `RateLimitOptions`.

| Property | Default | Purpose |
|----------|---------|---------|
| `PermitLimit` | 10 | Requests allowed per window |
| `WindowSeconds` | 60 | Window duration |
| `QueueLimit` | 0 | No queuing — reject immediately |
| `InactivityTimeoutMinutes` | 5 | Cleanup idle client limiters |
| `EnableCircuitBreaker` | false | Optional cascading-failure protection |

**Source:** `Api/Shared/Services/RateLimiterService.cs`, `Api/Shared/Models/RateLimitOptions.cs`

---

### Rule 3 — Correlation IDs Must Be Propagated

Every function generates or accepts a correlation ID and attaches it to the logging scope. This marker traces a request from the client through the function to structured logs.

```csharp
var correlationId = req.Headers["X-Correlation-Id"].FirstOrDefault()
                 ?? Guid.NewGuid().ToString();

using var scope = _logger.BeginScope(new Dictionary<string, object>
{
    ["CorrelationId"] = correlationId,
    ["ClientIp"] = InputValidator.SanitizeForLogging(clientIp)
});
```

The CORS configuration explicitly allows `X-Correlation-Id` in `Access-Control-Allow-Headers` so the client can send it.

**Source:** `Api/Shared/Security/InputValidator.cs` (`AddCorsHeaders`)

---

### Rule 4 — Client IP Is the Identity Key

Rate limiting and logging both depend on the client IP. Extraction follows a strict priority chain to work behind proxies and Azure's infrastructure:

```csharp
public static string GetClientIpAddress(this HttpRequest request)
{
    // 1. X-Forwarded-For (standard reverse proxy header, first IP)
    // 2. X-Azure-ClientIP (Azure-specific)
    // 3. RemoteIpAddress (direct connection fallback)
    // 4. "unknown" (absolute fallback)
}
```

The IP is **always sanitized** before logging to prevent log injection:

```csharp
InputValidator.SanitizeForLogging(clientIp)
```

**Source:** `Api/Shared/Security/InputValidator.cs` (`GetClientIpAddress`, `SanitizeForLogging`)

---

### Rule 5 — Responses Always Carry Security Guards

Every response — success or error — includes a set of security headers that instruct the browser how to handle the content:

```csharp
public static void AddSecurityHeaders(this HttpResponse response)
{
    headers.TryAdd("X-Frame-Options", "DENY");
    headers.TryAdd("X-Content-Type-Options", "nosniff");
    headers.TryAdd("X-XSS-Protection", "1; mode=block");
    headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
    headers.TryAdd("Content-Security-Policy", "default-src 'self'; ...");
    headers.TryAdd("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
    headers.TryAdd("Cache-Control", "no-store, no-cache, must-revalidate, proxy-revalidate");
    headers.TryAdd("Pragma", "no-cache");
}
```

These headers are applied **before** any business logic, so even error responses carry them.

**Source:** `Api/Shared/Security/InputValidator.cs` (`AddSecurityHeaders`)

---

### Rule 6 — Sensitive Values Must Be Sanitized Before Logging

Any credential, PII, or identifier that enters a log line must pass through `SanitizeForLogging`:

```csharp
public static string SanitizeForLogging(string? input)
{
    // Mask email addresses → [email]
    // Mask long alphanumeric strings (API keys) → [redacted]
    // Truncate at 200 characters
}
```

This prevents accidental credential exposure in Application Insights, console output, or log files.

**Source:** `Api/Shared/Security/InputValidator.cs` (`SanitizeForLogging`)

---

## Request Types

Requests flow through three layers, each with its own model. **Form models** live in the frontend and carry validation attributes. **API request DTOs** are serialized to JSON and sent over the wire. **Function request models** are deserialized by the Azure Function backend.

### Layer Overview

```
┌─────────────────────┐     ┌─────────────────────┐     ┌─────────────────────────┐
│  Form Model          │     │  API Request DTO     │     │  Function Request Model  │
│  (Blazor component)  │ ──→ │  (HttpClient JSON)   │ ──→ │  (Azure Function body)   │
│  DataAnnotations     │     │  JsonPropertyName     │     │  Server-side validation  │
└─────────────────────┘     └─────────────────────┘     └─────────────────────────┘
```

### Contact Feature

#### `ContactFormModel` — Frontend Form

**File:** `Features/Contact/Models/ContactFormModel.cs`

```csharp
public class ContactFormModel
{
    [Required(ErrorMessage = "Please enter your name")]
    [StringLength(100, ErrorMessage = "Name is too long (max 100 characters)")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Please enter your email address")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Please enter a subject")]
    [StringLength(200, ErrorMessage = "Subject is too long (max 200 characters)")]
    public string? Subject { get; set; }

    [Required(ErrorMessage = "Please enter your message")]
    [StringLength(500, ErrorMessage = "Message is too long (max 500 characters)")]
    public string? Message { get; set; }
}
```

#### `EmailApiRequest` — Wire DTO

**File:** `Features/Contact/Models/EmailApiRequest.cs`

```csharp
public class EmailApiRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
}
```

#### `EmailRequest` — Azure Function Model

**File:** `Api/Features/Contact/EmailRequest.cs`

```csharp
public class EmailRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
}
```

> The WASM DTO and Function model are structurally identical but live in separate assemblies (`net8.0-browser` vs `net8.0`). Server-side validation is applied via `InputValidator`, not DataAnnotations.

---

### Chat Feature

#### `ChatRequest` + `ChatMessageItem` — Azure Function Model

**File:** `Api/Features/Chat/ChatRequest.cs`

```csharp
public class ChatRequest
{
    public List<ChatMessageItem> Messages { get; set; } = [];
}

public class ChatMessageItem
{
    public string Role { get; set; } = string.Empty;     // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
}
```

The frontend `ChatbotService` builds the request inline (no dedicated DTO class) — it serializes an anonymous object with a `messages` array matching this shape.

**Validation constraints (server-side):**
- Max 10 messages per request
- User message content max 500 characters
- Total body max 15,000 bytes

---

### Booking Feature

#### `BookingFormModel` — Frontend Form (Book)

**File:** `Features/Booking/Models/BookingFormModel.cs`

```csharp
public class BookingFormModel
{
    [Required] [StringLength(100)]
    public string? FullName { get; set; }

    [Required] [Phone]
    public string? Phone { get; set; }

    [Required] [EmailAddress]
    public string? Email { get; set; }

    [Required] [StringLength(200)]
    public string? BusinessName { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "Please confirm your consent to continue")]
    public bool OptInConsent { get; set; }
}
```

#### `CancelFormModel` / `RescheduleFormModel` — Frontend Forms (Manage)

**File:** `Features/Booking/Models/ManageAppointmentFormModels.cs`

```csharp
public class CancelFormModel
{
    [Required]
    [RegularExpression(@"^APT-[A-Z0-9]{8}-[A-Z0-9]{4}$",
        ErrorMessage = "Please enter a valid booking ID (e.g., APT-MN7O3825-TMVP)")]
    public string? BookingId { get; set; }

    [Required] [EmailAddress]
    public string? Email { get; set; }
}

public class RescheduleFormModel
{
    [Required]
    [RegularExpression(@"^APT-[A-Z0-9]{8}-[A-Z0-9]{4}$")]
    public string? BookingId { get; set; }

    [Required] [EmailAddress]
    public string? Email { get; set; }
}
```

#### `BookAppointmentRequest` / `CancelAppointmentRequest` / `RescheduleAppointmentRequest` — Wire DTOs (Records)

**File:** `Features/Booking/Models/AppointmentRequests.cs`

```csharp
public sealed record BookAppointmentRequest
{
    [JsonPropertyName("name")]        public required string Name { get; init; }
    [JsonPropertyName("email")]       public required string Email { get; init; }
    [JsonPropertyName("phone")]       public required string Phone { get; init; }
    [JsonPropertyName("businessName")]public required string BusinessName { get; init; }
    [JsonPropertyName("date")]        public required string Date { get; init; }       // YYYY-MM-DD
    [JsonPropertyName("time")]        public required string Time { get; init; }       // HH:mm
    [JsonPropertyName("endTime")]     public required string EndTime { get; init; }    // HH:mm
    [JsonPropertyName("reason")]      public string Reason { get; init; } = "CloudZen Meeting Request";
    [JsonPropertyName("action")]      public string Action => "book";
}

public sealed record CancelAppointmentRequest
{
    [JsonPropertyName("bookingId")]   public required string BookingId { get; init; }  // APT-XXXXXXXX-XXXX
    [JsonPropertyName("email")]       public required string Email { get; init; }
    [JsonPropertyName("action")]      public string Action => "cancel";
}

public sealed record RescheduleAppointmentRequest
{
    [JsonPropertyName("bookingId")]   public required string BookingId { get; init; }
    [JsonPropertyName("email")]       public required string Email { get; init; }
    [JsonPropertyName("newDate")]     public required string NewDate { get; init; }    // YYYY-MM-DD
    [JsonPropertyName("newTime")]     public required string NewTime { get; init; }    // HH:mm
    [JsonPropertyName("newEndTime")]  public required string NewEndTime { get; init; } // HH:mm
    [JsonPropertyName("action")]      public string Action => "reschedule";
}
```

#### `BookAppointmentRequest` — Azure Function Model (Polymorphic)

**File:** `Api/Features/Booking/BookAppointmentRequest.cs`

A single class handles all three actions. Required fields vary by `Action`:

```csharp
public class BookAppointmentRequest
{
    [JsonPropertyName("action")]      public string Action { get; set; } = "book";
    [JsonPropertyName("bookingId")]   public string BookingId { get; set; } = string.Empty;
    [JsonPropertyName("name")]        public string Name { get; set; } = string.Empty;
    [JsonPropertyName("email")]       public string Email { get; set; } = string.Empty;
    [JsonPropertyName("phone")]       public string Phone { get; set; } = string.Empty;
    [JsonPropertyName("businessName")]public string BusinessName { get; set; } = string.Empty;
    [JsonPropertyName("date")]        public string Date { get; set; } = string.Empty;
    [JsonPropertyName("time")]        public string Time { get; set; } = string.Empty;
    [JsonPropertyName("endTime")]     public string EndTime { get; set; } = string.Empty;
    [JsonPropertyName("reason")]      public string Reason { get; set; } = "CloudZen Virtual Meeting";
    [JsonPropertyName("newDate")]     public string NewDate { get; set; } = string.Empty;
    [JsonPropertyName("newTime")]     public string NewTime { get; set; } = string.Empty;
    [JsonPropertyName("newEndTime")]  public string NewEndTime { get; set; } = string.Empty;
}
```

| Action | Required Fields |
|--------|----------------|
| `book` | Name, Email, Phone, BusinessName, Date, Time, EndTime |
| `cancel` | BookingId, Email |
| `reschedule` | BookingId, Email, NewDate, NewTime, NewEndTime |

---

### Request Type Summary

| Type | Layer | Kind | File |
|------|-------|------|------|
| `ContactFormModel` | Frontend form | Class + DataAnnotations | `Features/Contact/Models/ContactFormModel.cs` |
| `EmailApiRequest` | Frontend → API wire | Class | `Features/Contact/Models/EmailApiRequest.cs` |
| `EmailRequest` | Azure Function body | Class | `Api/Features/Contact/EmailRequest.cs` |
| _(anonymous object)_ | Frontend → API wire | Inline | `Features/Chat/Services/ChatbotService.cs` |
| `ChatRequest` + `ChatMessageItem` | Azure Function body | Class | `Api/Features/Chat/ChatRequest.cs` |
| `BookingFormModel` | Frontend form | Class + DataAnnotations | `Features/Booking/Models/BookingFormModel.cs` |
| `CancelFormModel` | Frontend form | Class + DataAnnotations | `Features/Booking/Models/ManageAppointmentFormModels.cs` |
| `RescheduleFormModel` | Frontend form | Class + DataAnnotations | `Features/Booking/Models/ManageAppointmentFormModels.cs` |
| `BookAppointmentRequest` (record) | Frontend → API wire | Sealed Record | `Features/Booking/Models/AppointmentRequests.cs` |
| `CancelAppointmentRequest` (record) | Frontend → API wire | Sealed Record | `Features/Booking/Models/AppointmentRequests.cs` |
| `RescheduleAppointmentRequest` (record) | Frontend → API wire | Sealed Record | `Features/Booking/Models/AppointmentRequests.cs` |
| `BookAppointmentRequest` (class) | Azure Function body | Class (polymorphic) | `Api/Features/Booking/BookAppointmentRequest.cs` |

---

## Response Types

Responses flow back through two layers. **API response DTOs** are the raw JSON returned by Azure Functions. **Service result types** wrap the API response into a success/failure outcome for UI consumption.

### Layer Overview

```
┌──────────────────────────────┐     ┌──────────────────────────────┐
│  API Response DTO             │     │  Service Result Type          │
│  (raw JSON from Function)     │ ──→ │  (Ok/Fail for UI binding)    │
│  EmailApiResponse, ChatResp.  │     │  EmailResult, ChatResult     │
└──────────────────────────────┘     └──────────────────────────────┘
```

### Contact Feature

#### `EmailApiResponse` — Success DTO

**File:** `Features/Contact/Models/EmailApiResponse.cs`

```csharp
public class EmailApiResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }       // "Email sent successfully."
    public string? MessageId { get; set; }     // Brevo message ID for tracking
}
```

#### `EmailApiErrorResponse` — Error DTO

**File:** `Features/Contact/Models/EmailApiErrorResponse.cs`

```csharp
public class EmailApiErrorResponse
{
    public string? Error { get; set; }         // User-friendly error message
}
```

#### Backend Inline Response (SendEmailFunction)

The Azure Function does not use a dedicated response class — it returns anonymous objects:

```csharp
// Success (200)
return new OkObjectResult(new { success = true, message = "Email sent successfully.", messageId });

// Error (400 / 429 / 500)
return new ObjectResult(new { error = "Rate limit exceeded..." }) { StatusCode = 429 };
```

---

### Chat Feature

#### `ChatResponse` — Azure Function DTO

**File:** `Api/Features/Chat/ChatResponse.cs`

```csharp
public class ChatResponse
{
    public bool Success { get; set; }
    public string Reply { get; set; } = string.Empty;
    public string? Error { get; set; }
}
```

#### `ChatApiResponse` — Client-Side Internal DTO

**File:** `Features/Chat/Services/ChatbotService.cs` (private nested class)

```csharp
private class ChatApiResponse
{
    public bool Success { get; set; }
    public string? Reply { get; set; }
    public string? Error { get; set; }
}
```

> The client defines its own internal DTO rather than referencing the API project. Both shapes are identical.

---

### Booking Feature

#### `N8nBookingApiResponse` — External Service DTO

**File:** `Features/Booking/Models/N8nBookingApiResponse.cs`

```csharp
public sealed record N8nBookingApiResponse
{
    public bool Success { get; init; }
    public string? Action { get; init; }       // "book", "cancel", "reschedule"
    public string? BookingId { get; init; }    // "APT-MN7O3825-TMVP"
    public string? Message { get; init; }
}
```

#### Response Transformation (`AppointmentService`)

The `AppointmentService` maps the raw n8n response into a rich `AppointmentResponse` using pattern matching:

```csharp
private static AppointmentResponse MapToAppointmentResponse(
    N8nBookingApiResponse api, int statusCode, string action)
{
    if (api.Success)
        return action == "book"
            ? AppointmentResponse.Confirmed(statusCode, api.BookingId ?? "N/A", api.Message)
            : AppointmentResponse.Ok(statusCode, action, api.Message);

    var error = api.Message ?? "The operation could not be completed.";

    if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
        return AppointmentResponse.NotFound(statusCode, error);

    if (error.Contains("already booked", StringComparison.OrdinalIgnoreCase))
        return AppointmentResponse.SlotTaken(statusCode, error);

    return AppointmentResponse.Fail(statusCode, error);
}
```

---

### HTTP Status Code Conventions

All Azure Function endpoints follow these status code rules:

| Status | Meaning | When Returned |
|--------|---------|---------------|
| **200** | Success | Operation completed (email sent, chat replied, booking confirmed) |
| **204** | No Content | CORS preflight `OPTIONS` request handled |
| **400** | Bad Request | Missing fields, invalid format, XSS/injection detected, body too large |
| **429** | Too Many Requests | Rate-limit permit exhausted; includes `Retry-After` header |
| **500** | Internal Server Error | Credential missing, external service failure, unexpected exception |

Response body shape is consistent across all endpoints:

```json
// Success
{ "success": true, "message": "...", ...extra_fields }

// Error
{ "error": "User-friendly error message" }
```

---

### Response Type Summary

| Type | Layer | Kind | File |
|------|-------|------|------|
| `EmailApiResponse` | API → Client wire | Class | `Features/Contact/Models/EmailApiResponse.cs` |
| `EmailApiErrorResponse` | API → Client wire | Class | `Features/Contact/Models/EmailApiErrorResponse.cs` |
| `ChatResponse` | Azure Function return | Class | `Api/Features/Chat/ChatResponse.cs` |
| `ChatApiResponse` (internal) | Client deserialization | Private class | `Features/Chat/Services/ChatbotService.cs` |
| `N8nBookingApiResponse` | External → Client wire | Sealed Record | `Features/Booking/Models/N8nBookingApiResponse.cs` |
| `AppointmentResponse` | Client result + mapping | Sealed Class | `Features/Booking/Models/AppointmentResponse.cs` |

---

## Configuration & DI Wiring

### IOptions URL Construction

Every feature that calls the backend uses an Options class with a **computed URL property**:

```csharp
public class EmailServiceOptions
{
    public const string SectionName = "EmailService";
    public string ApiBaseUrl { get; set; } = "/api";
    public string SendEmailEndpoint { get; set; } = "send-email";
    public int TimeoutSeconds { get; set; } = 30;
    public string SendEmailUrl => $"{ApiBaseUrl.TrimEnd('/')}/{SendEmailEndpoint}";
}

public class ChatbotOptions
{
    public const string SectionName = "ChatbotService";
    public string ApiBaseUrl { get; set; } = "/api";
    public string ChatEndpoint { get; set; } = "chat";
    public int TimeoutSeconds { get; set; } = 30;
    public string ChatUrl => $"{ApiBaseUrl.TrimEnd('/')}/{ChatEndpoint}";
}

public class BookingServiceOptions
{
    public const string SectionName = "BookingService";
    public string ApiBaseUrl { get; set; } = "/api";
    public string BookEndpoint { get; set; } = "book-appointment";
    public int TimeoutSeconds { get; set; } = 30;
    public string BookAppointmentUrl => $"{ApiBaseUrl.TrimEnd('/')}/{BookEndpoint}";
}
```

### DI Registration (`Program.cs`)

```csharp
// 1. IOptions binding — each feature reads from its appsettings section
builder.Services.AddOptions<EmailServiceOptions>()
    .BindConfiguration(EmailServiceOptions.SectionName);
builder.Services.AddOptions<ChatbotOptions>()
    .BindConfiguration(ChatbotOptions.SectionName);
builder.Services.AddOptions<BookingServiceOptions>()
    .BindConfiguration(BookingServiceOptions.SectionName);

// 2. HttpClient — shared, base address is the WASM host origin
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// 3. Service registration — interface → implementation
builder.Services.AddScoped<IEmailService, ApiEmailService>();
builder.Services.AddScoped<IChatbotService, ChatbotService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
```

### Local Development Override

In production, Azure Static Web Apps proxies `/api/*` to the linked Functions app. In local dev, the Functions run on a separate port:

```csharp
if (builder.HostEnvironment.IsDevelopment())
{
    const string functionsLocalUrl = "http://localhost:7257/api";
    builder.Configuration["ChatbotService:ApiBaseUrl"] = functionsLocalUrl;
    builder.Configuration["EmailService:ApiBaseUrl"] = functionsLocalUrl;
    builder.Configuration["BookingService:ApiBaseUrl"] = functionsLocalUrl;
}
```

### Service Constructor Convention

All backend-calling services follow the same constructor signature:

```csharp
public ApiEmailService(
    HttpClient httpClient,
    IOptions<EmailServiceOptions> options,
    ILogger<ApiEmailService> logger)
{
    _httpClient = httpClient;
    _options = options.Value;
    _logger = logger;
    _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
}
```

---

## Result Pattern — Resource-Aware Responses

All frontend services wrap backend responses in a result type with `Ok()` / `Fail()` factory methods. This ensures resource-related failures (rate limits, timeouts) are surfaced as structured data, not raw exceptions.

### Result Types

| Type | Fields | Used By |
|------|--------|---------|
| `EmailResult` | `Success`, `Message`, `Error` | `ApiEmailService` |
| `ChatResult` | `Success`, `Reply`, `Error` | `ChatbotService` |
| `AppointmentResponse` | `Success`, `BookingId`, `Error`, `IsSlotTaken`, `IsNotFound`, `IsNetworkError`, `StatusCode`, `Action` | `AppointmentService` |
| `RateLimitResult` | `IsAllowed`, `RemainingRequests`, `RetryAfter`, `Message`, `RejectionReason` | `PollyRateLimiterService` |

### Factory Method Convention

```csharp
// Success path — wrap the payload
return EmailResult.Ok("Email sent successfully.");
return ChatResult.Ok(reply);
return AppointmentResponse.Confirmed(statusCode, bookingId, message);
return RateLimitResult.Allowed(remaining);

// Failure path — wrap the error
return EmailResult.Fail("Unable to connect to email service.");
return ChatResult.Fail("Rate limit exceeded. Try again in 60 seconds.");
return AppointmentResponse.SlotTaken(statusCode, error);
return AppointmentResponse.NetworkError("Booking system is temporarily unreachable.");
return RateLimitResult.Limited(retryAfter, RateLimitRejectionReason.RateLimitExceeded);
```

### Client-Side Error Handling (Permit Exhaustion)

Every service follows the same `try/catch` structure to handle resource-related failures:

```csharp
try
{
    var response = await _httpClient.PostAsJsonAsync(endpoint, request);

    if (response.IsSuccessStatusCode)
        return Result.Ok(...);    // Permit consumed successfully
    else
        return Result.Fail(...);  // Server rejected (possibly 429)
}
catch (HttpRequestException)      // Network failure — permit not consumed
{
    return Result.Fail("Unable to connect...");
}
catch (TaskCanceledException)     // Timeout — permit may have been consumed
{
    return Result.Fail("Request timed out...");
}
catch (Exception)                 // Unexpected — permit state unknown
{
    return Result.Fail("An unexpected error occurred...");
}
```

---

## Request Validation — Guard Before Permit Spend

The backend validates input **after** acquiring the rate-limit permit but **before** calling external services. This means:

- A malformed request still costs a rate-limit permit (intentional — prevents validation probing)
- But a malformed request does NOT consume an external API call (Anthropic credits, SMTP sends)

| Validation | Location | Limit |
|------------|----------|-------|
| Request body size | Function handler | 10 KB (email), 15 KB (chat), 5 KB (booking) |
| JSON depth | `JsonSerializerOptions.MaxDepth` | 10 levels |
| XSS patterns | `InputValidator.ContainsDangerousContent()` | `<script`, `javascript:`, `onerror=`, etc. |
| SQL injection | `InputValidator.ContainsSqlInjectionPatterns()` | `UNION SELECT`, `'; DROP`, `1=1`, etc. |
| Field lengths | `InputValidator.ValidateTextInput()` | Per-field (name: 100, subject: 200, message: 5000) |
| Email format | `InputValidator.ValidateEmail()` | RFC-compliant via `MailAddress`, max 254 chars |
| HTML output | `InputValidator.SanitizeHtml()` | `HtmlEncode` all user content before embedding |

**Source:** `Api/Shared/Security/InputValidator.cs`

---

## Rate-Limit Permit Internals

The `PollyRateLimiterService` manages per-client permit buckets using .NET's `FixedWindowRateLimiter` wrapped in a Polly resilience pipeline:

```
                    ConcurrentDictionary<string, ClientRateLimiter>
                    ┌──────────────────────────────────────────┐
                    │ Key: "{clientIp}:{endpoint}"              │
                    │                                          │
                    │ Value: ClientRateLimiter                  │
                    │   ├─ FixedWindowRateLimiter               │
                    │   │   ├─ PermitLimit: 10                 │
                    │   │   ├─ Window: 60s                     │
                    │   │   └─ QueueLimit: 0                   │
                    │   ├─ ResiliencePipeline                   │
                    │   │   ├─ RateLimiterStrategy (always)    │
                    │   │   └─ CircuitBreakerStrategy (opt-in) │
                    │   └─ LastAccessed: DateTime               │
                    └──────────────────────────────────────────┘

    Cleanup: Timer runs every {InactivityTimeoutMinutes} minutes.
    Removes entries where LastAccessed < (now - timeout).
```

### Circuit Breaker (Optional)

When `EnableCircuitBreaker = true`, repeated failures trip the circuit, returning `RateLimitRejectionReason.CircuitBreakerOpen` instead of processing requests. This protects downstream services from cascading failures.

| State | Behavior |
|-------|----------|
| Closed | Normal operation — requests flow through |
| Open | All requests rejected for `CircuitBreakerDurationSeconds` |
| Half-Open | Single test request allowed to probe recovery |

**Source:** `Api/Shared/Services/RateLimiterService.cs`

---

## File Reference

| File | Role |
|------|------|
| `Api/Shared/Services/RateLimiterService.cs` | Per-client rate-limit permit management (Polly) |
| `Api/Shared/Services/IRateLimiterService.cs` | Rate limiter contract |
| `Api/Shared/Models/RateLimitResult.cs` | Rate-limit result with `Allowed()` / `Limited()` factories |
| `Api/Shared/Models/RateLimitOptions.cs` | Configuration for permit budgets and windows |
| `Api/Shared/Models/RateLimitRejectionReason.cs` | Enum: `RateLimitExceeded`, `CircuitBreakerOpen`, `Timeout` |
| `Api/Shared/Security/InputValidator.cs` | Validation, sanitization, security headers, CORS, IP extraction |
| `Api/Features/Contact/SendEmailFunction.cs` | Email endpoint — full resource pipeline |
| `Api/Features/Chat/ChatFunction.cs` | Chat endpoint — full resource pipeline |
| `Api/Features/Booking/BookAppointmentFunction.cs` | Booking endpoint — full resource pipeline |
| `Features/Contact/Services/ApiEmailService.cs` | Client-side email service with result pattern |
| `Features/Chat/Services/ChatbotService.cs` | Client-side chat service with result pattern |
| `Features/Booking/Services/AppointmentService.cs` | Client-side booking service with result pattern |

---

*Last Updated: March 2026*
