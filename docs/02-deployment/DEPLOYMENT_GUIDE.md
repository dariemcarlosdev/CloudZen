# Azure Deployment and Configuration Guide

This guide will walk you through configuring your Azure services (Static Web App, Key Vault, Blob Storage) and your application to work with them.

> **See also:** [AZURE_FUNCTION_DEPLOYMENT.md](AZURE_FUNCTION_DEPLOYMENT.md) · [DEPLOYMENT_CHECKLIST.md](DEPLOYMENT_CHECKLIST.md)
>
> **Last synced: 2026-07-25**

## Important: Blazor WebAssembly Limitations

**Your application is Blazor WebAssembly**, which runs entirely in the browser. This means:

- ❌ **Cannot directly access Azure Key Vault** - WebAssembly apps run client-side and cannot use `DefaultAzureCredential` or Managed Identity directly
- ❌ **Cannot use connection strings** - All configuration in `appsettings.json` is publicly accessible in the browser
- ❌ **Cannot send emails directly via SMTP** - SMTP requires server-side execution
- ✔️ **Can call Azure Functions or APIs** - Use serverless backends for secure operations
- ✔️ **Can use SAS tokens** - For Blob Storage access (like your resume download)

## 1. Azure Static Web App

Your `azure-static-web-apps.yml` workflow is set up to deploy to an Azure Static Web App.

### Creating the Static Web App in Azure:

1.  Go to the [Azure Portal](https://portal.azure.com).
2.  Click **Create a resource** and search for **Static Web App**.
3.  Select your subscription and resource group (or create a new one).
4.  Enter a name for your app (e.g., `cloudzen-portfolio`).
5.  Choose the **Free** plan for hosting personal projects or **Standard** plan for production apps with custom domains and more features.
6.  Select a region close to your users.
7.  For **Deployment details**, select **GitHub**.
8.  Sign in to GitHub and select your repository (`CloudZen`) and branch (`master`).
9.  For **Build Presets**, select **Blazor**.
10. The workflow file handles the build. Azure will auto-generate a workflow, but you're already using your custom one.
11. Click **Review + create**, then **Create**.

Azure will create the Static Web App and add a secret named `AZURE_STATIC_WEB_APPS_API_TOKEN...` to your GitHub repository. Your workflow uses this to deploy.

### Create `staticwebapp.config.json` (REQUIRED)

Azure Static Web Apps requires a configuration file for routing, security headers, and environment variables. Create this file in your `wwwroot` folder:

**File: `wwwroot/staticwebapp.config.json`**

```json
{
  "navigationFallback": {
    "rewrite": "/index.html",
    "exclude": ["/images/*.{png,jpg,gif,svg}", "/css/*", "/*.js"]
  },
  "globalHeaders": {
    "X-Content-Type-Options": "nosniff",
    "X-Frame-Options": "DENY",
    "Content-Security-Policy": "default-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.tailwindcss.com https://cdn.jsdelivr.net https://cloudzenstorage.blob.core.windows.net data:; img-src 'self' data: https:; font-src 'self' data: https://cdn.jsdelivr.net; connect-src 'self' https://cloudzen-api-func-e4gehdaef9ftdhbn.westus2-01.azurewebsites.net;"
  },
  "routes": [
    {
      "route": "/api/*",
      "allowedRoles": ["anonymous"]
    }
  ],
  "responseOverrides": {
    "404": {
      "rewrite": "/index.html",
      "statusCode": 200
    }
  }
}
```

### Function App Environment Variables

Runtime config/secrets (`BREVO_SMTP_KEY`, `N8N_WEBHOOK_URL`, `ANTHROPIC_API_KEY`, rate limiting, CORS origins, etc.) are set on the **Azure Function App** — not the Static Web App. These are two separate Azure resources with separate Configuration blades.

**Authoritative table:** see [AZURE_FUNCTION_DEPLOYMENT.md](AZURE_FUNCTION_DEPLOYMENT.md#azure-portal-configuration) for the current, complete list (kept in one place to avoid drift).

**Note:** These environment variables are **only accessible to Azure Functions**, not to your Blazor WebAssembly app directly.

## 2. Azure Key Vault

❗ **Important for Blazor WebAssembly:** Your client-side app cannot directly access Key Vault. Key Vault is only useful if you create an **Azure Functions API backend** to handle sensitive operations.

### Creating the Key Vault:

1.  In the Azure Portal, create a new **Key Vault** resource.
2.  Select a resource group and region.
3.  Give it a unique name (e.g., `cloudzen-keyvault`).
4.  On the **Access configuration** tab, choose **Azure role-based access control (RBAC)** (recommended) or **Vault access policy**.
5.  Keep the other defaults and create the vault.

### Adding Secrets:

1.  Go to your new Key Vault and select **Secrets**.
2.  Click **Generate/Import**.
3.  Add these secrets:

| Secret Name | Value | Purpose |
|------------|-------|---------|
| `BREVO-SMTP-KEY` | Your Brevo SMTP relay key | Email sending |
| `BREVO-SMTP-LOGIN` | Your Brevo SMTP login | Email sending |
| `BLOB-STORAGE-CONNECTION-STRING` | Your storage connection string | Blob operations |
| `ANTHROPIC-API-KEY` | Your Anthropic Claude API key | AI chatbot (ChatFunction) |

### Granting Access (Only needed if using Azure Functions):

If you create Azure Functions to handle backend operations:

1.  Go to your **Azure Function App** in the portal.
2.  Select **Identity** under **Settings**.
3.  Enable **System assigned** managed identity.
4.  Copy the **Object (principal) ID**.
5.  Go back to your Key Vault.
6.  Select **Access control (IAM)**.
7.  Click **Add role assignment**.
8.  Select **Key Vault Secrets User** role.
9.  Assign access to the managed identity of your Function App.

## 3. Azure Blob Storage

For storing files like your resume PDF and potentially user uploads.

### Creating the Storage Account:

1.  In the Azure Portal, create a new **Storage Account**.
2.  Select a resource group and region.
3.  Give it a unique name (e.g., `cloudzenstorage` - must be lowercase, no spaces).
4.  Choose **Standard** performance, **LRS** (Locally Redundant Storage) redundancy for cost savings.
5.  On the **Advanced** tab:
    - Enable **Allow Blob public access** (for public files like your resume)
    - Keep **Minimum TLS version** as 1.2
6.  Create the storage account.

### Creating Blob Containers:

1.  Go to your storage account and select **Containers**.
2.  Create these containers:

| Container Name | Public Access Level | Purpose |
|---------------|-------------------|---------|
| `cloudzencontainer` | Blob (anonymous read access for blobs only) | Public files (resume PDF) |
| `uploads` | Private (no anonymous access) | User uploads (requires backend) |

### Generating SAS Tokens for Public Files:

For files like your resume that should be publicly downloadable:

1.  Navigate to the specific blob (e.g., your resume PDF).
2.  Click **Generate SAS**.
3.  Set permissions to **Read**.
4.  Set a long expiry date (e.g., 1-2 years).
5.  Click **Generate SAS token and URL**.
6.  Copy the **Blob SAS URL** and use it in your `appsettings.json`.

**Example (as seen in your current config):**
```json
{
  "CloudZenBlobStorageConnection": {
    "blobServiceUri": "https://cloudzenstorage.blob.core.windows.net/cloudzencontainer/YourResume.pdf?sp=r&st=..."
  }
}
```

### CORS Configuration (Important for Blazor WebAssembly):

Your Blazor app runs in the browser and needs CORS enabled to access blobs:

1.  In your storage account, go to **Resource sharing (CORS)** under **Settings**.
2.  Under **Blob service**, add a CORS rule:
    - **Allowed origins**: `https://your-static-web-app-url.azurestaticapps.net` (or `*` for development)
    - **Allowed methods**: `GET, HEAD`
    - **Allowed headers**: `*`
    - **Exposed headers**: `*`
    - **Max age**: `3600`
3.  Click **Save**.

## 4. Application Configuration

### Resolved: Secrets No Longer Client-Side

Earlier versions of `wwwroot/appsettings.json` exposed the Brevo API key directly in the Blazor WebAssembly bundle (publicly visible in browser dev tools). This is fixed — secrets now live in Azure Function App settings / Key Vault, never in client-side config.

### Current Architecture:

**An Azure Functions backend handles all sensitive operations** (already deployed — see [AZURE_FUNCTION_DEPLOYMENT.md](AZURE_FUNCTION_DEPLOYMENT.md) for full setup):

```
Blazor WASM (Client) → Azure Functions API (Backend) → External Services (Email, Storage, Anthropic Claude)
                                   ↓
                          Azure Key Vault (Secrets, optional)
```

### Azure Functions for Secure Operations:

The Functions backend (`CloudZen.Api`) handles:

1. **Email sending** (`SendEmailFunction`) - Reads `BREVO_SMTP_KEY` / `BREVO_SMTP_LOGIN` from environment/Key Vault
2. **AI chatbot** (`ChatFunction`) - Proxies to Anthropic Claude using `ANTHROPIC_API_KEY`
3. **File uploads** - Handles blob storage operations securely

### Add NuGet Packages (for your Blazor project):

```bash
# Already installed, but verify versions:
dotnet list package
```

Your project already has these packages (good!):
- ✔️ `Azure.Storage.Blobs` - For blob operations
- ✔️ `Azure.Identity` - For managed identity (when you add Functions)
- ✔️ `Azure.Data.Tables` - If you need table storage

### Update `appsettings.json` (Remove Secrets!):

**File: `wwwroot/appsettings.json`**

```json
{
  "CloudZenBlobStorageConnection": {
    "blobServiceUri": "https://cloudzenstorage.blob.core.windows.net/cloudzencontainer/YourResume.pdf?sp=r&st=..."
  },
  "ApiBaseUrl": "https://your-function-app.azurewebsites.net/api",
  "EmailSettings": {
    "Provider": "Brevo",
    "FromEmail": "cloudzen.inc@gmail.com",
    "CcEmail": "softevolutionsl@gmail.com"
  }
}
```

### Update `Program.cs`:

Blazor WebAssembly **cannot access Azure environment variables directly**, so email sending is not done client-side.

Client code calls the Azure Function endpoint instead:

```csharp
// Instead of reading BREVO_SMTP_KEY client-side (❌ won't work in WASM):
// POST /api/send-email
// The Function reads BREVO_SMTP_KEY / BREVO_SMTP_LOGIN from its environment/Key Vault
```

### Recommended File Structure:

```
CloudZen/
├── CloudZen.csproj (Blazor WASM)
├── wwwroot/
│   ├── appsettings.json (no secrets!)
│   └── staticwebapp.config.json (NEW - routing config)
├── Services/
│   └── EmailService.cs (calls Azure Function API)
└── Api/ (NEW - Azure Functions project)
    ├── SendEmailFunction.cs
    └── UploadFileFunction.cs
```

## 5. Azure Functions Backend (Deployed)

The Functions backend is already created and deployed. This section documents the setup for reference — see [AZURE_FUNCTION_DEPLOYMENT.md](AZURE_FUNCTION_DEPLOYMENT.md) for the full, current configuration (including `ChatFunction` and rate limiting).

### Step 1: Create Functions Project

```bash
# In your solution directory:
dotnet new func -n CloudZen.Api
cd CloudZen.Api
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
dotnet add package Azure.Identity
dotnet add package sib_api_v3_sdk
```

### Step 2: Create SendEmail Function

**File: `Api/SendEmailFunction.cs`**

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;

public class SendEmailFunction
{
    private readonly IConfiguration _config;

    public SendEmailFunction(IConfiguration config)
    {
        _config = config;
    }

    [FunctionName("SendEmail")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
    {
        // Read secret from Key Vault via Managed Identity
        var apiKey = _config["BREVO_SMTP_KEY"];
        
        // Parse request body
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        var emailRequest = JsonSerializer.Deserialize<EmailRequest>(body);

        // Send email using Brevo
        Configuration.Default.ApiKey["api-key"] = apiKey;
        var apiInstance = new TransactionalEmailsApi();
        
        var email = new SendSmtpEmail(
            sender: new SendSmtpEmailSender { Email = "cloudzen.inc@gmail.com" },
            to: new List<SendSmtpEmailTo> { new("cloudzen.inc@gmail.com", emailRequest.FromName) },
            subject: emailRequest.Subject,
            htmlContent: $"<strong>From:</strong> {emailRequest.FromName} ({emailRequest.FromEmail})<br/><br/>{emailRequest.Message}"
        );

        await apiInstance.SendTransacEmailAsync(email);
        return new OkResult();
    }
}

public class EmailRequest
{
    public string Subject { get; set; }
    public string Message { get; set; }
    public string FromName { get; set; }
    public string FromEmail { get; set; }
}
```

### Step 3: Configure Function App

In Azure Portal:

1. Create a new **Function App**.
2. Select **Consumption** plan for pay-per-use.
3. Enable **Managed Identity**.
4. Grant Key Vault access (as described in section 2).
5. In **Configuration**, add:
   - `KEY_VAULT_ENDPOINT`: Your Key Vault URI

### Step 4: Link Functions to Static Web App

Azure Static Web Apps can be linked with Azure Functions:

1. In your Static Web App, go to **APIs**.
2. Click **Link** and select your Function App.
3. Or specify API location in your GitHub workflow.

## 6. GitHub Secrets

Your workflow already references these secrets. Ensure they're configured:

1.  In your GitHub repository, go to **Settings** > **Secrets and variables** > **Actions**.
2.  Verify/add these secrets:

| Secret Name | Value | Purpose |
|------------|-------|---------|
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Auto-generated by Azure | Deployment authentication |
| `AZURE_FUNCTIONAPP_PUBLISH_PROFILE` | From Function App publish profile | Function App deployment authentication |

**Note:** Runtime secrets (`BREVO_SMTP_KEY`, `BREVO_SMTP_LOGIN`, `ANTHROPIC_API_KEY`, etc.) live in Azure Function App settings / Key Vault, not GitHub Secrets — they're not needed at build time.

## 7. Security Best Practices

### Remove Exposed Secrets:

1. **Immediately rotate your Brevo API key** since it's exposed in your repository.
2. Remove the key from `appsettings.json`.
3. Add `wwwroot/appsettings.json` patterns with sensitive data to `.gitignore`.
4. Use `appsettings.Development.json` (git-ignored) for local secrets.

### Use Environment-Specific Configuration:

**File: `.gitignore`** (add these lines)
```
**/appsettings.Development.json
**/appsettings.*.json
!**/appsettings.json
```

**File: `wwwroot/appsettings.Development.json`** (create, git-ignored)
```json
{
  "ApiBaseUrl": "http://localhost:7071/api",
  "CloudZenBlobStorageConnection": {
    "blobServiceUri": "your-local-test-url"
  }
}
```

### Content Security Policy:

Already configured in `staticwebapp.config.json`. Adjust as needed for your domains.

## 8. Deployment Checklist

- [x] Azure Static Web App created and linked to GitHub
- [x] `staticwebapp.config.json` added to `wwwroot/`
- [x] Azure Functions backend created and deployed for sensitive operations
- [x] Exposed API keys rotated and removed from `appsettings.json`
- [x] GitHub workflow updated (removed `environment_variables`)
- [x] Build and deploy pipeline tested
- [ ] Environment Variables set in Azure Function App (see §1 table)
- [ ] Azure Key Vault configured with secrets (optional — `KEY_VAULT_ENDPOINT`)
- [ ] Managed Identity enabled for Functions and granted Key Vault access (optional)
- [ ] Azure Blob Storage CORS configured
- [ ] `staticwebapp.config.json` CSP `connect-src` updated with Function App URL
- [ ] Contact form and AI chatbot tested end-to-end
- [ ] Custom domain configured (optional, in Static Web App settings)
- [ ] SSL/TLS certificates auto-provisioned by Azure

## 9. Testing Your Deployment

### Local Testing:

```bash
# Run Blazor WASM
dotnet run

# Run Azure Functions locally (in separate terminal)
cd Api
func start
```

### Production Testing:

1. Push to `master` branch - triggers GitHub Actions workflow
2. Monitor workflow execution in GitHub Actions tab
3. Visit your Static Web App URL
4. Test email sending (should call Azure Function)
5. Test resume download (should use SAS token)
6. Check Application Insights for logs and errors

## 10. Monitoring & Diagnostics

### Enable Application Insights:

1. In Azure Portal, create **Application Insights** resource.
2. Link it to your Static Web App and Function App.
3. Monitor:
   - Request rates and response times
   - Failed requests and exceptions
   - Custom events from your app

### View Logs:

- **Static Web App**: Deployment logs in Azure Portal
- **Function App**: Live logs in Azure Portal or via CLI
- **GitHub Actions**: Workflow logs in repository

By following these steps, you will have a **secure, scalable, and production-ready** deployment on Azure that properly handles the constraints of Blazor WebAssembly applications.
