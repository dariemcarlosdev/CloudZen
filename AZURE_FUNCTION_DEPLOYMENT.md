# Azure Function Deployment Guide

This guide documents the complete process for deploying the `CloudZen.Api` Azure Function to Azure.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Azure Function App Creation](#azure-function-app-creation)
3. [GitHub Actions Configuration](#github-actions-configuration)
4. [Azure Portal Configuration](#azure-portal-configuration)
5. [CORS Configuration](#cors-configuration)
6. [Testing](#testing)
7. [Troubleshooting](#troubleshooting)

---

## Prerequisites

- Azure Subscription
- GitHub Repository with Actions enabled
- .NET 8 SDK
- Azure Functions Core Tools (for local testing)

---

## Azure Function App Creation

### Step 1: Create Function App in Azure Portal

1. Go to [Azure Portal](https://portal.azure.com)
2. Click **"+ Create a resource"** ? Search **"Function App"**
3. Configure the following settings:

#### Basics Tab

| Setting | Value |
|---------|-------|
| **Subscription** | Your Azure subscription |
| **Resource Group** | `CloudZend-RG` (or create new) |
| **Function App name** | `cloudzen-api-func` |
| **Runtime stack** | **.NET** |
| **Version** | **8 (LTS), Isolated worker model** |
| **Region** | `West US 2` (or region with available quota) |
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

4. Click **Review + create** ? **Create**

---

## GitHub Actions Configuration

### Step 1: Enable Basic Auth (Required for Publish Profile)

1. Go to **Azure Portal** ? **Function App** ? **Configuration**
2. Click **General settings** tab
3. Set both to **ON**:
   - **SCM Basic Auth Publishing Credentials**
   - **FTP Basic Auth Publishing Credentials**
4. Click **Save**

### Step 2: Get Publish Profile

1. Go to **Function App** ? **Overview**
2. Click **"Get publish profile"** (downloads `.PublishSettings` file)

### Step 3: Add GitHub Secret

1. Go to GitHub repo ? **Settings** ? **Secrets and variables** ? **Actions**
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
  AZURE_FUNCTIONAPP_NAME: 'cloudzen-api-func'
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
1. GitHub ? **Actions** tab
2. Select **"Deploy Azure Function"**
3. Click **"Run workflow"** ? **"Run workflow"**

---

## Azure Portal Configuration

### Environment Variables

Go to **Function App** ? **Settings** ? **Environment variables** ? **App settings**

Add these application settings:

| Name | Value | Description |
|------|-------|-------------|
| `BREVO_API_KEY` | `your-brevo-api-key` | Brevo email service API key |
| `EmailSettings:FromEmail` | `cloudzen.inc@gmail.com` | Sender email address |
| `EmailSettings:CcEmail` | `softevolutionsl@gmail.com` | CC email address |
| `RateLimiting:PermitLimit` | `10` | Max requests per window |
| `RateLimiting:WindowSeconds` | `60` | Rate limit window in seconds |
| `RateLimiting:QueueLimit` | `0` | Queue limit for excess requests |
| `RateLimiting:InactivityTimeoutMinutes` | `5` | Timeout for inactive limiters |
| `RateLimiting:EnableCircuitBreaker` | `false` | Enable circuit breaker pattern |
| `ProductionOrigin` | `https://your-app.azurestaticapps.net` | Your Static Web App URL |

Click **Apply** ? **Confirm**

---

## CORS Configuration

### Option A: Azure Portal (Recommended)

1. Go to **Function App** ? **API** ? **CORS**
2. Add allowed origins:

```
https://your-static-web-app.azurestaticapps.net
https://localhost:5001
https://localhost:7257
http://localhost:5000
```

3. Click **Save**

### Option B: Code-based (Already Configured)

CORS is also configured in `Api/Program.cs`:

```csharp
// Development origins
string[] allowedOrigins = new[]
{
    "https://localhost:5001",
    "https://localhost:7001",
    "http://localhost:5000",
    "https://localhost:44370",
    "https://localhost:7257"
};

// Production: Set via ProductionOrigin environment variable
var productionOrigin = builder.Configuration["ProductionOrigin"];
if (!string.IsNullOrEmpty(productionOrigin))
{
    allowedOrigins = [.. allowedOrigins, productionOrigin];
}
```

---

## Blazor App Configuration

Update `wwwroot/appsettings.json` to point to your deployed Function:

```json
{
  "EmailService": {
    "ApiBaseUrl": "https://cloudzen-api-func.azurewebsites.net/api",
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "SendEmailEndpoint": "send-email"
  }
}
```

---

## Testing

### Test Function Endpoint

```bash
# Test the function is running
curl https://cloudzen-api-func.azurewebsites.net/api/send-email

# Expected response for GET (method not allowed or similar)
# POST with proper body should send email
```

### Test from Blazor App

1. Open your Static Web App URL
2. Navigate to Contact section
3. Fill out and submit the contact form
4. Check for success message
5. Verify email was received

### Monitor Logs

1. **Azure Portal** ? **Function App** ? **Monitor** ? **Logs**
2. Or use **Application Insights** for detailed telemetry

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

**Cause:** Origin not allowed.

**Solution:**
1. Add your Static Web App URL to CORS in Azure Portal
2. Or set `ProductionOrigin` environment variable

#### 4. Function Not Found (404)

**Cause:** Function not deployed or route incorrect.

**Solution:**
1. Check deployment logs in GitHub Actions
2. Verify function exists in Azure Portal ? Functions
3. Check route: `/api/send-email`

#### 5. Email Not Sending

**Cause:** Missing or invalid `BREVO_API_KEY`.

**Solution:**
1. Verify `BREVO_API_KEY` is set in Environment Variables
2. Ensure the API key is valid in Brevo dashboard
3. Check Application Insights for detailed errors

---

## Security Notes

### ?? Important

1. **Never commit secrets to source control**
   - `local.settings.json` is in `.gitignore`
   - Use Azure Environment Variables for production

2. **Rotate API keys if exposed**
   - If your Brevo API key was committed, rotate it immediately
   - Generate new key in Brevo dashboard

3. **Use HTTPS only**
   - All Azure Function endpoints use HTTPS by default
   - Ensure Blazor app calls HTTPS endpoints

---

## File Structure

```
CloudZen/
??? Api/
?   ??? CloudZen.Api.csproj
?   ??? Program.cs
?   ??? host.json
?   ??? local.settings.json          # Git-ignored, local secrets
?   ??? Functions/
?   ?   ??? SendEmailFunction.cs
?   ??? Models/
?   ?   ??? EmailRequest.cs
?   ?   ??? EmailSettings.cs
?   ?   ??? Options/
?   ?       ??? RateLimitOptions.cs
?   ??? Security/
?   ?   ??? InputValidator.cs
?   ??? Services/
?       ??? IRateLimiterService.cs
?       ??? RateLimiterService.cs
??? .github/
?   ??? workflows/
?       ??? azure-functions.yml       # Function deployment
?       ??? azure-static-web-apps.yml # Blazor deployment
??? wwwroot/
?   ??? appsettings.json             # Contains API URL
?   ??? staticwebapp.config.json
??? .gitignore                        # Includes local.settings.json
```

---

## Quick Reference

| Resource | URL |
|----------|-----|
| Function App | `https://cloudzen-api-func.azurewebsites.net` |
| API Endpoint | `https://cloudzen-api-func.azurewebsites.net/api/send-email` |
| Azure Portal | [portal.azure.com](https://portal.azure.com) |
| GitHub Actions | [github.com/dariemcarlosdev/CloudZen/actions](https://github.com/dariemcarlosdev/CloudZen/actions) |

---

## Deployment Checklist

- [x] Create Azure Function App (West US 2)
- [x] Enable Basic Auth Publishing
- [x] Download Publish Profile
- [x] Add `AZURE_FUNCTIONAPP_PUBLISH_PROFILE` to GitHub Secrets
- [x] GitHub Actions workflow configured
- [x] Deployment successful
- [ ] Add Environment Variables in Azure Portal
- [ ] Configure CORS with Static Web App URL
- [ ] Update `appsettings.json` with Function URL
- [ ] Test contact form end-to-end
- [ ] Monitor Application Insights for errors

---

*Last updated: Successfully deployed to Azure on West US 2 region*
