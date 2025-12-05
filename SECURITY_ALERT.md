# 🚨 CRITICAL SECURITY ALERT 🚨

## 🔓 Exposed API Key in Your Repository

Your Brevo API key is **publicly exposed** in `wwwroot/appsettings.json`:

```json
"BrevoApiKey": "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"
```

### ⚠️ Why This Is Critical

Since your app is **Blazor WebAssembly**, all files in `wwwroot/` (including `appsettings.json`) are downloaded to the user's browser and are **publicly accessible**. Anyone can:

1. 👁️ View the file at `https://your-site.com/appsettings.json`
2. 🔍 Extract your API key from browser DevTools
3. 💸 Use your API key to send emails at your expense
4. 📧 Potentially exceed your Brevo quota or send spam

### 🛠️ Immediate Actions Required

#### 1. 🔄 Rotate Your Brevo API Key (DO THIS NOW!)

1. 🌐 Go to [Brevo Dashboard](https://app.brevo.com)
2. 🗂️ Navigate to **SMTP & API** > **API Keys**
3. 🗑️ Delete the exposed key: `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`
4. ✨ Generate a new API key
5. 🔐 Store it securely in Azure Key Vault (see deployment guide)

#### 2. 🧹 Remove Secret from Repository

```bash
# Remove the secret from the file
# Edit wwwroot/appsettings.json and remove the BrevoApiKey value

# If you've already committed it, you need to remove it from Git history:
git filter-branch --force --index-filter \
  "git rm --cached --ignore-unmatch wwwroot/appsettings.json" \
  --prune-empty --tag-name-filter cat -- --all

# Then force push (WARNING: This rewrites history)
git push origin --force --all
```

#### 3. 🛡️ Prevent Future Exposure

Add to `.gitignore`:
```
**/appsettings.Development.json
**/appsettings.*.json
!**/appsettings.json
```

Then create a template `appsettings.json` with no secrets:
```json
{
  "CloudZenBlobStorageConnection": {
    "blobServiceUri": ""
  },
  "EmailSettings": {
    "Provider": "Brevo",
    "FromEmail": "cloudzen.inc@gmail.com"
  }
}
```

### 🏗️ Correct Architecture for Blazor WebAssembly

**❌ Never store secrets in Blazor WebAssembly apps!** Use this architecture instead:

```
┌─────────────────────────────┐
│  🌐 Blazor WebAssembly      │ (Client-side, public)
│     (Browser)               │
│  ⚠️  NO SECRETS HERE!       │
└─────────────────────────────┘
            │ 🔒 HTTPS
            ▼
┌─────────────────────────────┐
│  ⚡ Azure Functions API      │ (Server-side, secure)
│     • 📧 SendEmail endpoint │
│     • 🔑 Reads secrets from │
│          Key Vault          │
└─────────────────────────────┘
            │ 🔐 Managed Identity
            ▼
┌─────────────────────────────┐
│  🔐 Azure Key Vault          │
│     • 🔑 BREVO_API_KEY      │
│     • 🗝️  Other secrets      │
└─────────────────────────────┘
```

### 📝 Required Changes

1. ⚡ **Create Azure Functions backend** (see `deployment_guide.md` section 5)
2. 📤 **Move email sending logic** from `BrevoEmailProvider.cs` to Azure Function
3. 🔗 **Update Blazor app** to call the Function endpoint instead
4. 🔐 **Store secrets** in Azure Key Vault, accessed only by the Function

### 💡 Example Migration

**❌ Before (INSECURE):**
```csharp
// In BrevoEmailProvider.cs (Blazor WASM)
var apikey = Environment.GetEnvironmentVariable("BREVO_API_KEY"); // Won't work!
```

**✅ After (SECURE):**
```csharp
// In EmailService.cs (Blazor WASM)
public async Task SendEmailAsync(EmailRequest request)
{
    var apiUrl = _config["ApiBaseUrl"] + "/SendEmail";
    var response = await _httpClient.PostAsJsonAsync(apiUrl, request);
    response.EnsureSuccessStatusCode();
}

// In SendEmailFunction.cs (Azure Functions)
[FunctionName("SendEmail")]
public async Task<IActionResult> Run([HttpTrigger] HttpRequest req)
{
    var apiKey = _config["BREVO-API-KEY"]; // From Key Vault via Managed Identity
    // ... send email using Brevo SDK
}
```

### 💰 Cost of Not Fixing This

- 🚫 Unauthorized use of your Brevo account
- 📊 Potential quota exhaustion
- 📧 Spam sent from your account
- ⛔ Account suspension by Brevo
- 💔 Reputational damage

### ✅ Next Steps

1. 📖 Read `deployment_guide.md` (updated with full details)
2. 🔄 Rotate your Brevo API key immediately
3. ⚡ Create Azure Functions backend
4. 📤 Move sensitive operations to Functions
5. 🧹 Remove all secrets from `appsettings.json`
6. 🧪 Test the new architecture locally
7. 🚀 Deploy to Azure

### 💬 Need Help?

Refer to:
- 📚 `deployment_guide.md` - Complete Azure setup instructions
- ⚙️ `wwwroot/staticwebapp.config.json` - Required configuration (already created)

---

**⚠️ This is a critical security issue. Please address it before deploying to production.**
