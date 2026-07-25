# Issue #2: TimeSpan Configuration Error in host.json

## Quick Description
Azure Function fails to start with error:
```
Failed to convert configuration value at 'AzureFunctionsJobHost:extensions:http:hsts:MaxAge' 
to type 'System.TimeSpan'. The TimeSpan string '31536000' could not be parsed.
```

## Why This Issue Happens
The `maxAge` property in `host.json` expects a **TimeSpan format**, not raw seconds:
```json
// WRONG - raw seconds
"maxAge": "31536000"

// CORRECT - TimeSpan format (days.hours:minutes:seconds)
"maxAge": "365.00:00:00"
```

## Resolution
Update `Api/host.json`:
```json
{
    "extensions": {
        "http": {
            "hsts": {
                "isEnabled": true,
                "maxAge": "365.00:00:00",  // 365 days in TimeSpan format
                "includeSubDomains": true,
                "preload": true
            }
        }
    }
}
```

**TimeSpan Format Reference:**
| Value | Format | Meaning |
|-------|--------|---------|
| 1 hour | `01:00:00` | hours:minutes:seconds |
| 1 day | `1.00:00:00` | days.hours:minutes:seconds |
| 365 days | `365.00:00:00` | 365 days |
