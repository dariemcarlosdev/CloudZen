# Azure Function Deployment Guide

This guide documents the complete process for deploying the `CloudZen.Api` Azure Function to Azure.

> **See also:** [BLUE_GREEN_DEPLOYMENT.md](BLUE_GREEN_DEPLOYMENT.md) for the staging/production (blue/green) multi-environment deployment setup.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Azure Function App Creation](#azure-function-app-creation)
3. [GitHub Actions Configuration](#github-actions-configuration)
4. [Azure Portal Configuration](#azure-portal-configuration)
5. [CORS Configuration](#cors-configuration)
6. [Blazor App Configuration](#blazor-app-configuration)
7. [Local Development](#local-development)
8. [Testing](#testing)
9. [Troubleshooting](#troubleshooting)
10. [Security Notes](#security-notes)
11. [File Structure](#file-structure)

---

## Prerequisites

- Azure Subscription
- GitHub Repository with Actions enabled
- .NET 8 SDK
- Azure Functions Core Tools v4 (for local testing)
- Azure CLI (for Key Vault setup)

---

## Azure Function App Creation

### Step 1: Create Function App in Azure Portal

1. Go to [Azure Portal](https://portal.azure.com)
2. Click **"+ Create a resource"** > Search **"Function App"**
3. Configure the following settings:

#### Basics Tab

| Setting | Value |
|---------|-------|
| **Subscription** | Your Azure subscription |
| **Resource Group** | `CloudZend-RG` (or create new) |
| **Function App name** | `cloudzen-api-func-e4gehdaef9ftdhbn` |
| **Runtime stack** | **.NET** |
| **Version** | **8 (LTS), Isolated worker model** |
| **Region** | `West US 2` |
| **Operating System** | Windows or Linux |
| **Hosting plan** | **Consumption (Serverless)** |

#### Storage Tab

| Setting | Value |
|---------|-------|
| **Storage account** | Create new: `cloudzenapifuncstorage` |
| **Azure Files connection** | Leave unchecked |
| **Diagnostic Settings** | Configure later |

#### Networking Tab

| Setting | Value |
|---------|-------|
| **Enable public access** | **On** |
| **Network injection** | **Off** |

#### Monitoring Tab

| Setting | Value |
|---------|-------|
| **Enable Application Insights** | **Yes** |
| **Application Insights** | Create new: `cloudzen-api-insights` |

#### Durable Functions Tab

| Setting | Value |
|---------|-------|
| **Enable Durable Functions** | **No** |
| **Backend provider** | Bring your own: Azure Storage |

4. Click **Review + create** > **Create**

---

## GitHub Actions Configuration

### Step 1: Enable Basic Auth (Required for Publish Profile)

1. Go to **Azure Portal** > **Function App** > **Configuration**
2. Click **General settings** tab
3. Set both to **ON**:
   - **SCM Basic Auth Publishing Credentials**
   - **FTP Basic Auth Publishing Credentials**
4. Click **Save**

### Step 2: Get Publish Profile

1. Go to **Function App** > **Overview**
2. Click **"Get publish profile"** (downloads `.PublishSettings` file)

### Step 3: Add GitHub Secret

1. Go to GitHub repo > **Settings** > **Secrets and variables** > **Actions**
2. Click **New repository secret**
3. **Name:** `AZURE_FUNCTIONAPP_PUBLISH_PROFILE`
4. **Value:** Paste entire contents of `.PublishSettings` file
5. Click **Add secret**

### Step 4: Workflow File

The workflow file is located at `.github/workflows/azure-functions.yml`:

```yaml
name: Deploy Azure Function

on:
  push:
    branches:
      - master
    paths:
      - 'Api/**'  # Only trigger when Api folder changes
  workflow_dispatch:  # Allow manual trigger

env:
  AZURE_FUNCTIONAPP_NAME: 'cloudzen-api-func-e4gehdaef9ftdhbn'
  AZURE_FUNCTIONAPP_PACKAGE_PATH: 'Api'
  DOTNET_VERSION: '8.0.x'

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Setup .NET ${{ env.DOTNET_VERSION }}
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore dependencies
        run: dotnet restore ${{ env.AZURE_FUNCTIONAPP_PACKAGE_PATH }}

      - name: Build
        run: dotnet build ${{ env.AZURE_FUNCTIONAPP_PACKAGE_PATH }} --configuration Release --no-restore

      - name: Publish
        run: dotnet publish ${{ env.AZURE_FUNCTIONAPP_PACKAGE_PATH }} --configuration Release --no-build --output ./output

      - name: Deploy to Azure Functions
        uses: Azure/functions-action@v1
        with:
          app-name: ${{ env.AZURE_FUNCTIONAPP_NAME }}
          package: './output'
          publish-profile: ${{ secrets.AZURE_FUNCTIONAPP_PUBLISH_PROFILE }}
```

### Step 5: Trigger Deployment

**Option A: Push changes**
```bash
git add .
git commit -m "Deploy Azure Function"
git push origin master
```

**Option B: Manual trigger**
1. GitHub > **Actions** tab
2. Select **"Deploy Azure Function"**
3. Click **"Run workflow"** > **"Run workflow"**

---

## Azure Portal Configuration

### Environment Variables

Go to **Function App** > **Settings** > **Environment variables** > **App settings**

Add these application settings:

| Name | Value | Description |
|------|-------|-------------|
| `ANTHROPIC_API_KEY` | `your-anthropic-api-key` | Anthropic Claude API key (for AI chatbot) |
| `BREVO_SMTP_KEY` | `your-brevo-smtp-key` | Brevo SMTP relay key |
| `BREVO_SMTP_LOGIN` | `your-smtp-login@smtp-brevo.com` | Brevo SMTP login |
| `KEY_VAULT_ENDPOINT` | `https://cloudzenvault.vault.azure.net/` | *(Optional)* Azure Key Vault URI for secrets management |
| `EmailSettings:FromEmail` | `cloudzen.inc@gmail.com` | Sender email address |
| `EmailSettings:CcEmail` | `softevolutionsl@gmail.com` | CC email address |
| `ProductionOrigin` | `https://www.cloud-zen.net` | Your Static Web App URL (added to CORS allowed origins) |
| `AllowedOrigins:0` | `https://www.cloud-zen.net` | *(Optional)* Explicit CORS origin list (overrides defaults) |
| `AllowedOrigins:1` | `https://lively-flower-02783cd0f.3.azurestaticapps.net` | *(Optional)* Azure-assigned SWA origin |
| `RateLimiting:PermitLimit` | `10` | Max requests per window |
| `RateLimiting:WindowSeconds` | `60` | Rate limit window in seconds |
| `RateLimiting:QueueLimit` | `0` | Queue limit for excess requests |
| `RateLimiting:InactivityTimeoutMinutes` | `5` | Timeout for inactive limiters |
| `RateLimiting:EnableCircuitBreaker` | `false` | Enable Polly circuit breaker pattern |

Click **Apply** > **Confirm**

### Azure Key Vault Integration (Optional)

If `KEY_VAULT_ENDPOINT` is configured, the Function App will load secrets from Azure Key Vault using `DefaultAzureCredential`. This supports:

- **Azure Managed Identity** (production -- enable System Assigned Identity on the Function App)
- **Azure CLI credential** (local development)
- **Environment credentials** (CI/CD pipelines)

To set up:
1. Create a Key Vault (e.g., `cloudzenvault`)
2. Grant the Function App's managed identity **Key Vault Secrets User** role
3. Store secrets like `ANTHROPIC-API-KEY` and `BREVO-SMTP-KEY` in the vault
4. Set `KEY_VAULT_ENDPOINT` to `https://cloudzenvault.vault.azure.net/`

---

## CORS Configuration

CORS is configured at two levels:

### Level 1: Application Code (`Api/Program.cs`)

CORS origins are resolved at runtime in `Program.cs` with the following priority:

1. **`AllowedOrigins` configuration section** -- If set (via Azure Environment Variables), these are used as-is.
2. **`ProductionOrigin`** -- If `AllowedOrigins` is not set, falls back to this single origin.
3. **Development defaults** -- If neither is set and `AZURE_FUNCTIONS_ENVIRONMENT=Development`, falls back to:
   - `https://localhost:7243` (Blazor WASM Kestrel HTTPS)
   - `http://localhost:5054` (Blazor WASM Kestrel HTTP)
4. **Production enforcement** -- If none of the above are configured, the app throws at startup.
5. **`ProductionOrigin` append** -- Always appended if set and not already present in the resolved list.

Origins are registered via a `CorsSettings` singleton and applied per-request by each Function using extension methods:

```csharp
// In each function handler:
req.HttpContext.Response.AddCorsHeaders(req, _corsSettings);
```

### Level 2: Azure Portal (Recommended for Production)

1. Go to **Function App** > **API** > **CORS**
2. Add allowed origins:

```
https://www.cloud-zen.net
https://lively-flower-02783cd0f.3.azurestaticapps.net
```

3. Click **Save**

> **Note:** The `host.json` CORS section has been intentionally removed. When Azure Portal CORS is configured, it overrides `host.json` CORS anyway. Having both can cause conflicts. CORS is managed exclusively via Azure Portal settings and the `Program.cs` runtime logic (which reads `AllowedOrigins` / `ProductionOrigin` from Azure Environment Variables).

---

## Blazor App Configuration

The Blazor WASM client uses environment-specific configuration files:

### `wwwroot/appsettings.json` (Base -- used in all environments)

```json
{
  "EmailService": {
    "ApiBaseUrl": "/api",
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "SendEmailEndpoint": "send-email"
  },
  "ChatbotService": {
    "ApiBaseUrl": "/api",
    "TimeoutSeconds": 60,
    "ChatEndpoint": "chat"
  }
}
```

### `wwwroot/appsettings.Development.json` (Local development)

```json
{
  "EmailService": {
    "ApiBaseUrl": "http://localhost:7257/api"
  },
  "ChatbotService": {
    "ApiBaseUrl": "http://localhost:7257/api"
  }
}
```

### `wwwroot/appsettings.Production.json` (Deployed)

```json
{
  "EmailService": {
    "ApiBaseUrl": "https://cloudzen-api-func-e4gehdaef9ftdhbn.westus2-01.azurewebsites.net/api",
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "SendEmailEndpoint": "send-email"
  },
  "ChatbotService": {
    "ApiBaseUrl": "https://cloudzen-api-func-e4gehdaef9ftdhbn.westus2-01.azurewebsites.net/api"
  }
}
```

### Static Web App Security Headers (`wwwroot/staticwebapp.config.json`)

The `Content-Security-Policy` and `connect-src` directives must include the Function App URL to allow API calls from the deployed Blazor app:

```
connect-src 'self' https://cloudzen-api-func-e4gehdaef9ftdhbn.westus2-01.azurewebsites.net https://www.cloud-zen.net;
```

---

## Local Development

### Running the API locally

```bash
cd Api
func start --port 7257
```

Or use the Visual Studio launch profiles in `Api/Properties/launchSettings.json`:

| Profile | Port | Notes |
|---------|------|-------|
| `CloudZen.Api` | `7257` | Standard HTTP |
| `CloudZen.Api (HTTPS)` | `7257` | HTTPS with local certificate |

### Running the Blazor WASM client locally

```bash
dotnet run
```

Blazor launch profiles (`Properties/launchSettings.json`):

| Profile | URL | Notes |
|---------|-----|-------|
| `http` | `http://localhost:5054` | HTTP only |
| `https` | `https://localhost:7243` / `http://localhost:5054` | HTTPS + HTTP |

> **Tip:** The API CORS defaults in development mode are pre-configured to match these Blazor ports.

---

## Testing

### Test Function Endpoints

```bash
# Test the email function is running
curl https://cloudzen-api-func-e4gehdaef9ftdhbn.westus2-01.azurewebsites.net/api/send-email

# Test the chat function
curl -X POST https://cloudzen-api-func-e4gehdaef9ftdhbn.westus2-01.azurewebsites.net/api/chat \
  -H "Content-Type: application/json" \
  -d '{"messages":[{"role":"user","content":"What does CloudZen do?"}]}'
```

### Test from Blazor App

1. Open your Static Web App URL
2. Navigate to Contact section
3. Fill out and submit the contact form
4. Check for success message
5. Verify email was received
6. **Test AI Chatbot**: Click the chat FAB button, send a message, verify response

### Monitor Logs

1. **Azure Portal** > **Function App** > **Monitor** > **Logs**
2. Or use **Application Insights** for detailed telemetry (adaptive sampling and live metrics are enabled)

---

## Troubleshooting

### Common Issues

#### 1. 401 Unauthorized on Deployment

**Cause:** Basic Auth is disabled or publish profile is invalid.

**Solution:**
1. Enable **SCM Basic Auth** in Azure Portal
2. **Reset publish profile** and re-download
3. Update GitHub secret with new profile

#### 2. Quota Error During Creation

**Cause:** No quota for Consumption plan in selected region.

**Solution:** Choose a different region (e.g., West US 2, Central US, West Europe)

#### 3. CORS Errors

**Cause:** Origin not allowed at one or more CORS levels.

**Solution:**
1. Set `ProductionOrigin` or `AllowedOrigins` environment variables in Azure Portal
2. Add origin in Azure Portal > **API** > **CORS**
3. Ensure `staticwebapp.config.json` `connect-src` includes the Function App URL

#### 4. Function Not Found (404)

**Cause:** Function not deployed or route incorrect.

**Solution:**
1. Check deployment logs in GitHub Actions
2. Verify function exists in Azure Portal > Functions
3. Check route: `/api/send-email` or `/api/chat`

#### 5. Email Not Sending

**Cause:** Missing or invalid SMTP credentials.

**Solution:**
1. Verify `BREVO_SMTP_KEY` and `BREVO_SMTP_LOGIN` are set in Environment Variables
2. Ensure the SMTP key is valid in Brevo dashboard
3. Check Application Insights for detailed errors

#### 6. Chat Function Returns 500

**Cause:** Missing or invalid `ANTHROPIC_API_KEY`, or Anthropic billing/quota issues.

**Solution:**
1. Verify `ANTHROPIC_API_KEY` is set in Environment Variables or Key Vault
2. Check Anthropic dashboard for billing status and credit balance
3. Check Application Insights for specific error messages (e.g., `credit balance is too low`)

#### 7. Startup Crash: "CORS 'AllowedOrigins' must be configured"

**Cause:** Production environment has no `AllowedOrigins` or `ProductionOrigin` configured.

**Solution:**
1. Set `AllowedOrigins:0`, `AllowedOrigins:1`, etc. in Environment Variables
2. Or set `ProductionOrigin` to the Static Web App URL

#### 8. Rate Limit (429) Errors

**Cause:** Client exceeded the configured request limit.

**Solution:**
1. Check `RateLimiting` configuration values
2. The `Retry-After` header in the response indicates wait time
3. If circuit breaker is enabled, wait for the recovery period

#### 9. "0 functions found (Custom)" After Deploy

**Cause:** `FUNCTIONS_WORKER_RUNTIME` is missing, wrong, or another startup crash.

**Solution:**
1. Verify `FUNCTIONS_WORKER_RUNTIME` = `dotnet-isolated` (not `dotnet`)
2. Verify `FUNCTIONS_EXTENSION_VERSION` = `~4`
3. Verify `WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED` = `1`
4. Check **Application Insights → Failures → Exceptions** for worker crash details
5. See [BLUE_GREEN_DEPLOYMENT.md § Troubleshooting](BLUE_GREEN_DEPLOYMENT.md#10-troubleshooting) for full diagnosis steps

---

## Security Notes

### Important

1. **Never commit secrets to source control**
   - `local.settings.json` is in `.gitignore`
   - Use Azure Environment Variables or Key Vault for production
   - The Anthropic API key and system prompt/knowledge base are server-side only

2. **Azure Key Vault integration**
   - Secrets can be managed in Key Vault when `KEY_VAULT_ENDPOINT` is set
   - Uses `DefaultAzureCredential` with limited credential types for security
   - Enable System Assigned Managed Identity on the Function App

3. **Rotate API keys if exposed**
   - If any API key was committed, rotate it immediately
   - Generate new keys in the respective dashboards (Brevo, Anthropic)

4. **Use HTTPS only**
   - All Azure Function endpoints use HTTPS by default
   - HSTS is enabled in `host.json` with 1-year max-age, subdomain inclusion, and preload
   - Ensure Blazor app calls HTTPS endpoints

5. **Security headers**
   - `X-Content-Type-Options: nosniff`
   - `X-Frame-Options: DENY`
   - `X-XSS-Protection: 1; mode=block`
   - `Referrer-Policy: strict-origin-when-cross-origin`
   - `Permissions-Policy: geolocation=(), microphone=(), camera=()`

6. **Input validation**
   - All user inputs are validated and sanitized via `InputValidator`
   - Request body size is limited (15 KB for chat)
   - Message count and content length are capped
   - XSS/injection patterns are detected and rejected

7. **Rate limiting**
   - Per-client fixed window rate limiting via Polly
   - Optional circuit breaker for cascading failure protection
   - Automatic cleanup of inactive client limiters

---

## File Structure

```
CloudZen/
├── Api/
│   ├── CloudZen.Api.csproj
│   ├── Program.cs                       # Host builder, DI, CORS, Key Vault
│   ├── host.json                        # HTTP config, CORS origins, HSTS, security headers
│   ├── local.settings.json              # Git-ignored, local secrets
│   ├── Properties/
│   │   └── launchSettings.json          # Local dev profiles (port 7257)
│   ├── Functions/
│   │   ├── SendEmailFunction.cs         # Email via Brevo SMTP (MailKit)
│   │   └── ChatFunction.cs             # AI chatbot proxy to Anthropic Claude
│   ├── Models/
│   │   ├── EmailRequest.cs
│   │   ├── EmailSettings.cs
│   │   ├── ChatRequest.cs               # Chat API request model
│   │   ├── ChatResponse.cs              # Chat API response model
│   │   ├── RateLimitResult.cs           # Rate limit check result
│   │   ├── RateLimitRejectionReason.cs  # Rejection reason enum
│   │   └── Options/
│   │       └── RateLimitOptions.cs      # Rate limiting configuration
│   ├── Security/
│   │   └── InputValidator.cs            # Input validation, sanitization, CORS helpers, CorsSettings
│   └── Services/
│       ├── IRateLimiterService.cs
│       └── RateLimiterService.cs        # Polly-based per-client rate limiter
├── Models/
│   ├── ChatMessage.cs                   # Shared chat message model
│   ├── EmailApiRequest.cs
│   ├── EmailApiResponse.cs
│   ├── EmailApiErrorResponse.cs
│   └── Options/
│       ├── ChatbotOptions.cs            # Chatbot client configuration
│       ├── EmailServiceOptions.cs
│       └── BlobStorageOptions.cs
├── Services/
│   ├── Abstractions/
│   │   ├── IChatbotService.cs
│   │   └── IEmailService.cs
│   ├── ChatbotService.cs               # Blazor-side chatbot client
│   └── ApiEmailService.cs
├── Shared/
│   └── Chatbot/
│       ├── CloudZenChatbot.razor        # AI chatbot widget UI
│       ├── CloudZenChatbot.razor.cs     # Code-behind (contact info highlighting)
│       └── CloudZenChatbot.razor.css    # Scoped chatbot styles
├── Properties/
│   └── launchSettings.json              # Blazor WASM dev profiles (ports 7243/5054)
├── .github/
│   └── workflows/
│       ├── azure-functions.yml          # Function deployment
│       └── azure-static-web-apps.yml    # Blazor deployment
├── wwwroot/
│   ├── appsettings.json                 # Base config (relative /api paths)
│   ├── appsettings.Development.json     # Dev overrides (localhost:7257)
│   ├── appsettings.Production.json      # Production overrides (full Azure URLs)
│   ├── appsettings.Staging.json         # Staging overrides (staging Function App URL)
│   ├── staticwebapp.config.json         # SWA routing, CSP, security headers
│   └── images/
│       └── cloudzen-logo.png            # Brand logo (chatbot avatar)
├── .gitignore                           # Includes local.settings.json
├── AZURE_FUNCTION_DEPLOYMENT.md         # This file
└── BLUE_GREEN_DEPLOYMENT.md             # Blue/green staging/production guide
```

---

## Quick Reference

| Resource | URL |
|----------|-----|
| Production Function App | `https://cloudzen-api-func-e4gehdaef9ftdhbn.westus2-01.azurewebsites.net` |
| Staging Function App | `https://cloudzen-api-func-staging-hch0amaed0gke2dv.westus2-01.azurewebsites.net` |
| Email Endpoint | `.../api/send-email` |
| Chat Endpoint | `.../api/chat` |
| Azure Portal | [portal.azure.com](https://portal.azure.com) |
| GitHub Actions | [github.com/dariemcarlosdev/CloudZen/actions](https://github.com/dariemcarlosdev/CloudZen/actions) |
| Key Vault | `https://cloudzenvault.vault.azure.net/` |

---

## Deployment Checklist

- [x] Create Azure Function App (`cloudzen-api-func-e4gehdaef9ftdhbn`, West US 2)
- [x] Enable Basic Auth Publishing
- [x] Download Publish Profile
- [x] Add `AZURE_FUNCTIONAPP_PUBLISH_PROFILE` to GitHub Secrets
- [x] GitHub Actions workflow configured
- [x] Deployment successful
- [ ] Add Environment Variables in Azure Portal
- [ ] Configure `KEY_VAULT_ENDPOINT` and Managed Identity (optional)
- [ ] Configure CORS with Static Web App URL (Portal + `ProductionOrigin` = `https://www.cloud-zen.net`)
- [ ] Update `appsettings.Production.json` with Function URL
- [ ] Update `staticwebapp.config.json` CSP `connect-src` with Function URL
- [ ] Test contact form end-to-end
- [ ] Test AI chatbot end-to-end
- [ ] Monitor Application Insights for errors

---

*Last updated: June 2025 -- Updated Function App name, URLs, CORS configuration (multi-level), Key Vault integration, Polly rate limiting, environment-specific Blazor config, security headers, file structure, and local development setup.*
