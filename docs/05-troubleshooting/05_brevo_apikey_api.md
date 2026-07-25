# Issue #5: Brevo API Key Not Configured

## Quick Description
Email sending fails with 500 error:
```json
{
    "error": "Email service is not configured properly."
}
```

## Why This Issue Happens
The `BREVO_API_KEY` environment variable is missing or empty in `local.settings.json`.

## Resolution
Update `Api/local.settings.json`:
```json
{
    "Values": {
        "BREVO_API_KEY": "xkeysib-your-actual-api-key-here"
    }
}
```

⚠️ **Security Note:** Never commit real API keys to source control. Ensure `local.settings.json` is in `.gitignore`.
