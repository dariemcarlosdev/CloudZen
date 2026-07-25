# Issue #9: Azure Functions "0 Functions Found" — Missing `.azurefunctions` Folder in Deployment

## Quick Description
Azure Function App is running but reports 0 functions loaded. Azure Log Stream shows:
```
Could not find the .azurefunctions folder in the deployed artifacts of a .NET isolated function app.
Reading functions metadata (Custom)
0 functions found (Custom)
0 functions loaded
```
All endpoints return **404 Not Found**.

## Why This Issue Happens
The `upload-artifact@v4` GitHub Action **excludes hidden files/folders** (those starting with `.`) by default. The .NET isolated worker SDK generates a `.azurefunctions` folder during `dotnet publish` that the Azure Functions runtime requires to discover functions. When this folder is silently excluded from the uploaded artifact, the deploy job pushes an incomplete package to Azure.

**The failure chain:**
```
dotnet publish       → ✅ .azurefunctions/ generated in ./output
upload-artifact@v4   → ❌ .azurefunctions/ silently excluded (hidden folder)
download-artifact    → artifact missing .azurefunctions/
deploy to Azure      → incomplete package deployed
Azure Functions host → "0 functions found" → all routes return 404
```

## Resolution
Add `include-hidden-files: true` to the `upload-artifact@v4` step in `.github/workflows/azure-functions.yml`:

```yaml
- name: Upload build artifact
  uses: actions/upload-artifact@v4
  with:
    name: function-app
    path: ./output
    include-hidden-files: true  # Required for .azurefunctions folder
```

## Related Issues That Can Cause the Same Symptom
These were also fixed during the same investigation:

1. **Invalid JSON comments in `host.json`** — JSON does not support `//` comments. If `host.json` contains `//` commented-out blocks, the Azure Functions host fails to parse it, preventing function discovery. Visual Studio's editor tolerates JSONC, but the Azure runtime does not.

2. **Worker process crash on startup due to missing CORS config** — If neither `AllowedOrigins` nor `ProductionOrigin` environment variables are set in Azure App Settings, the `Program.cs` CORS configuration throws an `InvalidOperationException`, crashing the worker process before it can report its functions. The fix was to add `ProductionOrigin` as a fallback before throwing:
   ```csharp
   // Priority: AllowedOrigins → ProductionOrigin → Dev defaults → throw
   if (configuredOrigins is not null && configuredOrigins.Length > 0)
       allowedOrigins = configuredOrigins;
   else if (!string.IsNullOrEmpty(productionOrigin))
       allowedOrigins = [productionOrigin];
   else if (isDevelopment)
       allowedOrigins = new[] { "https://localhost:7243", "http://localhost:5054" };
   else
       throw new InvalidOperationException("CORS 'AllowedOrigins' or 'ProductionOrigin' must be configured.");
   ```

## Verification
After deploying, confirm in **Azure Portal > Function App > Functions** that both `Chat` and `SendEmail` appear, or check Log Stream for:
```
2 functions found (Custom)
2 functions loaded
```
