# Azure Deployment Quick Reference

## ⚡ Quick Start Checklist

### 🚨 Before You Begin - CRITICAL
- [ ] **Rotate your exposed Brevo API key** (see `SECURITY_ALERT.md`)
- [ ] Remove API keys from `wwwroot/appsettings.json`
- [ ] Read the Blazor WebAssembly limitations (can't use Key Vault directly)

### 1️⃣ Azure Static Web Apps Setup
```bash
# Your workflow is ready, just need to:
```
- [ ] Create Static Web App in Azure Portal
- [ ] Select GitHub deployment source
- [ ] Choose Blazor preset
- [ ] Copy the auto-generated `AZURE_STATIC_WEB_APPS_API_TOKEN` (added to GitHub Secrets automatically)
- [ ] `staticwebapp.config.json` is already created in `wwwroot/`

### 2️⃣ Azure Blob Storage Setup
```bash
# For your resume and file uploads
```
- [ ] Create Storage Account (e.g., `cloudzenstorage`)
- [ ] Create container `cloudzencontainer` (Public access: Blob)
- [ ] Upload your resume PDF
- [ ] Generate SAS token with Read permission (1-2 year expiry)
- [ ] Update `appsettings.json` with the SAS URL
- [ ] Configure CORS (allow your Static Web App domain)

### 3️⃣ Azure Functions Backend (REQUIRED for secure operations)
```bash
# Create the backend project:
dotnet new func -n CloudZen.Api
cd CloudZen.Api
dotnet add package Azure.Identity
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
dotnet add package sib_api_v3_sdk
```
- [ ] Create `SendEmailFunction.cs` (see `deployment_guide.md` section 5)
- [ ] Deploy to Azure Function App (Consumption plan)
- [ ] Enable Managed Identity
- [ ] Link to Static Web App (in Azure Portal: Static Web App > APIs)

### 4️⃣ Azure Key Vault Setup
```bash
# For storing secrets securely
```
- [ ] Create Key Vault (e.g., `cloudzen-keyvault`)
- [ ] Add secret: `BREVO-API-KEY` (new rotated key)
- [ ] Add secret: `BLOB-STORAGE-CONNECTION-STRING`
- [ ] Grant Function App's Managed Identity access (role: Key Vault Secrets User)

### 5️⃣ Update Your Blazor App
- [ ] Remove `BrevoEmailProvider.cs` direct API calls
- [ ] Create `EmailService.cs` that calls Azure Function endpoint
- [ ] Update `Program.cs` to register the new service
- [ ] Remove secrets from `wwwroot/appsettings.json`
- [ ] Add API endpoint URL to config

### 6️⃣ GitHub Configuration
- [ ] Verify `AZURE_STATIC_WEB_APPS_API_TOKEN` in GitHub Secrets
- [ ] Remove `BREVO_API_KEY` from GitHub Secrets (not needed anymore)
- [ ] Update `.github/workflows/azure-static-web-apps.yml` (remove environment_variables line)

### 7️⃣ Security Hardening
- [ ] Rotate exposed Brevo API key
- [ ] Add sensitive config files to `.gitignore`
- [ ] Review `staticwebapp.config.json` security headers
- [ ] Enable Application Insights for monitoring
- [ ] Test CORS configuration

### 8️⃣ Testing & Deployment
```bash
# Local testing:
dotnet run                    # Run Blazor WASM
cd Api && func start          # Run Functions locally

# Production deployment:
git add .
git commit -m "Add Azure configuration"
git push origin master        # Triggers GitHub Actions
```
- [ ] Test locally with Azure Storage Emulator or live storage
- [ ] Test Azure Functions locally
- [ ] Deploy to Azure (automatic via GitHub Actions)
- [ ] Verify Static Web App URL
- [ ] Test all features (email, resume download)
- [ ] Monitor Application Insights for errors

## 📁 Files Created/Modified

### ✅ Created
- `deployment_guide.md` - Complete deployment instructions
- `SECURITY_ALERT.md` - Critical security warning
- `wwwroot/staticwebapp.config.json` - Azure Static Web Apps configuration
- `DEPLOYMENT_CHECKLIST.md` - This file

### ⚠️ Needs Modification
- `wwwroot/appsettings.json` - Remove API keys!
- `.gitignore` - Add sensitive config patterns
- `Services/BrevoEmailProvider.cs` - Move to Azure Function
- `Program.cs` - Update service registration
- `.github/workflows/azure-static-web-apps.yml` - Remove environment_variables

### 🔗 Reference URLs

| Resource | URL |
|----------|-----|
| Azure Portal | https://portal.azure.com |
| Brevo Dashboard | https://app.brevo.com |
| Your GitHub Repo | https://github.com/dariemcarlosdev/CloudZen |
| Azure Static Web Apps Docs | https://learn.microsoft.com/en-us/azure/static-web-apps/ |
| Azure Functions Docs | https://learn.microsoft.com/en-us/azure/azure-functions/ |

## 🛠️ Common Issues & Solutions

### Issue: "Environment variable not found"
**Solution:** Blazor WASM can't read Azure environment variables. Use Azure Functions backend instead.

### Issue: "CORS error when accessing blob storage"
**Solution:** Configure CORS in your Storage Account settings (Blob service).

### Issue: "404 on page refresh"
**Solution:** `staticwebapp.config.json` handles this with navigationFallback (already configured).

### Issue: "Cannot access Key Vault from Blazor"
**Solution:** This is by design. Use Azure Functions with Managed Identity instead.

## 🏗️ Architecture Summary

```
┌────────────────────────────────────────────────────────┐
│  GitHub Repository                                     │
│  ├── CloudZen/ (Blazor WASM - Client-side)           │
│  └── CloudZen.Api/ (Azure Functions - Server-side)   │
└──────────────────┬─────────────────────────────────────┘
                   │ GitHub Actions
                   ▼
┌────────────────────────────────────────────────────────┐
│  Azure Static Web Apps                                 │
│  ├── Hosts Blazor WASM                                │
│  └── Linked API (Azure Functions)                     │
└──────────────────┬─────────────────────────────────────┘
                   │
        ┌──────────┴──────────┐
        ▼                     ▼
┌───────────────┐     ┌──────────────────┐
│  Azure Key    │     │  Azure Blob      │
│  Vault        │     │  Storage         │
│  (Secrets)    │     │  (Resume, Files) │
└───────────────┘     └──────────────────┘
```

## 🎯 Success Criteria

Your deployment is successful when:
- ✔️ Blazor app loads at your Static Web App URL
- ✔️ Contact form sends emails via Azure Function (not client-side)
- ✔️ Resume downloads from Blob Storage with SAS token
- ✔️ No secrets in `wwwroot/appsettings.json`
- ✔️ All API keys rotated and stored in Key Vault
- ✔️ HTTPS enabled with auto-provisioned certificate
- ✔️ GitHub Actions workflow completes successfully
- ✔️ No console errors in browser DevTools

---

**Need detailed instructions?** See `deployment_guide.md`

**Security concerns?** See `SECURITY_ALERT.md`
