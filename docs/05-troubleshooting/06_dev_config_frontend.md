# Issue #6: Blazor App Not Loading Development Configuration

## Quick Description
The Blazor app uses `/api` instead of `http://localhost:7257/api` for the API URL.

## Why This Issue Happens
1. `wwwroot/appsettings.Development.json` doesn't exist
2. The environment is not set to "Development"
3. Configuration file not being loaded

## Resolution

**Step 1: Create development config** (`wwwroot/appsettings.Development.json`)
```json
{
  "ApiBaseUrl": "http://localhost:7257/api"
}
```

**Step 2: Ensure file is copied to output**
The file should be in the `wwwroot` folder and will be automatically served.

**Step 3: Verify in browser**
Open browser DevTools → Network tab → Check the API request URL.
