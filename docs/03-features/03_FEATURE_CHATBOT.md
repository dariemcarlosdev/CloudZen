# Chatbot Virtual Assistant Feature

> **Document**: CloudZen AI Chatbot -- Complete Technical Reference  
> **Scope**: Architecture, implementation, configuration, security, deployment, and testing of the AI chatbot feature  
> **Audience**: AI assistants, developers  
> **Last Updated**: March 2026  

**Out of scope**: General Azure Functions patterns (see [Proxy Pattern doc](../06-patterns/01_azure_functions_proxy_api.md)), shared security infrastructure (see [API Endpoints doc](../01-architecture/API_ENDPOINTS.md)), Tailwind CSS styling system.

---

## Table of Contents

1. [Overview](#1-overview)
2. [Architecture](#2-architecture)
3. [Project Structure](#3-project-structure)
4. [User Flow](#4-user-flow)
5. [Components](#5-components)
6. [API Integration](#6-api-integration)
7. [AI Provider -- Anthropic Claude](#7-ai-provider----anthropic-claude)
8. [Request/Response](#8-requestresponse)
9. [Configuration](#9-configuration)
10. [Security and Abuse Prevention](#10-security-and-abuse-prevention)
11. [Token Consumption Controls](#11-token-consumption-controls)
12. [Lead Generation and Conversion Strategy](#12-lead-generation-and-conversion-strategy)
13. [Error Handling](#13-error-handling)
14. [UI Components](#14-ui-components)
15. [Local Development](#15-local-development)
16. [Deployment](#16-deployment)
17. [Testing Guide](#17-testing-guide)
18. [Related Docs](#18-related-docs)

---

## 1. Overview

The CloudZen AI Chatbot is a website-embedded conversational assistant that answers visitor questions about CloudZen's services, process, and portfolio. It converts visitors into leads by guiding them toward booking a free consultation, while protecting against abuse with multi-layered rate limiting, input validation, and conversation caps.

The chatbot uses Anthropic Claude as the AI backend. All API communication is proxied through Azure Functions so that the API key and knowledge base are never exposed to the browser. It is **not** a general-purpose AI assistant -- it is scoped exclusively to CloudZen's business context.

### Key Design Principles

| Principle | Implementation |
|---|---|
| **Security first** | API key stays server-side; knowledge base never sent to client |
| **Cost control** | Capped tokens, capped messages, capped reply length, conversation history trimming |
| **Lead conversion** | 5-question limit then CTA to book consultation; system prompt always redirects to outreach |
| **Jargon-free** | System prompt enforces plain English, 1-2 sentence responses |
| **Abuse resistant** | Per-IP rate limiting, input validation, off-topic rejection via prompt |

---

## 2. Architecture

```
+-----------------------------------------------------------------+
|                    BROWSER (Client)                              |
|                                                                  |
|  +----------------------------------------------------------+   |
|  |         CloudZenChatbot.razor (Blazor WASM)               |   |
|  |                                                           |   |
|  |  - Floating chat widget (FAB button)                      |   |
|  |  - Conversation UI with message bubbles                   |   |
|  |  - Suggested questions (quick-start chips)                |   |
|  |  - 5-question client-side cap                             |   |
|  |  - "Book a Free Consultation" CTA after limit             |   |
|  |  - "X questions remaining" counter                        |   |
|  +----------------------------+-----------------------------+   |
|                               | HTTP POST /api/chat              |
+-------------------------------+---------------------------------+
                                |
                                v
+-----------------------------------------------------------------+
|              AZURE FUNCTIONS API (Server)                        |
|                                                                  |
|  +----------------------------------------------------------+   |
|  |              ChatFunction.cs                              |   |
|  |                                                           |   |
|  |  1. CORS headers & preflight handling                     |   |
|  |  2. Security headers                                      |   |
|  |  3. Per-IP rate limiting (Polly)                          |   |
|  |  4. Input validation & size checks                        |   |
|  |  5. Conversation history trimming (last 6 msgs)           |   |
|  |  6. System prompt injection (knowledge base)              |   |
|  |  7. Anthropic API proxy call                              |   |
|  |  8. Response truncation (<=500 chars)                     |   |
|  |  9. Error classification & handling                       |   |
|  +----------------------------+-----------------------------+   |
|                               |                                  |
|  +----------------------------+-----------------------------+   |
|  |         Supporting Services                               |   |
|  |  - PollyRateLimiterService (per-client rate limits)       |   |
|  |  - InputValidator (sanitization)                          |   |
|  |  - CorsSettings (origin validation)                       |   |
|  |  - IHttpClientFactory ("SecureClient")                    |   |
|  +----------------------------------------------------------+   |
|                               |                                  |
+-------------------------------+---------------------------------+
                                | HTTP POST (x-api-key header)
                                v
+-----------------------------------------------------------------+
|              ANTHROPIC API (External)                            |
|                                                                  |
|  Endpoint: https://api.anthropic.com/v1/messages                |
|  Model:    claude-sonnet-4-20250514                               |
|  Version:  2023-06-01                                           |
|                                                                  |
|  Receives: system prompt + trimmed conversation history         |
|  Returns:  JSON with content[].text blocks                      |
+-----------------------------------------------------------------+
```

### Architecture Highlights

- **Blazor WebAssembly** runs entirely in the browser -- no server-side rendering required.
- **Azure Functions** (isolated worker, .NET 8) acts as a secure proxy -- the client **never** contacts Anthropic directly.
- The **API key** and **knowledge base** exist only on the server.
- **Azure Static Web Apps** links the Blazor frontend to the Functions API under the same domain (`/api/chat`).

---

## 3. Project Structure

```
CloudZen/
+-- CloudZen.csproj                          # Blazor WASM frontend
|   +-- Features/Chat/
|   |   +-- Components/
|   |   |   +-- CloudZenChatbot.razor        # Chat widget UI component
|   |   |   +-- CloudZenChatbot.razor.cs     # Chat logic (code-behind)
|   |   |   +-- CloudZenChatbot.razor.css    # Scoped styles (dark theme)
|   |   +-- Models/
|   |   |   +-- ChatMessage.cs               # Client-side message model
|   |   |   +-- ChatResult.cs                # Result type (Ok/Fail pattern)
|   |   +-- Services/
|   |   |   +-- IChatbotService.cs           # Service interface
|   |   |   +-- ChatbotService.cs            # HTTP client -> Azure Function
|   |   +-- ChatbotOptions.cs                # Client config (URL, timeout)
|   +-- wwwroot/
|       +-- appsettings.json                 # Base config
|       +-- appsettings.Development.json     # Local dev (localhost:7257)
|       +-- appsettings.Production.json      # Production API URL
|
+-- Api/CloudZen.Api.csproj                  # Azure Functions backend
    +-- Features/Chat/
    |   +-- ChatFunction.cs                  # Main chat endpoint + knowledge base
    +-- Models/
    |   +-- ChatRequest.cs                   # API request model
    |   +-- ChatResponse.cs                  # API response model
    |   +-- Options/
    |       +-- RateLimitOptions.cs           # Rate limiting config
    +-- Services/
    |   +-- IRateLimiterService.cs            # Rate limiter interface
    |   +-- RateLimiterService.cs             # Polly-based implementation
    +-- Security/
    |   +-- InputValidator.cs                # Input sanitization
    +-- local.settings.json                  # Local dev settings
```

---

## 4. User Flow

### Summary

| Step | Action | Component |
|------|--------|-----------|
| 1 | User clicks floating chat button (FAB) | `CloudZenChatbot.razor` |
| 2 | Chat panel slides open | CSS animation |
| 3 | User types message or clicks a suggested question | Input field / chips |
| 4 | Message sent to API | `ChatbotService` |
| 5 | AI response displayed | Message list |
| 6 | Conversation continues (up to 5 user messages) | History maintained |
| 7 | After 5th message, CTA replaces input | CTA button |
| 8 | User closes panel or clicks outside | Panel closes |

### End-to-End Detail

```
User clicks chat FAB -> Chat panel opens
        |
        v
User types message (or clicks suggested question)
        |
        v
CloudZenChatbot.razor:
  +-- Validates: not empty, not loading, under 5-message limit
  +-- Adds user message to local conversation list
  +-- Shows typing indicator
  +-- Calls ChatbotService.SendMessageAsync(messages)
        |
        v
ChatbotService.cs:
  +-- Serializes full conversation history as JSON
  +-- POST -> /api/chat
        |
        v
ChatFunction.cs (Azure Function):
  +-- Adds CORS + security headers
  +-- Checks rate limit (Polly, per-IP)
  +-- Validates request body (size, format, message count)
  +-- Validates each message (role, content length -- user only)
  +-- Retrieves API key from config/env/Key Vault
  +-- Trims conversation to last 6 messages
  +-- Ensures first message is role "user"
  +-- Injects system prompt + knowledge base
  +-- Calls Anthropic API (claude-sonnet-4-20250514, max 200 tokens)
  +-- Parses response, extracts text
  +-- Truncates to <=500 characters at sentence boundary
  +-- Returns ChatResponse { Success, Reply }
        |
        v
ChatbotService.cs:
  +-- Returns ChatResult.Ok(reply) or ChatResult.Fail(error)
        |
        v
CloudZenChatbot.razor:
  +-- Adds assistant message to conversation
  +-- If 5th message: adds final CTA message
  +-- If limit reached: replaces input with "Book a Free Consultation" CTA
  +-- StateHasChanged() -> UI updates
```

---

## 5. Components

### Razor Components

| Component | Location | Purpose |
|-----------|----------|---------|
| `CloudZenChatbot.razor` | `Features/Chat/Components/` | Main chatbot UI (FAB + panel) |
| `CloudZenChatbot.razor.cs` | `Features/Chat/Components/` | Chat logic (code-behind) |
| `CloudZenChatbot.razor.css` | `Features/Chat/Components/` | Scoped CSS (animations, scrollbar) |

### Services

| Service | Location | Purpose |
|---------|----------|---------|
| `ChatbotService` | `Features/Chat/Services/` | API client for chat endpoint. Uses `HttpClient` with configurable 60s timeout. Sends full conversation history. Returns `ChatResult` (Ok/Fail pattern). |
| `IChatbotService` | `Features/Chat/Services/` | Service interface |

### Models

| Model | Location | Purpose |
|-------|----------|---------|
| `ChatMessage` | `Features/Chat/Models/` | Message with role + content. Factory methods: `ChatMessage.User(content)`, `ChatMessage.Assistant(content)` |
| `ChatResult` | `Features/Chat/Models/` | Result type with `Ok()`/`Fail()` |
| `ChatbotOptions` | `Features/Chat/` | Configuration options (URL, timeout) |

---

## 6. API Integration

| Property | Value |
|----------|-------|
| **Endpoint** | `POST /api/chat` (also OPTIONS for CORS preflight) |
| **Function** | `Api/Features/Chat/ChatFunction.cs` |
| **Auth Level** | Anonymous (rate-limited instead) |
| **Runtime** | .NET 8 isolated worker |
| **External API** | Anthropic Claude (`https://api.anthropic.com/v1/messages`) |
| **Model** | `claude-sonnet-4-20250514` |

### Proxy Architecture

```
Blazor WASM (no secrets) -> Azure Function (holds API key) -> Anthropic Claude API
```

**Why proxy?**
- API key never exposed to client browser
- Rate limiting enforced server-side (Polly)
- System prompt / knowledge base kept confidential
- Input validation and sanitization on the server

### Request Pipeline (ChatFunction.cs)

| Step | Action |
|------|--------|
| 1 | CORS headers added to all responses |
| 2 | Preflight handling -- returns 204 for OPTIONS |
| 3 | Security headers added |
| 4 | Rate limiting -- per-IP, Polly-based fixed window |
| 5 | Body validation -- size, format, deserialization |
| 6 | Message validation -- role, content length (user messages only) |
| 7 | API key retrieval from `IConfiguration` or environment variable |
| 8 | Anthropic API call with trimmed history + system prompt |
| 9 | Response parsing -- extract text from content blocks |
| 10 | Response truncation -- <=500 chars at sentence boundary |
| 11 | Error classification -- billing, rate limit, generic HTTP, timeout |

### Rate Limiter: PollyRateLimiterService

| Property | Detail |
|----------|--------|
| **Library** | Polly resilience pipelines |
| **Scope** | Per-client (keyed by IP + endpoint) |
| **Algorithm** | Fixed window (default: 10 requests per 60 seconds) |
| **Queue limit** | 0 (immediate rejection) |
| **Circuit breaker** | Optional, for cascading failure protection |
| **Memory** | Automatic cleanup of inactive client limiters |
| **Exceeded response** | HTTP 429 with `Retry-After` header |

---

## 7. AI Provider -- Anthropic Claude

### Model Configuration

| Setting | Value | Rationale |
|---|---|---|
| **Model** | `claude-sonnet-4-20250514` | Best balance of quality, speed, and cost |
| **Max Tokens** | `200` | ~800 chars max; naturally constrains output length |
| **Anthropic Version** | `2023-06-01` | Stable API version |

### Knowledge Base

The knowledge base is a comprehensive `const string` stored server-side in `ChatFunction.cs` (~800 lines). It is **never sent to the client**. Contents:

| Section | Content |
|---------|---------|
| Identity and Brand | Name, tagline, positioning, contact info |
| Mission and Values | Core promise, differentiators |
| Services (9 categories) | Custom software, cloud, legacy modernization, DevOps, dashboards, AI automation, specialist network, QA, agile delivery |
| Case Studies (3 projects) | Assessment platform, SAP pipeline, AI menu optimizer |
| Process | 6-step: consultation, discovery, proposal, build, launch, support |
| Ideal Client Profile | Non-technical small business owners |
| Pain Points | Specific problems CloudZen solves |
| Technology Expertise | Azure, Blazor, .NET, AI/ML, data pipelines |
| Contact and Booking | Email, response time, consultation process |
| Tone Guidelines | Warm, jargon-free, outcome-focused |

### System Prompt Rules

| Rule | Purpose |
|---|---|
| <=500 characters per response | Cost control; keeps responses scannable |
| 1-2 sentences max | Prevents lengthy explanations |
| Always suggest next step | Every answer ends with consultation CTA |
| No detailed technical advice | Redirects to real conversation |
| No pricing/timeline specifics | Forces consultation booking |
| No off-topic engagement | Rejects jokes, roleplay, unrelated questions |
| Not a general-purpose AI | Scoped exclusively to CloudZen |
| Proactive consultation redirect | After 2-3 questions, suggests booking |

---

## 8. Request/Response

### Request Body

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| `messages` | array | Yes | Max 10 messages |
| `messages[].role` | string | Yes | `"user"` or `"assistant"` |
| `messages[].content` | string | Yes | Max 500 chars (user messages) |

### Response

| Field | Type | Description |
|-------|------|-------------|
| `success` | boolean | Operation result |
| `reply` | string | AI response text |
| `message` | string | Error message (on failure) |

---

## 9. Configuration

### Frontend -- Blazor (`wwwroot/appsettings.json`)

```json
{
  "ChatbotService": {
    "ApiBaseUrl": "/api",
    "TimeoutSeconds": 60,
    "ChatEndpoint": "chat"
  }
}
```

| Setting | File | Purpose |
|---------|------|---------|
| `ChatbotService.ApiBaseUrl` | `appsettings.json` | API endpoint base URL |
| `ChatbotService.TimeoutSeconds` | `appsettings.json` | HTTP client timeout |
| `ChatbotService.ChatEndpoint` | `appsettings.json` | Endpoint path |

### Backend -- Azure Functions (`local.settings.json`)

```json
{
  "Values": {
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ANTHROPIC_API_KEY": "<your-anthropic-api-key>",
    "RateLimiting:PermitLimit": "10",
    "RateLimiting:WindowSeconds": "60",
    "RateLimiting:QueueLimit": "0",
    "RateLimiting:EnableCircuitBreaker": "false",
    "RateLimiting:CircuitBreakerFailureThreshold": "5",
    "RateLimiting:CircuitBreakerDurationSeconds": "30",
    "RateLimiting:InactivityTimeoutMinutes": "5"
  }
}
```

### Constants in ChatFunction.cs

| Constant | Value | Description |
|---|---|---|
| `AnthropicApiUrl` | `https://api.anthropic.com/v1/messages` | Anthropic Messages API endpoint |
| `AnthropicVersion` | `2023-06-01` | API version header |
| `DefaultModel` | `claude-sonnet-4-20250514` | Claude model identifier |
| `MaxTokens` | `200` | Max tokens per AI response |
| `MaxRequestBodySize` | `15,000` | Max request body in bytes |
| `MaxMessages` | `10` | Max messages per request |
| `MaxConversationHistoryMessages` | `6` | Messages sent to Anthropic (trimmed) |
| `MaxMessageContentLength` | `500` | Max user message characters |
| `MaxReplyLength` | `500` | Max reply characters (truncation) |

### Constants in CloudZenChatbot.razor

| Constant | Value | Description |
|---|---|---|
| `MaxUserMessages` | `5` | Client-side user message cap per session |

---

## 10. Security and Abuse Prevention

### Multi-Layer Security Model

```
Layer 1 -- CLIENT-SIDE
+-- 5 user messages max per session
+-- Input disabled after limit
+-- Suggested questions (controlled vocabulary)
+-- Textarea with placeholder guidance

Layer 2 -- API VALIDATION
+-- Max request body: 15,000 bytes
+-- Max messages per request: 10
+-- Max user message length: 500 characters
+-- Role validation: only "user" or "assistant"
+-- Content validation: non-empty, non-whitespace
+-- JSON depth limit: 10

Layer 3 -- RATE LIMITING
+-- Polly-based per-IP rate limiter
+-- Default: 10 requests / 60 seconds
+-- Queue limit: 0 (immediate rejection)
+-- Optional circuit breaker
+-- Automatic inactive client cleanup
+-- Retry-After header on 429 responses

Layer 4 -- API KEY SECURITY
+-- Anthropic API key in Azure Key Vault / environment variables
+-- Never exposed to client browser
+-- Knowledge base stays server-side only
+-- CORS + security headers on all responses

Layer 5 -- AI PROMPT HARDENING
+-- Off-topic rejection instruction
+-- No roleplay / joke engagement
+-- Scoped to CloudZen topics only
+-- No detailed technical implementation advice
+-- Pricing/timeline -> "book a consultation"

Layer 6 -- RESPONSE CONTROLS
+-- Max 200 tokens per response
+-- Server-side truncation at 500 characters
+-- Sentence-boundary-aware truncation
+-- Empty response fallback message
```

### Summary of Protection Rules

| Rule | Where | Value |
|---|---|---|
| User messages per session | Client (Blazor) | 5 max |
| User message length | API validation | 500 chars |
| Messages per API request | API validation | 10 max |
| Request body size | API validation | 15 KB |
| Conversation history to Anthropic | API (trim) | Last 6 messages |
| AI response tokens | Anthropic `max_tokens` | 200 |
| AI response length | API truncation | 500 chars |
| Rate limit | API (Polly) | 10 req / 60s per IP |
| Off-topic rejection | System prompt | Instruction-based |
| Pricing/timeline deflection | System prompt | Instruction-based |
| Response brevity | System prompt + token limit | 1-2 sentences |

---

## 11. Token Consumption Controls

Total cost per conversation is controlled at every level:

| Control | Setting | Impact |
|---|---|---|
| **Max tokens per response** | 200 | ~50-100 words per reply |
| **Max reply characters** | 500 | Server-side hard truncation |
| **System prompt instruction** | "<=500 chars, 1-2 sentences" | Guides model to be concise |
| **Conversation history trim** | Last 6 messages only | Older messages dropped before API call |
| **Client conversation cap** | 5 user messages | Max 5 API calls per session |
| **Rate limit** | 10 requests / 60 seconds | Per-IP burst protection |
| **User message length** | 500 characters max | Limits input token count |
| **Max messages per request** | 10 | Prevents oversized payloads |

### Estimated Token Budget Per Conversation

| Component | Estimated Tokens |
|---|---|
| System prompt (knowledge base) | ~2,500 (fixed, sent each call) |
| Conversation history (6 msgs x ~100 tokens) | ~600 |
| Response generation | <=200 |
| **Total per API call** | ~3,300 |
| **Total per session (5 calls)** | ~16,500 |

---

## 12. Lead Generation and Conversion Strategy

The chatbot is designed as a **lead qualification funnel**, not a support tool.

### Conversion Tactics

| Tactic | Mechanism |
|---|---|
| Suggested questions | Pre-populated, conversion-optimized topics ("How do I get started?") |
| Short answers | 1-2 sentences create curiosity, not satisfaction |
| Every answer includes CTA | System prompt mandates suggesting consultation or email |
| Pricing deflection | "That depends on your situation" then book consultation |
| 5-question hard limit | Forces transition from chatbot to real conversation |
| Final CTA message | Bot's last message explicitly asks them to email |
| Visual CTA button | Styled "Book a Free Consultation" mailto link replaces input |
| Footer reinforcement | Email address + "Replies within 24h" always visible |
| Proactive redirect | After 2-3 questions, prompt suggests consultation unprompted |

### Conversion Funnel

```
Visitor lands on site
        |
        v
Sees floating chat FAB -> Curiosity click
        |
        v
Reads welcome message + suggested questions -> Low-friction engagement
        |
        v
Asks 1-2 questions -> Gets helpful but brief answers with CTAs
        |
        v
Asks 3rd question -> Bot proactively suggests consultation
        |
        v
Asks 4th-5th question -> Counter shows "1 question remaining"
        |
        v
Limit reached -> "Book a Free Consultation" CTA replaces input
        |
        v
Clicks CTA -> mailto:cloudzen.inc@gmail.com (pre-filled subject)
```

---

## 13. Error Handling

### Backend Error Classification (ChatFunction.cs)

The backend parses Anthropic error responses and classifies them:

```csharp
// Billing errors (insufficient credits)
if (statusCode == 400 && body.Contains("credit balance is too low"))
    // throws "billing error" -> caught -> 503 Service Unavailable

// Rate limit errors
if (statusCode == 429 || errorType == "rate_limit_error")
    // throws "rate limit" -> caught -> 429 Too Many Requests

// All other errors
    // throws generic -> caught -> 500 Internal Server Error
```

### Error Response Matrix

| Error | HTTP Status | User Message | Logged |
|-------|-------------|--------------|--------|
| Rate limited (app) | 429 | Rate limiter message | Yes |
| Rate limited (Anthropic) | 429 | "The AI service is currently busy." | Yes |
| Billing/credits issue | 503 | "AI service temporarily unavailable." | Yes |
| Generic HTTP error | 500 | "Unable to reach the AI service." | Yes (details) |
| Timeout | 500 | "The AI service took too long to respond." | Yes |
| Invalid JSON | 400 | "Invalid request format." | Yes |
| Network failure | -- | "Unable to connect to the chat service." | Yes |
| Unexpected error | 500 | "Something went wrong." | Yes |

### Client-Side Error Handling (ChatbotService.cs)

- **HTTP errors** -- parsed from response body or generic status message
- **Network errors** -- "Unable to connect to the chat service."
- **Timeouts** -- "Request timed out. Please try again."
- **Unexpected errors** -- "Something went wrong. Please try again later."

### UI Error Display (CloudZenChatbot.razor)

Errors are shown as assistant messages in the chat:

```csharp
messages.Add(ChatMessage.Assistant(
    result.Error ?? "Something went wrong. Please email cloudzen.inc@gmail.com directly."));
```

---

## 14. UI Components

### Floating Action Button (FAB)

| State | Appearance |
|-------|------------|
| Default | Chat icon, orange background, pulse animation |
| Panel open | Close icon (X) |
| Hover | Scale up, shadow increase |

### Chat Panel

| Element | Description |
|---------|-------------|
| Header | "CloudZen Assistant" title, close button |
| Dimensions | 380x560px dark-themed container |
| Message list | Scrollable, auto-scroll to bottom |
| User message | Right-aligned, blue bubble with avatar |
| Assistant message | Left-aligned, dark bubble with avatar |
| Input area | Text input + send button (Enter to send, Shift+Enter for newline) |
| Loading state | Three animated dots typing indicator |
| Suggested questions | 4 quick-start chips shown on first open |
| Questions counter | Footer shows "X questions remaining" |
| CTA after limit | Input replaced with styled "Book a Free Consultation" mailto button |

### Suggested Questions (Conversion-Optimized)

```
"What does CloudZen do?"
"Can you help modernize my old system?"
"How do I get started?"
"Tell me about your past projects"
```

### Message Types

| Role | Factory Method | Display |
|------|----------------|---------|
| `user` | `ChatMessage.User(content)` | Right-aligned, blue bubble |
| `assistant` | `ChatMessage.Assistant(content)` | Left-aligned, dark bubble |

### Scoped CSS Features

| Feature | Purpose |
|---------|---------|
| Panel slide animation | Smooth open/close |
| Custom scrollbar | Styled scrollbar for message list |
| Typing indicator | Animated dots during API call |
| Message transitions | Fade-in for new messages |

### Entry Points

| Location | Element | Action |
|----------|---------|--------|
| All pages | Floating chat button (bottom-right) | Opens chat panel |

---

## 15. Local Development

### Prerequisites

- .NET 8 SDK
- Azure Functions Core Tools v4
- Azure Storage Emulator or Azurite
- Anthropic API key with credits

### Running Locally

**Terminal 1 -- Azure Functions API:**

```powershell
cd Api
func start --port 7257
```

**Terminal 2 -- Blazor WASM Frontend:**

```powershell
dotnet run
```

Open `https://localhost:7243` and click the chat FAB.

### Development Configuration

`wwwroot/appsettings.Development.json` points to local Functions:

```json
{
  "ChatbotService": {
    "ApiBaseUrl": "http://localhost:7257/api"
  }
}
```

---

## 16. Deployment

### Production Architecture

```
Azure Static Web Apps
+-- Frontend: Blazor WASM (static files)
+-- Linked API: Azure Functions (.NET 8 isolated)
```

### Production Configuration

`wwwroot/appsettings.Production.json`:

```json
{
  "ChatbotService": {
    "ApiBaseUrl": "https://cloudzen-api-func-e4gehdaef9ftdhbn.westus2-01.azurewebsites.net/api"
  }
}
```

### Required Environment Variables (Azure Function App)

| Variable | Source | Description |
|---|---|---|
| `ANTHROPIC_API_KEY` | Azure Key Vault | Anthropic API key |
| `RateLimiting:PermitLimit` | App Settings | Requests per window |
| `RateLimiting:WindowSeconds` | App Settings | Rate limit window |

---

## 17. Testing Guide

### Client-Side: Conversation Cap

1. Open chatbot widget
2. Send 5 messages -- verify footer shows decreasing "X questions remaining"
3. After 5th message: input replaced with CTA button, final bot CTA message appears

### API: Message Validation

```javascript
// Too-long user message (expect 400)
fetch("/api/chat", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
        messages: [{ role: "user", content: "A".repeat(501) }]
    })
}).then(r => r.json()).then(console.log);
```

### API: Too Many Messages

```javascript
// 11 messages (expect 400)
const msgs = Array.from({length: 11}, (_, i) => ({
    role: i % 2 === 0 ? "user" : "assistant", content: "test"
}));
fetch("/api/chat", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ messages: msgs })
}).then(r => r.json()).then(console.log);
```

### API: Rate Limiting

```javascript
// Burst 11 requests (expect 11th -> 429)
for (let i = 0; i < 11; i++) {
    fetch("/api/chat", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ messages: [{ role: "user", content: "hi" }] })
    }).then(r => console.log(`Request ${i+1}: ${r.status}`));
}
```

### AI Behavior (requires Anthropic credits)

| Test | Expected Behavior |
|---|---|
| Off-topic: "Write me a poem" | Politely redirects to CloudZen topics |
| Pricing: "How much does it cost?" | "Depends on your situation" then book consultation |
| 3+ questions in a row | Proactively suggests consultation |
| Long question about implementation | High-level answer only, redirects to consultation |
| Reply length | Every response <=500 characters |

---

## 18. Related Docs

- [API_ENDPOINTS.md](../01-architecture/API_ENDPOINTS.md) -- Full endpoint specification including request/response schemas for all API routes
- [01_azure_functions_proxy_api.md](../06-patterns/01_azure_functions_proxy_api.md) -- Proxy pattern documentation covering the shared Azure Functions architecture
- [CONFIGURATION.md](../01-architecture/CONFIGURATION.md) -- Options pattern and `IOptions<T>` binding conventions used across the app
