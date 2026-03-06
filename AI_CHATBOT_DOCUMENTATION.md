# CloudZen AI Chatbot — Technical Documentation

> **Version:** 1.0  
> **Last Updated:** March 2026  
> **Branch:** `ai-chatbot-tool-integration`  
> **Status:** Active Development

---

## Table of Contents

1. [Overview](#1-overview)
2. [Architecture](#2-architecture)
3. [Project Structure](#3-project-structure)
4. [How It Works — End-to-End Flow](#4-how-it-works--end-to-end-flow)
5. [Frontend — Blazor WebAssembly UI](#5-frontend--blazor-webassembly-ui)
6. [Backend — Azure Functions API](#6-backend--azure-functions-api)
7. [AI Provider — Anthropic Claude](#7-ai-provider--anthropic-claude)
8. [Security & Abuse Prevention](#8-security--abuse-prevention)
9. [Token Consumption Controls](#9-token-consumption-controls)
10. [Lead Generation & Conversion Strategy](#10-lead-generation--conversion-strategy)
11. [Configuration Reference](#11-configuration-reference)
12. [Error Handling](#12-error-handling)
13. [Local Development](#13-local-development)
14. [Deployment](#14-deployment)
15. [Testing Guide](#15-testing-guide)

---

## 1. Overview

The CloudZen AI Chatbot is a website-embedded conversational assistant designed to:

- **Answer visitor questions** about CloudZen's services, process, and portfolio
- **Convert visitors into leads** by guiding them toward booking a free consultation
- **Protect against abuse** with multi-layered rate limiting, input validation, and conversation caps
- **Minimize API costs** through strict token consumption controls

The chatbot is **not** a general-purpose AI assistant. It is scoped exclusively to CloudZen's business context and trained via a server-side knowledge base that is never exposed to the client.

### Key Design Principles

| Principle | Implementation |
|---|---|
| **Security first** | API key stays server-side; knowledge base never sent to client |
| **Cost control** | Capped tokens, capped messages, capped reply length, conversation history trimming |
| **Lead conversion** | 5-question limit → CTA to book consultation; system prompt always redirects to outreach |
| **Jargon-free** | System prompt enforces plain English, 1-2 sentence responses |
| **Abuse resistant** | Per-IP rate limiting, input validation, off-topic rejection via prompt |

---

## 2. Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    BROWSER (Client)                          │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐   │
│  │         CloudZenChatbot.razor (Blazor WASM)          │   │
│  │                                                      │   │
│  │  • Floating chat widget (FAB button)                 │   │
│  │  • Conversation UI with message bubbles              │   │
│  │  • Suggested questions (quick-start chips)           │   │
│  │  • 5-question client-side cap                        │   │
│  │  • "Book a Free Consultation" CTA after limit        │   │
│  │  • "X questions remaining" counter                   │   │
│  └─────────────────────┬────────────────────────────────┘   │
│                        │ HTTP POST /api/chat                │
└────────────────────────┼────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              AZURE FUNCTIONS API (Server)                    │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐   │
│  │              ChatFunction.cs                          │   │
│  │                                                      │   │
│  │  1. CORS headers & preflight handling                │   │
│  │  2. Security headers                                 │   │
│  │  3. Per-IP rate limiting (Polly)                     │   │
│  │  4. Input validation & size checks                   │   │
│  │  5. Conversation history trimming (last 6 msgs)      │   │
│  │  6. System prompt injection (knowledge base)         │   │
│  │  7. Anthropic API proxy call                         │   │
│  │  8. Response truncation (≤500 chars)                 │   │
│  │  9. Error classification & handling                  │   │
│  └─────────────────────┬────────────────────────────────┘   │
│                        │                                    │
│  ┌─────────────────────┴────────────────────────────────┐   │
│  │         Supporting Services                           │   │
│  │  • PollyRateLimiterService (per-client rate limits)   │   │
│  │  • InputValidator (sanitization)                      │   │
│  │  • CorsSettings (origin validation)                   │   │
│  │  • IHttpClientFactory ("SecureClient")                │   │
│  └──────────────────────────────────────────────────────┘   │
│                        │                                    │
└────────────────────────┼────────────────────────────────────┘
                         │ HTTP POST (x-api-key header)
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              ANTHROPIC API (External)                        │
│                                                             │
│  Endpoint: https://api.anthropic.com/v1/messages            │
│  Model:    claude-sonnet-4-20250514                           │
│  Version:  2023-06-01                                       │
│                                                             │
│  Receives: system prompt + trimmed conversation history     │
│  Returns:  JSON with content[].text blocks                  │
└─────────────────────────────────────────────────────────────┘
```

### Architecture Highlights

- **Blazor WebAssembly** runs entirely in the browser — no server-side rendering required
- **Azure Functions** (isolated worker, .NET 8) acts as a secure proxy — the client **never** contacts Anthropic directly
- The **API key** and **knowledge base** exist only on the server
- **Azure Static Web Apps** links the Blazor frontend to the Functions API under the same domain (`/api/chat`)

---

## 3. Project Structure

```
CloudZen/
├── CloudZen.csproj                          # Blazor WASM frontend
│   ├── Shared/Chatbot/
│   │   ├── CloudZenChatbot.razor            # Chat widget UI component
│   │   └── CloudZenChatbot.razor.css        # Scoped styles (dark theme)
│   ├── Services/
│   │   ├── Abstractions/
│   │   │   └── IChatbotService.cs           # Service interface
│   │   └── ChatbotService.cs               # HTTP client → Azure Function
│   ├── Models/
│   │   ├── ChatMessage.cs                   # Client-side message model
│   │   └── Options/
│   │       └── ChatbotOptions.cs            # Client config (URL, timeout)
│   └── wwwroot/
│       ├── appsettings.json                 # Base config
│       ├── appsettings.Development.json     # Local dev (localhost:7257)
│       └── appsettings.Production.json      # Production API URL
│
├── Api/CloudZen.Api.csproj                  # Azure Functions backend
│   ├── Functions/
│   │   └── ChatFunction.cs                  # Main chat endpoint + knowledge base
│   ├── Models/
│   │   ├── ChatRequest.cs                   # API request model
│   │   ├── ChatResponse.cs                  # API response model
│   │   └── Options/
│   │       └── RateLimitOptions.cs          # Rate limiting config
│   ├── Services/
│   │   ├── IRateLimiterService.cs           # Rate limiter interface
│   │   └── RateLimiterService.cs            # Polly-based implementation
│   ├── Security/
│   │   └── InputValidator.cs                # Input sanitization
│   └── local.settings.json                  # Local dev settings
│
└── AI_CHATBOT_DOCUMENTATION.md              # This file
```

---

## 4. How It Works — End-to-End Flow

```
User clicks chat FAB → Chat panel opens
        │
        ▼
User types message (or clicks suggested question)
        │
        ▼
CloudZenChatbot.razor:
  ├── Validates: not empty, not loading, under 5-message limit
  ├── Adds user message to local conversation list
  ├── Shows typing indicator
  └── Calls ChatbotService.SendMessageAsync(messages)
        │
        ▼
ChatbotService.cs:
  ├── Serializes full conversation history as JSON
  └── POST → /api/chat
        │
        ▼
ChatFunction.cs (Azure Function):
  ├── Adds CORS + security headers
  ├── Checks rate limit (Polly, per-IP)
  ├── Validates request body (size, format, message count)
  ├── Validates each message (role, content length — user only)
  ├── Retrieves API key from config/env/Key Vault
  ├── Trims conversation to last 6 messages
  ├── Ensures first message is role "user"
  ├── Injects system prompt + knowledge base
  ├── Calls Anthropic API (claude-sonnet-4-20250514, max 200 tokens)
  ├── Parses response, extracts text
  ├── Truncates to ≤500 characters at sentence boundary
  └── Returns ChatResponse { Success, Reply }
        │
        ▼
ChatbotService.cs:
  └── Returns ChatResult.Ok(reply) or ChatResult.Fail(error)
        │
        ▼
CloudZenChatbot.razor:
  ├── Adds assistant message to conversation
  ├── If 5th message: adds final CTA message
  ├── If limit reached: replaces input with "Book a Free Consultation" CTA
  └── StateHasChanged() → UI updates
```

---

## 5. Frontend — Blazor WebAssembly UI

### Component: `CloudZenChatbot.razor`

| Feature | Detail |
|---|---|
| **Toggle** | Floating Action Button (FAB) in bottom-right corner |
| **Chat panel** | 380×560px dark-themed container with header, messages, input |
| **Message bubbles** | User (blue, right-aligned) / Bot (dark, left-aligned) with avatars |
| **Typing indicator** | Three animated dots while waiting for API response |
| **Suggested questions** | 4 quick-start chips shown on first open |
| **Conversation cap** | 5 user messages max per session |
| **Questions counter** | Footer shows "X questions remaining" |
| **CTA after limit** | Input replaced with styled "📧 Book a Free Consultation" mailto button |
| **Final CTA message** | Bot sends a closing message encouraging email outreach |
| **Keyboard support** | Enter to send, Shift+Enter for newline |

### Suggested Questions (Conversion-Optimized)

```
"What does CloudZen do?"
"Can you help modernize my old system?"
"How do I get started?"
"Tell me about your past projects"
```

### Service: `ChatbotService.cs`

- Implements `IChatbotService`
- Uses `HttpClient` with configurable timeout (60s default)
- Sends full conversation history to `/api/chat`
- Handles HTTP errors, timeouts, and deserialization failures gracefully
- Returns `ChatResult` (Success/Fail pattern)

### Configuration: `ChatbotOptions.cs`

```json
{
  "ChatbotService": {
    "ApiBaseUrl": "/api",
    "TimeoutSeconds": 60,
    "ChatEndpoint": "chat"
  }
}
```

---

## 6. Backend — Azure Functions API

### Function: `ChatFunction.cs`

- **Trigger:** HTTP POST `/api/chat` (also OPTIONS for CORS preflight)
- **Auth Level:** Anonymous (rate-limited instead)
- **Runtime:** .NET 8 isolated worker

### Request Pipeline

1. **CORS headers** — added to all responses
2. **Preflight handling** — returns 204 for OPTIONS
3. **Security headers** — added to response
4. **Rate limiting** — per-IP, Polly-based fixed window
5. **Body validation** — size, format, deserialization
6. **Message validation** — role, content length (user messages only)
7. **API key retrieval** — from `IConfiguration` or environment variable
8. **Anthropic API call** — with trimmed history + system prompt
9. **Response parsing** — extract text from content blocks
10. **Response truncation** — ≤500 chars at sentence boundary
11. **Error classification** — billing, rate limit, generic HTTP, timeout

### Rate Limiter: `PollyRateLimiterService`

- Built on **Polly** resilience pipelines
- **Per-client** rate limiting (keyed by IP + endpoint)
- **Fixed window** algorithm (default: 10 requests per 60 seconds)
- Optional **circuit breaker** for cascading failure protection
- **Automatic cleanup** of inactive client limiters (memory management)
- Configurable via `RateLimitOptions`

---

## 7. AI Provider — Anthropic Claude

### Model Configuration

| Setting | Value | Rationale |
|---|---|---|
| **Model** | `claude-sonnet-4-20250514` | Best balance of quality, speed, and cost |
| **Max Tokens** | `200` | ~800 chars max; naturally constrains output length |
| **Anthropic Version** | `2023-06-01` | Stable API version |

### Knowledge Base

The knowledge base is a comprehensive `const string` stored server-side in `ChatFunction.cs`. It contains:

- **Identity & Brand** — name, tagline, positioning, contact info
- **Mission & Values** — core promise, differentiators
- **Services** (9 categories) — custom software, cloud, legacy modernization, DevOps, dashboards, AI automation, specialist network, QA, agile delivery
- **Case Studies** (3 projects) — assessment platform, SAP pipeline, AI menu optimizer
- **Process** — 6-step: consultation → discovery → proposal → build → launch → support
- **Ideal Client Profile** — non-technical small business owners
- **Pain Points** — the specific problems CloudZen solves
- **Technology Expertise** — Azure, Blazor, .NET, AI/ML, data pipelines
- **Contact & Booking** — email, response time, consultation process
- **Tone Guidelines** — warm, jargon-free, outcome-focused

### System Prompt Rules

The system prompt enforces these behavioral constraints:

| Rule | Purpose |
|---|---|
| **≤500 characters per response** | Cost control; keeps responses scannable |
| **1-2 sentences max** | Prevents lengthy explanations |
| **Always suggest next step** | Every answer ends with consultation CTA |
| **No detailed technical advice** | Redirects to real conversation |
| **No pricing/timeline specifics** | Forces consultation booking |
| **No off-topic engagement** | Rejects jokes, roleplay, unrelated questions |
| **Not a general-purpose AI** | Scoped exclusively to CloudZen |
| **Proactive consultation redirect** | After 2-3 questions, suggests booking |

---

## 8. Security & Abuse Prevention

### Multi-Layer Security Model

```
Layer 1 — CLIENT-SIDE
├── 5 user messages max per session
├── Input disabled after limit
├── Suggested questions (controlled vocabulary)
└── Textarea with placeholder guidance

Layer 2 — API VALIDATION
├── Max request body: 15,000 bytes
├── Max messages per request: 10
├── Max user message length: 500 characters
├── Role validation: only "user" or "assistant"
├── Content validation: non-empty, non-whitespace
└── JSON depth limit: 10

Layer 3 — RATE LIMITING
├── Polly-based per-IP rate limiter
├── Default: 10 requests / 60 seconds
├── Queue limit: 0 (immediate rejection)
├── Optional circuit breaker
├── Automatic inactive client cleanup
└── Retry-After header on 429 responses

Layer 4 — API KEY SECURITY
├── Anthropic API key in Azure Key Vault / environment variables
├── Never exposed to client browser
├── Knowledge base stays server-side only
└── CORS + security headers on all responses

Layer 5 — AI PROMPT HARDENING
├── Off-topic rejection instruction
├── No roleplay / joke engagement
├── Scoped to CloudZen topics only
├── No detailed technical implementation advice
└── Pricing/timeline → "book a consultation"

Layer 6 — RESPONSE CONTROLS
├── Max 200 tokens per response
├── Server-side truncation at 500 characters
├── Sentence-boundary-aware truncation
└── Empty response fallback message
```

### Error Handling by Type

| Error | HTTP Status | User Message |
|---|---|---|
| Rate limited (app) | 429 | Rate limiter message |
| Rate limited (Anthropic) | 429 | "The AI service is currently busy." |
| Billing/credits issue | 503 | "AI service temporarily unavailable." |
| Generic HTTP error | 500 | "Unable to reach the AI service." |
| Timeout | 500 | "The AI service took too long to respond." |
| Invalid JSON | 400 | "Invalid request format." |
| Unexpected error | 500 | "Something went wrong." |

---

## 9. Token Consumption Controls

Total cost per conversation is controlled at every level:

| Control | Setting | Impact |
|---|---|---|
| **Max tokens per response** | 200 | ~50-100 words per reply |
| **Max reply characters** | 500 | Server-side hard truncation |
| **System prompt instruction** | "≤500 chars, 1-2 sentences" | Guides model to be concise |
| **Conversation history trim** | Last 6 messages only | Older messages dropped before API call |
| **Client conversation cap** | 5 user messages | Max 5 API calls per session |
| **Rate limit** | 10 requests / 60 seconds | Per-IP burst protection |
| **User message length** | 500 characters max | Limits input token count |
| **Max messages per request** | 10 | Prevents oversized payloads |

### Estimated Token Budget Per Conversation

| Component | Estimated Tokens |
|---|---|
| System prompt (knowledge base) | ~2,500 (fixed, sent once per call) |
| Conversation history (6 msgs × ~100 tokens) | ~600 |
| Response generation | ≤200 |
| **Total per API call** | ~3,300 |
| **Total per session (5 calls)** | ~16,500 |

---

## 10. Lead Generation & Conversion Strategy

The chatbot is designed as a **lead qualification funnel**, not a support tool:

### Conversion Tactics

1. **Suggested questions** — pre-populated, conversion-optimized topics ("How do I get started?")
2. **Short answers** — 1-2 sentences create curiosity, not satisfaction
3. **Every answer includes CTA** — system prompt mandates suggesting consultation or email
4. **Pricing deflection** — "That depends on your situation" → book consultation
5. **5-question hard limit** — forces transition from chatbot to real conversation
6. **Final CTA message** — bot's last message explicitly asks them to email
7. **Visual CTA button** — styled "📧 Book a Free Consultation" mailto link replaces input
8. **Footer reinforcement** — email address + "Replies within 24h" always visible
9. **Proactive redirect** — after 2-3 questions, prompt suggests consultation unprompted

### Conversion Funnel

```
Visitor lands on site
        │
        ▼
Sees floating chat FAB → Curiosity click
        │
        ▼
Reads welcome message + suggested questions → Low-friction engagement
        │
        ▼
Asks 1-2 questions → Gets helpful but brief answers with CTAs
        │
        ▼
Asks 3rd question → Bot proactively suggests consultation
        │
        ▼
Asks 4th-5th question → Counter shows "1 question remaining"
        │
        ▼
Limit reached → "Book a Free Consultation" CTA replaces input
        │
        ▼
Clicks CTA → mailto:cloudzen.inc@gmail.com (pre-filled subject)
```

---

## 11. Configuration Reference

### Azure Functions Backend (`local.settings.json`)

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

### Blazor Frontend (`wwwroot/appsettings.json`)

```json
{
  "ChatbotService": {
    "ApiBaseUrl": "/api",
    "TimeoutSeconds": 60,
    "ChatEndpoint": "chat"
  }
}
```

### Constants in `ChatFunction.cs`

| Constant | Value | Description |
|---|---|---|
| `AnthropicApiUrl` | `https://api.anthropic.com/v1/messages` | Anthropic Messages API endpoint |
| `AnthropicVersion` | `2023-06-01` | API version header |
| `DefaultModel` | `claude-sonnet-4-20250514` | Claude model identifier |
| `MaxTokens` | `200` | Max tokens per AI response |
| `MaxRequestBodySize` | `15,000` | Max request body in bytes |
| `MaxMessages` | `10` | Max messages per request |
| `MaxConversationHistoryMessages` | `6` | Messages sent to Anthropic (trim) |
| `MaxMessageContentLength` | `500` | Max user message characters |
| `MaxReplyLength` | `500` | Max reply characters (truncation) |

### Constants in `CloudZenChatbot.razor`

| Constant | Value | Description |
|---|---|---|
| `MaxUserMessages` | `5` | Client-side user message cap |

---

## 12. Error Handling

### Anthropic API Error Classification

The backend parses Anthropic error responses and classifies them:

```csharp
// Billing errors (insufficient credits)
if (statusCode == 400 && body.Contains("credit balance is too low"))
    → throws "billing error" → caught → 503 Service Unavailable

// Rate limit errors
if (statusCode == 429 || errorType == "rate_limit_error")
    → throws "rate limit" → caught → 429 Too Many Requests

// All other errors
    → throws generic → caught → 500 Internal Server Error
```

### Client-Side Error Handling (`ChatbotService.cs`)

- **HTTP errors** → parsed from response body or generic status message
- **Network errors** → "Unable to connect to the chat service."
- **Timeouts** → "Request timed out. Please try again."
- **Unexpected errors** → "Something went wrong. Please try again later."

### UI Error Display (`CloudZenChatbot.razor`)

Errors are shown as assistant messages in the chat:

```csharp
messages.Add(ChatMessage.Assistant(
    result.Error ?? "Something went wrong. Please email cloudzen.inc@gmail.com directly."));
```

---

## 13. Local Development

### Prerequisites

- .NET 8 SDK
- Azure Functions Core Tools v4
- Azure Storage Emulator or Azurite
- Anthropic API key with credits

### Running Locally

**Terminal 1 — Azure Functions API:**

```powershell
cd Api
func start --port 7257
```

**Terminal 2 — Blazor WASM Frontend:**

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

## 14. Deployment

### Production Architecture

```
Azure Static Web Apps
├── Frontend: Blazor WASM (static files)
└── Linked API: Azure Functions (.NET 8 isolated)
```

### Production Config

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

## 15. Testing Guide

### Client-Side: Conversation Cap

1. Open chatbot widget
2. Send 5 messages — verify footer shows decreasing "X questions remaining"
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
// Burst 11 requests (expect 11th → 429)
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
| Pricing: "How much does it cost?" | "Depends on your situation" → book consultation |
| 3+ questions in a row | Proactively suggests consultation |
| Long question about implementation | High-level answer only → redirects to consultation |
| Reply length | Every response ≤500 characters |

---

## Summary of Protection Rules

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

*Built with ❤️ by CloudZen — Technology That Works.*
