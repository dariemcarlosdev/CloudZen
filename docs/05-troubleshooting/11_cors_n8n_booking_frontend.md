# Issue #11 — CORS Blocks Direct n8n Webhook Calls from Blazor WASM

**Date:** June 2025
**Status:** Resolved
**Affected files:**
- `Models/Options/BookingServiceOptions.cs`
- `Services/AppointmentService.cs`
- `wwwroot/appsettings.json`
- `Program.cs`
- `Api/Functions/BookAppointmentFunction.cs` *(new)*
- `Api/Models/BookAppointmentRequest.cs` *(new)*
- `Api/local.settings.json`

## Description

After implementing the booking flow, every appointment submission failed with the user-facing error:

> *"The CloudZen booking system is temporarily unreachable. Please try again in a moment."*

The browser console showed:

```
Access to fetch at 'https://cloudzen-n8n.pikapod.net/webhook/appointments'
from origin 'https://localhost:7243' has been blocked by CORS policy:
Response to preflight request doesn't pass access control check:
No 'Access-Control-Allow-Origin' header is present on the requested resource.
```

The `AppointmentService` was calling the n8n webhook URL **directly from the browser** via `HttpClient.PostAsJsonAsync()`.

## Why It Happens

Blazor WebAssembly runs entirely **inside the browser**. Every HTTP request made by `HttpClient` goes through the browser's `fetch()` API, which enforces the **Same-Origin Policy** and **CORS** (Cross-Origin Resource Sharing):

1. The Blazor app is served from `https://cloudzen.com` (or `https://localhost:7243` locally).
2. The n8n webhook lives at `https://cloudzen-n8n.pikapod.net` — a **different origin**.
3. Before the actual `POST`, the browser automatically sends an **OPTIONS preflight** request to `pikapod.net`.
4. The n8n server does **not** return `Access-Control-Allow-Origin` headers in its preflight response.
5. The browser **blocks the request entirely** — the C# code never receives a response.
6. `HttpClient` throws `HttpRequestException`, which the `catch` block maps to the "unreachable" message.

```
┌─────────┐     OPTIONS preflight      ┌──────────────────────────┐
│ Browser  │ ────────────────────────>  │ n8n (pikapod.net)        │
│ (WASM)   │ <── ❌ No CORS headers ──  │ No Access-Control-Allow  │
│          │                            │ -Origin in response      │
│          │  POST never sent           └──────────────────────────┘
└─────────┘
```

**Key distinction from Issue #1:** Issue #1 was CORS between the Blazor frontend and our **own** Azure Functions backend (solved by adding CORS headers to the Functions). This issue is CORS between the Blazor frontend and a **third-party** server (`pikapod.net`) whose CORS headers we do not control.

## Resolution

Route the request through the Azure Functions backend (server-to-server), following the same proxy pattern already used for email (`SendEmailFunction`) and chat (`ChatFunction`). Server-to-server HTTP calls are not subject to CORS — CORS is a **browser-only** security mechanism.

```
┌─────────┐  same-origin   ┌────────────────────┐  server-to-server  ┌─────────────────┐
│ Browser  │ ── POST ─────> │ /api/book-         │ ──── POST ───────> │ n8n (pikapod)   │
│ (WASM)   │ <── 200 ────── │ appointment        │ <── 200 ────────── │                 │
└─────────┘                 │ (Azure Function)   │                    └─────────────────┘
                            └────────────────────┘
                            same domain = no CORS
```

### Step 1 — Create the Azure Function proxy

**New file:** `Api/Functions/BookAppointmentFunction.cs`

The Function validates input, applies rate limiting, then forwards to n8n via `IHttpClientFactory`:

```csharp
[Function("BookAppointment")]
public async Task<IActionResult> Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options", Route = "book-appointment")] HttpRequest req)
{
    // CORS + security headers (same pattern as SendEmail / Chat)
    req.HttpContext.Response.AddCorsHeaders(req, _corsSettings);
    if (req.IsCorsPreflightRequest())
        return new StatusCodeResult(StatusCodes.Status204NoContent);
    req.HttpContext.Response.AddSecurityHeaders();

    // Rate limiting, input validation ...

    // Forward to n8n (server-to-server, no CORS)
    var webhookUrl = _config["N8N_WEBHOOK_URL"];
    var httpClient = _httpClientFactory.CreateClient("SecureClient");
    var n8nResponse = await httpClient.PostAsync(webhookUrl, jsonContent);

    // Pass the n8n JSON response through to the frontend
    return new ContentResult
    {
        Content = await n8nResponse.Content.ReadAsStringAsync(),
        ContentType = "application/json",
        StatusCode = StatusCodes.Status200OK
    };
}
```

### Step 2 — Move the n8n URL to server-side configuration

The webhook URL is now a **server-side secret** (anyone with it can create appointments).

**`Api/local.settings.json`** (local development):
```json
{
  "Values": {
    "N8N_WEBHOOK_URL": "https://cloudzen-n8n.pikapod.net/webhook/appointments"
  }
}
```

**Azure Portal** (production): Add `N8N_WEBHOOK_URL` as an App Setting on the Functions App.

### Step 3 — Update frontend to call the Azure Functions proxy

**`Models/Options/BookingServiceOptions.cs`** — replaced `WebhookUrl` with the same `ApiBaseUrl` + endpoint pattern used by `EmailServiceOptions` and `ChatbotOptions`:

```csharp
public class BookingServiceOptions
{
    public const string SectionName = "BookingService";
    public string ApiBaseUrl { get; set; } = "/api";
    public string BookEndpoint { get; set; } = "book-appointment";
    public int TimeoutSeconds { get; set; } = 30;
    public string BookAppointmentUrl => $"{ApiBaseUrl.TrimEnd('/')}/{BookEndpoint}";
}
```

**`wwwroot/appsettings.json`**:
```json
{
  "BookingService": {
    "ApiBaseUrl": "/api",
    "BookEndpoint": "book-appointment",
    "TimeoutSeconds": 30
  }
}
```

**`Program.cs`** — added local dev override:
```csharp
if (builder.HostEnvironment.IsDevelopment())
{
    const string functionsLocalUrl = "http://localhost:7257/api";
    builder.Configuration["BookingService:ApiBaseUrl"] = functionsLocalUrl;
}
```

**`Services/AppointmentService.cs`** — now calls the proxy:
```csharp
var response = await _httpClient.PostAsJsonAsync(_options.BookAppointmentUrl, request);
// _options.BookAppointmentUrl resolves to "/api/book-appointment" (production)
// or "http://localhost:7257/api/book-appointment" (local dev)
```

## How to Verify

1. **Local development:**
   - Start the Functions host: `cd Api && func start`
   - Start the Blazor app: `dotnet run`
   - Complete the booking flow — the request should go through `/api/book-appointment` (visible in the Functions console output and browser Network tab).

2. **Production:**
   - Ensure `N8N_WEBHOOK_URL` is set in the Azure Functions App Settings.
   - This keeps the n8n URL as a server-side secret, never exposed to the browser — consistent with how BREVO_SMTP_KEY and ANTHROPIC_API_KEY are handled.
   - The Blazor app calls `/api/book-appointment` on the same domain — Azure Static Web Apps proxies this to the linked Functions app automatically.

3. **Confirm no CORS errors:** Open browser DevTools → Network tab. The `book-appointment` request should show the same origin as the page (no cross-origin, no preflight).

## Related

- **Issue #1** (`01_cors_error_api.md`) — CORS between Blazor and our own Azure Functions (solved by adding CORS headers to Functions).
- **Architecture rule** (`.github/copilot-instructions.md`): *"API keys and secrets live only in the Functions backend. The WASM client never holds secrets."*
