# Issue #7: Rate Limit Exceeded (429 Error)

## Quick Description
API returns:
```json
{
    "error": "Rate limit exceeded. Try again in 60 seconds."
}
```

## Why This Issue Happens
The rate limiter restricts requests to 10 per 60 seconds per client IP (default configuration).

## Resolution

**Option 1: Wait for the window to reset** (60 seconds)

**Option 2: Restart the Azure Function** (clears in-memory rate limiter)

**Option 3: Adjust rate limit settings** (`Api/local.settings.json`)
```json
{
    "Values": {
        "RateLimiting:PermitLimit": "100",
        "RateLimiting:WindowSeconds": "60"
    }
}
```
