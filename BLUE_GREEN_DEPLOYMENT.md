# Blue/Green Deployment — Staging & Production

Complete guide for the CloudZen blue/green deployment pipeline using Azure Static Web Apps preview environments and separate Azure Function Apps.

> **Related docs:** [AZURE_FUNCTION_DEPLOYMENT.md](AZURE_FUNCTION_DEPLOYMENT.md) · [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md) · [DEPLOYMENT_CHECKLIST.md](DEPLOYMENT_CHECKLIST.md) · [AZURE_FUNCTIONS_HOSTING_MODELS.md](AZURE_FUNCTIONS_HOSTING_MODELS.md)

---

## Table of Contents

1. [Architecture](#1-architecture)
2. [Why Not Deployment Slots](#2-why-not-deployment-slots)
3. [Azure Portal Setup](#3-azure-portal-setup)
4. [GitHub Setup](#4-github-setup)
5. [Workflow Configuration](#5-workflow-configuration)
6. [How SWA Preview Environments Work](#6-how-swa-preview-environments-work)
7. [CORS & API Routing](#7-cors--api-routing)
8. [Day-to-Day Workflow](#8-day-to-day-workflow)
9. [Testing Procedure](#9-testing-procedure)
10. [Troubleshooting](#10-troubleshooting)

---

## 1. Architecture

Two completely independent Function Apps, one SWA with built-in preview environments:

```
GitHub Repository (master)
     │
     ├─ Push to master ────► SWA Production ──► Production Function App
     │                        www.cloud-zen.net   cloudzen-api-func-e4ge...
     │
     └─ PR to master ──────► SWA Preview Env ──► Staging Function App
                              <swa>-<PR#>.azurestaticapps.net
                                                  cloudzen-api-func-staging-hch0...
```

| Component | Production | Staging |
|-----------|-----------|---------|
| **SWA** | `www.cloud-zen.net` | `lively-flower-02783cd0f-<PR#>.azurestaticapps.net` |
| **Function App** | `cloudzen-api-func-e4gehdaef9ftdhbn` | `cloudzen-api-func-staging-hch0amaed0gke2dv` |
| **Blazor config** | `appsettings.Production.json` | `appsettings.Staging.json` (swapped at build time) |
| **CORS** | `AllowedOrigins__0 = https://www.cloud-zen.net` | `AllowedOrigins__0 = *` |
| **Trigger** | Push to `master` | PR to `master` |
| **Lifecycle** | Permanent | Auto-created on PR open, destroyed on PR close/merge |

The Blazor WASM app calls Function Apps **directly by full URL** (not through SWA's linked API feature). This is what enables each environment to point to its own backend.

---

## 2. Why Not Deployment Slots

Azure Deployment Slots require **Standard plan or higher** (~$70+/month). Our setup uses two **Consumption plan** Function Apps (pay-per-execution, near-zero cost for low traffic).

| Factor | Deployment Slots | Separate Function Apps (our setup) |
|--------|-----------------|-----------------------------------|
| **Cost** | ~$70+/month minimum | ~$0 (Consumption plan) |
| **Plan required** | Standard+ | Consumption (free tier) |
| **Independent config** | Slot-sticky settings | Fully independent App Settings |
| **Independent scaling** | Shared plan resources | Independent scaling |
| **Blue/green frontend** | Not applicable to SWA | SWA preview envs handle this |

**When slots make sense:** High-traffic production apps needing zero-downtime swaps with warm-up, running on Standard/Premium plans already.

---

## 3. Azure Portal Setup

### 3.1 Create Staging Function App

1. **Azure Portal → Create a resource → Function App**
2. Configure:

| Setting | Value |
|---------|-------|
| Name | `cloudzen-api-func-staging-<unique>` |
| Runtime | .NET 8, Isolated |
| Plan | Consumption (Serverless) |
| Region | Same as production (West US 2) |
| Resource Group | `CloudZend-RG` |

### 3.2 Production Function App — Environment Variables

| Setting | Value |
|---------|-------|
| `FUNCTIONS_WORKER_RUNTIME` | `dotnet-isolated` |
| `FUNCTIONS_EXTENSION_VERSION` | `~4` |
| `WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED` | `1` |
| `AllowedOrigins__0` | `https://www.cloud-zen.net` |
| `ProductionOrigin` | `https://www.cloud-zen.net` |
| `ANTHROPIC_API_KEY` | *(your key)* |
| `BREVO_SMTP_KEY` | *(your key)* |
| `BREVO_SMTP_LOGIN` | *(your login)* |
| `EmailSettings:FromEmail` | `cloudzen.inc@gmail.com` |
| `EmailSettings:CcEmail` | `softevolutionsl@gmail.com` |
| `RateLimiting:PermitLimit` | `10` |
| `RateLimiting:WindowSeconds` | `60` |
| `RateLimiting:QueueLimit` | `0` |
| `RateLimiting:InactivityTimeoutMinutes` | `5` |
| `RateLimiting:EnableCircuitBreaker` | `false` |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | *(auto-generated)* |
| `AzureWebJobsStorage` | *(auto-generated)* |

> ⚠️ **Critical:** `FUNCTIONS_WORKER_RUNTIME` must be `dotnet-isolated`. If missing, the host reports `0 functions found` and all endpoints return 404.

> ⚠️ **Do NOT check "Deployment slot setting"** on any setting. On Consumption plan with no slots, it can cause unexpected behavior.

### 3.3 Staging Function App — Environment Variables

Copy all production settings, then override:

| Setting | Value | Reason |
|---------|-------|--------|
| `AllowedOrigins__0` | `*` | Accept any SWA preview URL |
| `ProductionOrigin` | *(remove or leave empty)* | Not needed for staging |

All other settings (API keys, email config, rate limiting) should match production for realistic testing.

### 3.4 SWA — No API Linking Required

**Do NOT** link a Function App under **Azure Portal → SWA → APIs**. The Blazor app calls Function Apps directly via full URL. Linking would route all preview environments to the same backend, defeating blue/green.

---

## 4. GitHub Setup

### 4.1 Repository Secrets

**Settings → Secrets and variables → Actions** (repository level):

| Secret | Value |
|--------|-------|
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | SWA deployment token |
| `AZURE_FUNCTIONAPP_PUBLISH_PROFILE` | Production Function App publish profile XML |
| `AZURE_FUNCTIONAPP_PUBLISH_PROFILE_STAGING` | Staging Function App publish profile XML |

> Keep all secrets at **repository level** (not environment level). Both jobs can read repo-level secrets regardless of their `environment:` setting.

### 4.2 GitHub Environments (Optional)

**Settings → Environments:**

| Environment | Protection Rules | Branch Policy |
|-------------|-----------------|---------------|
| `production` | Required reviewers (optional) | `master` only |
| `staging` | None | Any branch |

If `production` has required reviewers, `deploy-production` will pause and wait for approval in GitHub Actions.

---

## 5. Workflow Configuration

### 5.1 Function App Workflow (`.github/workflows/azure-functions.yml`)

```yaml
name: Deploy Azure Function

on:
  push:
    branches: [master]
    paths:
      - 'Api/**'
      - '.github/workflows/azure-functions.yml'
  pull_request:
    types: [opened, synchronize, reopened]
    branches: [master]
    paths:
      - 'Api/**'
      - '.github/workflows/azure-functions.yml'
  workflow_dispatch:

jobs:
  build:
    runs-on: ubuntu-latest
    steps: [checkout, setup .NET, restore, build, publish, upload-artifact]

  deploy-staging:
    if: github.event_name == 'pull_request'
    needs: build
    environment: staging
    # → deploys to staging Function App

  deploy-production:
    if: (github.event_name == 'push' || github.event_name == 'workflow_dispatch') && github.ref == 'refs/heads/master'
    needs: build
    environment: production
    # → deploys to production Function App
```

**Key design decisions:**
- `workflow_dispatch` included in `deploy-production` condition for manual re-deploys
- `.github/workflows/azure-functions.yml` in `paths` so workflow changes trigger builds
- Artifact upload/download between jobs for clean separation

### 5.2 SWA Workflow (`.github/workflows/azure-static-web-apps.yml`)

```yaml
name: Azure CloudZen Static Web Apps CI/CD

on:
  push:
    branches: [master]
    paths-ignore:
      - 'Api/**'
      - '.github/workflows/azure-functions.yml'
      - '*.md'
  pull_request:
    types: [opened, synchronize, reopened, closed]
    branches: [master]
    paths-ignore: [same as above]
  workflow_dispatch:

jobs:
  build-and-deploy:
    steps:
      # ... checkout, setup .NET, restore, build ...

      # PR builds: swap staging config before publish
      - name: Apply staging configuration
        if: github.event_name == 'pull_request'
        run: |
          cp wwwroot/appsettings.Staging.json wwwroot/appsettings.Production.json
          sed -i "s|$PRODUCTION_FUNC_HOSTNAME|$STAGING_FUNC_HOSTNAME|g" wwwroot/staticwebapp.config.json

      # ... publish, deploy to SWA ...

  close-staging:
    if: github.event_name == 'pull_request' && github.event.action == 'closed'
    # → destroys preview environment
```

**The staging config swap:** Before `dotnet publish`, the workflow copies `appsettings.Staging.json` over `appsettings.Production.json` and updates `staticwebapp.config.json` CSP headers to point to the staging Function App. This makes the preview environment talk to the staging backend.

### 5.3 Trigger Matrix

| File Changed | SWA Workflow | Functions Workflow |
|---|---|---|
| Blazor files (`*.razor`, `*.cs`, etc.) | ✅ | ❌ |
| `Api/**` | ❌ | ✅ |
| `azure-static-web-apps.yml` | ✅ | ❌ |
| `azure-functions.yml` | ❌ | ✅ |
| `*.md` | ❌ | ❌ |

---

## 6. How SWA Preview Environments Work

### URL Generation

Azure SWA auto-creates preview environments for PRs using the `GITHUB_TOKEN` context:

```
Production:  https://lively-flower-02783cd0f.azurestaticapps.net
             https://www.cloud-zen.net  (custom domain)

PR #5:       https://lively-flower-02783cd0f-5.azurestaticapps.net
PR #7:       https://lively-flower-02783cd0f-7.azurestaticapps.net
```

Custom domains are **never** assigned to preview environments — by design.

### Lifecycle

```
PR opened    → SWA creates preview env → unique URL active
PR updated   → SWA rebuilds preview env → same URL, new content
PR closed    → close-staging job runs  → preview env destroyed → URL stops working
```

### Free Plan Limits

| Feature | Limit |
|---------|-------|
| Concurrent preview environments | **3 max** |
| Custom domains | 2 |
| Max app size | 250 MB |

---

## 7. CORS & API Routing

### Direct API Calls (Our Architecture)

The Blazor app calls Function Apps **by full URL**, bypassing SWA's API proxy:

```
Browser (www.cloud-zen.net)
   │ Direct HTTPS (cross-origin)
   ▼
cloudzen-api-func-e4ge....azurewebsites.net/api/chat
   │ Function App handles CORS via AllowedOrigins config
```

This is what enables blue/green — each environment points to a different Function App URL via `appsettings.*.json`.

### Wildcard CORS for Staging

The staging Function App uses `AllowedOrigins__0 = *` because preview environment URLs are unpredictable (contain PR numbers). The `CorsSettings.IsOriginAllowed()` method supports wildcards:

```csharp
public bool IsOriginAllowed(string? origin)
{
    if (AllowedOrigins.Contains("*")) return true; // staging only
    return AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
}
```

### The `/api/*` Route in `staticwebapp.config.json`

This route exists but is **not actively used** — it's for SWA's linked API feature, which we don't use. Harmless to keep.

---

## 8. Day-to-Day Workflow

### Feature Development (Staging)

```bash
git checkout -b feature/my-change
# ... make changes ...
git push origin feature/my-change
# Open PR to master on GitHub
```

**What happens:**
1. SWA workflow creates a preview environment with staging config
2. Functions workflow deploys to the staging Function App (if `Api/` changed)
3. Preview URL appears in the PR status checks
4. Test at the preview URL — chatbot/email use the staging backend

### Ship to Production

```bash
# Merge the PR on GitHub
```

**What happens:**
1. Push to `master` triggers both production deploys
2. SWA production is updated at `www.cloud-zen.net`
3. Functions production is deployed (approval required if configured)
4. `close-staging` job destroys the preview environment

---

## 9. Testing Procedure

### Phase 1: Push Infrastructure to Master

```bash
git add -A
git commit -m "Add blue/green deployment infrastructure"
git push origin master
```

Wait for both workflows to complete at [GitHub Actions](https://github.com/dariemcarlosdev/CloudZen/actions). If `deploy-production` requires approval, approve it.

### Phase 2: Create Test PR

```bash
git checkout -b test/staging-pipeline
# Make a small visible change to a Blazor file AND an Api file
git add -A
git commit -m "Test: verify staging pipeline"
git push origin test/staging-pipeline
```

Open a PR to `master`.

### Phase 3: Verify Staging

1. Find the **preview URL** in the PR status checks or workflow log
2. Open it in a browser
3. **DevTools → Network tab**
4. Test the chatbot — verify requests go to `cloudzen-api-func-staging-hch0...` (not production)
5. Test the contact form — same verification

### Phase 4: Merge and Verify Production

1. Merge the PR
2. Watch GitHub Actions — production deploys trigger
3. Approve if required
4. Verify `https://www.cloud-zen.net` works normally
5. Delete the test branch

---

## 10. Troubleshooting

### Functions Blade Shows Empty / "0 functions found"

| Check | Fix |
|-------|-----|
| `FUNCTIONS_WORKER_RUNTIME` missing or `dotnet` | Set to `dotnet-isolated` |
| `FUNCTIONS_EXTENSION_VERSION` wrong | Set to `~4` |
| `WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED` wrong | Set to `1` |
| `AllowedOrigins__0` missing | Add `https://www.cloud-zen.net` (production) or `*` (staging) |
| `KEY_VAULT_ENDPOINT` set but auth fails | Remove it if not using Key Vault |

### `deploy-production` Doesn't Run

| Symptom | Cause | Fix |
|---------|-------|-----|
| Only `build` runs on manual trigger | Old condition excluded `workflow_dispatch` | Ensure condition: `(push \|\| workflow_dispatch) && refs/heads/master` |
| Job shows "Waiting" | `production` environment has required reviewers | Approve in GitHub Actions |
| Workflow didn't trigger | Changed files not in `paths` filter | Add `.github/workflows/azure-functions.yml` to `paths` |

### Both Workflows Trigger on One Push

The SWA workflow uses `paths-ignore`, so it triggers on everything **except** ignored paths. Ensure `azure-functions.yml` is in the SWA's `paths-ignore` and vice versa.

### 404 on `/api/chat` After Deploy

1. Check Functions blade — are Chat/SendEmail listed?
2. If empty → startup crash → check **Log stream** or **Application Insights → Failures → Exceptions**
3. If listed → CORS issue → check `AllowedOrigins__0` value
4. Try **Stop → Start** (not just Restart)

---

*Last updated: March 2026 — Blue/Green deployment with separate Function Apps and SWA preview environments.*
