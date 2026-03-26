# Pattern #01: Azure Functions Proxy

## Summary

The Azure Functions Proxy pattern routes all external service calls through Azure Functions HTTP triggers. The Blazor WebAssembly frontend never communicates directly with third-party APIs — it sends requests to `/api/*` endpoints, and the Functions backend forwards them to the actual service after applying security, validation, and rate limiting.

> **API keys and secrets live only in the Functions backend. The WASM client never holds secrets.**

---

## Why This Pattern Exists

Blazor WebAssembly runs entirely in the browser. Any configuration, code, or secret shipped with the WASM app is **publicly visible** via browser DevTools. This means:

- API keys embedded in the client would be exposed to anyone
- Direct calls to third-party APIs would leak credentials
- There is no server-side process to protect sensitive data

The proxy pattern solves this by keeping all secrets server-side in the Azure Functions backend, which reads them from environment variables or Azure Key Vault.

---

## Architecture

```
┌─────────────────────────────┐
│   Blazor WASM (Browser)     │
│                             │
│  ApiEmailService ───────┐   │
│  ChatbotService  ───────┤   │
│  AppointmentService ────┤   │
│                         │   │
│  HttpClient → POST ────┘    │
│  to /api/{endpoint}         │
└────────────┬────────────────┘
             │ HTTP (JSON)
             ▼
┌─────────────────────────────┐
│   Azure Functions (Server)  │
│                             │
│  ┌─ CORS Check             │
│  ├─ Security Headers        │
│  ├─ Rate Limiting (Polly)   │
│  ├─ Input Validation        │
│  ├─ Secret Retrieval        │
│  │   (env vars / Key Vault) │
│  └─ Forward to External API │
└────────────┬────────────────┘
             │ HTTPS
             ▼
┌─────────────────────────────┐
│   External Services         │
│                             │
│  • Brevo SMTP (email)       │
│  • Anthropic Claude (AI)    │
│  • n8n Webhook (booking)    │
└─────────────────────────────┘
```

---

## Request Processing Pipeline

Every Azure Function endpoint follows this exact sequence:

| Step | Action | Failure Response |
|------|--------|------------------|
| 1 | Add CORS headers | — |
| 2 | Handle OPTIONS preflight → 204 | — |
| 3 | Add security headers | — |
| 4 | Rate limit check (Polly, per client IP) | 429 + `Retry-After` |
| 5 | Read & validate request body (size, format, content) | 400 |
| 6 | Retrieve API key from config/env | 500 |
| 7 | Call external service with secret | 500 / 503 |
| 8 | Return result to client | — |

---

## Implementations

### Email Proxy: `/api/send-email`

**Frontend → Backend → Brevo SMTP**

| Component | Location |
|-----------|----------|
| Frontend Service | `Services/ApiEmailService.cs` |
| Options Class | `Models/Options/EmailServiceOptions.cs` |
| Azure Function | `Api/Functions/SendEmailFunction.cs` |
| Backend Settings | `Api/Models/EmailSettings.cs` |

**Frontend (ApiEmailService):**
```csharp
// Builds request from contact form data and POSTs to the proxy
var request = new EmailApiRequest
{
    Subject = subject,
    Message = message,
    FromName = fromName,
    FromEmail = fromEmail
};

var response = await _httpClient.PostAsJsonAsync(_options.SendEmailUrl, request);
// Returns EmailResult.Ok() or EmailResult.Fail()
```

**Backend (SendEmailFunction):**
```csharp
[Function("SendEmail")]
public async Task<IActionResult> Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options", Route = "send-email")] HttpRequest req)
{
    // 1. CORS + security headers
    req.HttpContext.Response.AddCorsHeaders(req, _corsSettings);
    if (req.IsCorsPreflightRequest()) return new StatusCodeResult(204);
    req.HttpContext.Response.AddSecurityHeaders();

    // 2. Rate limiting
    var rateLimitResult = await _rateLimiter.TryAcquireAsync(clientIp, "send-email");
    if (!rateLimitResult.IsAcquired) return /* 429 */;

    // 3. Input validation (size, format, XSS patterns)
    var emailRequest = ValidateEmailRequest(body);

    // 4. Retrieve secrets from config (BREVO_SMTP_LOGIN, BREVO_SMTP_KEY)
    var smtpLogin = _configuration["BREVO_SMTP_LOGIN"];
    var smtpKey = _configuration["BREVO_SMTP_KEY"];

    // 5. Send via MailKit SMTP (smtp-relay.brevo.com:587)
    await SendEmailViaSmtpAsync(emailRequest, smtpLogin, smtpKey);
}
```

**Secrets used:** `BREVO_SMTP_LOGIN`, `BREVO_SMTP_KEY` (falls back to `BREVO_API_KEY`)

---

### Chat Proxy: `/api/chat`

**Frontend → Backend → Anthropic Claude API**

| Component | Location |
|-----------|----------|
| Frontend Service | `Services/ChatbotService.cs` |
| Options Class | `Models/Options/ChatbotOptions.cs` |
| Azure Function | `Api/Functions/ChatFunction.cs` |

**Frontend (ChatbotService):**
```csharp
// Builds request from conversation history and POSTs to the proxy
var request = new
{
    messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray()
};

var response = await _httpClient.PostAsJsonAsync(_options.ChatUrl, request);
// Returns ChatResult.Ok(reply) or ChatResult.Fail(error)
```

**Backend (ChatFunction):**
```csharp
[Function("Chat")]
public async Task<IActionResult> Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options", Route = "chat")] HttpRequest req)
{
    // Same pipeline: CORS → Rate Limit → Validate → Secret → Forward

    // Validation constraints:
    //   Max 10 messages per request
    //   User messages max 500 chars
    //   Max body size: 15,000 bytes

    // Retrieve secret
    var apiKey = _configuration["ANTHROPIC_API_KEY"];

    // Forward to Anthropic API (https://api.anthropic.com/v1/messages)
    //   Model: claude-sonnet-4-20250514
    //   Max tokens: 200
    //   Last 6 messages sent as conversation history
    //   System prompt with CloudZen knowledge base embedded
}
```

**Secrets used:** `ANTHROPIC_API_KEY`

---

### Booking Proxy: `/api/book-appointment`

**Frontend → Backend → n8n Webhook**

| Component | Location |
|-----------|----------|
| Frontend Service | `Services/AppointmentService.cs` |
| Options Class | `Models/Options/BookingServiceOptions.cs` |
| Azure Function | `Api/Functions/BookAppointmentFunction.cs` |
| Request Model | `Models/BookingAppointmentRequest.cs` |

**Frontend (AppointmentService):**
```csharp
// Builds request from booking form data and POSTs to the proxy
var request = new BookingAppointmentRequest
{
    Name = fullName,
    Email = email,
    Phone = phone,           // E.164 format
    BusinessName = business,
    Date = date,             // yyyy-MM-dd
    Time = time,             // HH:mm 24-hour
    EndTime = endTime,       // HH:mm 24-hour
    Action = "book",
    Reason = "CloudZen Virtual Meeting"
};

var response = await _httpClient.PostAsJsonAsync(_options.BookAppointmentUrl, request);
// Returns BookingResult.Ok(bookingId) or BookingResult.Fail(error)
```

**Backend (BookAppointmentFunction):**
```csharp
[Function("BookAppointment")]
public async Task<IActionResult> Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options", Route = "book-appointment")] HttpRequest req)
{
    // 1. CORS + security headers (same pipeline)
    req.HttpContext.Response.AddCorsHeaders(req, _corsSettings);
    if (req.IsCorsPreflightRequest()) return new StatusCodeResult(204);
    req.HttpContext.Response.AddSecurityHeaders();

    // 2. Rate limiting
    var rateLimitResult = await _rateLimiter.TryAcquireAsync(clientIp, "book-appointment");
    if (!rateLimitResult.IsAcquired) return /* 429 */;

    // 3. Input validation (name, email, phone, date, time)
    var validationError = ValidateBookingRequest(bookingRequest);

    // 4. Retrieve secret from config (N8N_WEBHOOK_URL)
    var webhookUrl = _config["N8N_WEBHOOK_URL"]
                  ?? Environment.GetEnvironmentVariable("N8N_WEBHOOK_URL");

    // 5. Forward to n8n webhook (server-to-server, no CORS issues)
    var httpClient = _httpClientFactory.CreateClient("SecureClient");
    var n8nResponse = await httpClient.PostAsync(webhookUrl, jsonContent);

    // 6. Pass n8n response back to frontend
}
```

**Secrets used:** `N8N_WEBHOOK_URL`

---

## Configuration Pattern

### Frontend Options (URL construction)

Both frontend services use an Options class with a computed URL property:

```csharp
public class EmailServiceOptions
{
    public const string SectionName = "EmailService";
    public string ApiBaseUrl { get; set; } = "/api";
    public string SendEmailEndpoint { get; set; } = "send-email";
    public string SendEmailUrl => $"{ApiBaseUrl.TrimEnd('/')}/{SendEmailEndpoint}";
}

public class ChatbotOptions
{
    public const string SectionName = "ChatbotService";
    public string ApiBaseUrl { get; set; } = "/api";
    public string ChatEndpoint { get; set; } = "chat";
    public string ChatUrl => $"{ApiBaseUrl.TrimEnd('/')}/{ChatEndpoint}";
}

public class BookingServiceOptions
{
    public const string SectionName = "BookingService";
    public string ApiBaseUrl { get; set; } = "/api";
    public string BookEndpoint { get; set; } = "book-appointment";
    public string BookAppointmentUrl => $"{ApiBaseUrl.TrimEnd('/')}/{BookEndpoint}";
}
```

### Local Development Override

In `Program.cs`, dev mode overrides the base URL to point to the local Functions instance:

```csharp
if (builder.HostEnvironment.IsDevelopment())
{
    const string functionsLocalUrl = "http://localhost:7257/api";
    builder.Configuration["ChatbotService:ApiBaseUrl"] = functionsLocalUrl;
    builder.Configuration["EmailService:ApiBaseUrl"] = functionsLocalUrl;
    builder.Configuration["BookingService:ApiBaseUrl"] = functionsLocalUrl;
}
```

In production, the default `/api` works because Azure Static Web Apps automatically proxies `/api/*` to the linked Functions app.

### Backend Secret Sources (priority order)

1. **Azure Key Vault** — via `KEY_VAULT_ENDPOINT` + `DefaultAzureCredential`
2. **Environment variables** — set in Azure Portal → Configuration
3. **`local.settings.json`** — local development only (gitignored)

---

## How to Add a New Proxy Endpoint

Follow these steps to add a new external service integration:

1. **Create a frontend Options class** in `Models/Options/`:
   ```csharp
   public class NewServiceOptions
   {
       public const string SectionName = "NewService";
       public string ApiBaseUrl { get; set; } = "/api";
       public string Endpoint { get; set; } = "new-endpoint";
       public string EndpointUrl => $"{ApiBaseUrl.TrimEnd('/')}/{Endpoint}";
   }
   ```

2. **Create a frontend service** in `Services/` implementing an interface from `Services/Abstractions/`:
   - Inject `HttpClient`, `IOptions<NewServiceOptions>`, `ILogger<T>`
   - POST JSON to `_options.EndpointUrl`
   - Return a result type with `Ok()`/`Fail()` factory methods

3. **Register in frontend `Program.cs`**:
   ```csharp
   builder.Services.AddOptions<NewServiceOptions>()
       .BindConfiguration(NewServiceOptions.SectionName);
   builder.Services.AddScoped<INewService, NewService>();
   ```

4. **Create an Azure Function** in `Api/Functions/`:
   - HTTP trigger with `"post", "options"` methods
   - Follow the pipeline: CORS → Security Headers → Rate Limit → Validate → Secret → Call → Return

5. **Add secrets** to `Api/local.settings.json` (dev) and Azure Portal (prod)

6. **Add dev URL override** in `Program.cs` if needed

---

*Last Updated: March 2026*
