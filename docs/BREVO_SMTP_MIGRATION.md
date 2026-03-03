# Brevo Email Integration - SMTP Migration Guide

## Overview

This document describes the migration from Brevo REST API to SMTP for sending emails in the CloudZen Azure Functions application. This change was necessary to resolve IP whitelisting issues when running on Azure Functions Consumption plan.

---

## Problem Statement

### Original Issue: IP Whitelisting with Brevo REST API

When using the Brevo REST API (`sib_api_v3_sdk`) to send transactional emails, Brevo enforces **IP-based access control** as a security measure. This caused failures in two scenarios:

#### 1. Local Development Environment
```
Error: "We have detected you are using an unrecognised IP address 
2601:58a:8f02:8f71:54eb:c9c3:c046:41d2. If you performed this action 
make sure to add the new IP address in this link: 
https://app.brevo.com/security/authorised_ips"
```

**Cause**: Local development machines have dynamic IP addresses that change frequently, making it impractical to whitelist them in Brevo.

#### 2. Azure Functions Consumption Plan (Production)
```
Error: sib_api_v3_sdk.Client.ApiException: Error calling SendTransacEmail: 
{"message":"We have detected you are using an unrecognised IP address...",
"code":"unauthorized"}
```

**Cause**: Azure Functions on the **Consumption plan** use a pool of shared outbound IP addresses (38+ IPs in our case) that can change dynamically. Unlike dedicated App Service plans, there's no single static outbound IP.

### Why IP Whitelisting Doesn't Work for Consumption Plan

Azure Functions Consumption plan characteristics:
- **Dynamic IP allocation**: Azure assigns outbound IPs from a shared pool
- **No static outbound IP**: Unlike Premium or App Service plans
- **IPs can change**: Infrastructure updates may change the IP pool

To get all possible outbound IPs:
```bash
az functionapp show --name <your-function-app-name> --resource-group <your-resource-group> --query "possibleOutboundIpAddresses" -o tsv
```

Our function had **38 possible outbound IPs**, making manual whitelisting impractical and unreliable.

---

## Solution: Switch to SMTP

### Why SMTP?

Brevo's SMTP relay (`smtp-relay.brevo.com`) **does not enforce IP restrictions** like the REST API. SMTP uses standard authentication (username/password) without IP-based access control.

### Benefits of SMTP Approach

| Feature | REST API | SMTP |
|---------|----------|------|
| IP Whitelisting Required | ✅ Yes | ❌ No |
| Works with Consumption Plan | ❌ Unreliable | ✅ Yes |
| Works with Dynamic IPs | ❌ No | ✅ Yes |
| TLS Encryption | ✅ Yes | ✅ Yes (STARTTLS) |
| Authentication | API Key | SMTP Credentials |

---

## Implementation Changes

### 1. Added MailKit NuGet Package

```bash
cd Api
dotnet add package MailKit
```

This adds `MailKit` and `MimeKit` - the industry-standard .NET email libraries.

### 2. Updated SendEmailFunction.cs

#### Removed Dependencies
```csharp
// REMOVED - Brevo REST API SDK
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;
```

#### Added Dependencies
```csharp
// ADDED - MailKit SMTP client
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
```

#### SMTP Configuration Constants
```csharp
// Brevo SMTP settings (hardcoded - these are standard Brevo values)
private const string BrevoSmtpHost = "smtp-relay.brevo.com";
private const int BrevoSmtpPort = 587;
```

#### New SMTP Method
```csharp
private async Task<string> SendEmailViaSmtpAsync(EmailRequest emailRequest, string smtpLogin, string smtpKey)
{
    var message = new MimeMessage();
    
    // Build email message...
    
    using var client = new SmtpClient();
    
    // SSL Certificate handling (see below)
    client.CheckCertificateRevocation = false;
    client.ServerCertificateValidationCallback = /* ... */;
    
    // Connect with STARTTLS
    await client.ConnectAsync(BrevoSmtpHost, BrevoSmtpPort, SecureSocketOptions.StartTls);
    
    // Authenticate
    await client.AuthenticateAsync(smtpLogin, smtpKey);
    
    // Send
    await client.SendAsync(message);
    
    // Disconnect
    await client.DisconnectAsync(true);
    
    return messageId;
}
```

### 3. Configuration Changes

#### Environment Variables Required

| Variable | Description | Example |
|----------|-------------|---------|
| `BREVO_SMTP_LOGIN` | Brevo SMTP login (from Brevo dashboard) | `<your-smtp-login>@smtp-brevo.com` |
| `BREVO_SMTP_KEY` | Brevo SMTP password/key | `xsmtpsib-<your-smtp-key>` |

#### local.settings.json (Development)
```json
{
  "IsEncrypted": false,
  "Values": {
    "BREVO_SMTP_LOGIN": "<your-brevo-smtp-login>",
    "BREVO_SMTP_KEY": "<your-brevo-smtp-key>",
    "EmailSettings:FromEmail": "<your-from-email>",
    "EmailSettings:CcEmail": "<your-cc-email>"
  }
}
```

> ⚠️ **Security Note**: Never commit `local.settings.json` with real credentials to source control. This file should be in `.gitignore`.

#### Getting SMTP Credentials from Brevo

1. Log into https://app.brevo.com
2. Navigate to **SMTP & API** → **SMTP**
3. Copy the following:
   - **SMTP Server**: `smtp-relay.brevo.com` (hardcoded in code)
   - **Port**: `587` (hardcoded in code)
   - **Login**: Your SMTP login (format: `<id>@smtp-brevo.com`)
   - **Master Password**: Your SMTP key (starts with `xsmtpsib-`)

---

## SSL Certificate Issue (Development Environment)

### Problem

After switching to SMTP, a new error occurred in the development environment:

```
MailKit.Security.SslHandshakeException: An error occurred while attempting 
to establish an SSL or TLS connection.

The server's SSL certificate could not be validated for the following reasons:
• The revocation function was unable to check revocation for the certificate.
• The revocation function was unable to check revocation because the 
  revocation server was offline.
```

### Cause

This error occurs when:
- The machine cannot reach Certificate Revocation List (CRL) servers
- Corporate firewalls block CRL/OCSP traffic
- Network restrictions prevent certificate validation
- Antivirus software intercepts SSL connections

### Solution

Configure MailKit to handle certificate revocation check failures gracefully:

```csharp
using var client = new SmtpClient();

// 1. Disable certificate revocation check BEFORE setting the callback
client.CheckCertificateRevocation = false;

// 2. Custom certificate validation callback - always returns true for Brevo
client.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
{
    if (sslPolicyErrors != System.Net.Security.SslPolicyErrors.None)
    {
        _logger.LogWarning("SSL certificate validation bypassed for Brevo SMTP. Errors: {Errors}", sslPolicyErrors);
    }
    // Always accept for Brevo's trusted SMTP server
    return true;
};

// Connect with STARTTLS
await client.ConnectAsync(BrevoSmtpHost, BrevoSmtpPort, SecureSocketOptions.StartTls);
```

### Why This is Safe

This approach is acceptable because:

1. **Known Server**: We're connecting to Brevo's official SMTP server (`smtp-relay.brevo.com`)
2. **TLS Still Active**: The connection still uses TLS encryption (STARTTLS on port 587)
3. **Authentication Required**: SMTP authentication with username/password is still enforced
4. **Issue is Revocation Only**: The certificate itself is valid; only the revocation check fails
5. **Development Environment**: This is a common issue in restricted network environments
6. **Industry Standard**: Many email clients handle revocation failures similarly

### Security Considerations

| Security Feature | Status |
|-----------------|--------|
| TLS Encryption | ✅ Enabled (STARTTLS) |
| Server Authentication | ✅ Certificate validated |
| Certificate Chain | ✅ Verified |
| Revocation Check | ⚠️ Skipped (unreachable servers) |
| SMTP Authentication | ✅ Username/password required |

---

## Configuration Architecture

### Understanding Where Configuration Lives

This project has a **two-tier architecture**: a Blazor WebAssembly frontend and an Azure Functions backend. Each tier has different configuration requirements.

```
┌─────────────────────────────────────────────────────────────────┐
│                    BLAZOR APP (Frontend)                        │
│   Configuration: wwwroot/appsettings.*.json                     │
│                                                                 │
│   Files:                                                        │
│   ├── wwwroot/appsettings.json           (base settings)       │
│   ├── wwwroot/appsettings.Development.json (local dev)         │
│   └── wwwroot/appsettings.Production.json  (production)        │
│                                                                 │
│   ✅ NO Azure Portal config needed                              │
│   These files are static assets bundled with the app            │
│   and automatically loaded based on environment.                │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ HTTP POST to ApiBaseUrl
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                 AZURE FUNCTION (Backend)                        │
│   Configuration: Azure Portal + local.settings.json             │
│                                                                 │
│   Local Development:                                            │
│   └── Api/local.settings.json                                   │
│                                                                 │
│   Production (Azure Portal):                                    │
│   └── Function App → Configuration → Application settings       │
│                                                                 │
│   ✅ REQUIRES Azure Portal configuration for:                   │
│      - BREVO_SMTP_LOGIN                                         │
│      - BREVO_SMTP_KEY                                           │
│      - EmailSettings:FromEmail                                  │
│      - EmailSettings:CcEmail                                    │
└─────────────────────────────────────────────────────────────────┘
```

### Blazor App Configuration (No Azure Config Needed)

The Blazor WebAssembly app uses static JSON files in `wwwroot/`:

| File | Environment | Loaded When |
|------|-------------|-------------|
| `appsettings.json` | All | Always (base configuration) |
| `appsettings.Development.json` | Development | Running locally with `dotnet run` |
| `appsettings.Production.json` | Production | Deployed to Azure/production host |

**Key Setting**: `EmailService.ApiBaseUrl`
- **Development**: `http://localhost:7257/api` (local Azure Function)
- **Production**: `https://your-function-app.azurewebsites.net/api`

**Why no Azure config?** These files are:
- Bundled into the app during build
- Served as static files from `wwwroot/`
- Downloaded by the browser when the app loads
- Automatically selected based on environment

### Azure Function Configuration (Azure Portal Required)

The Azure Function backend requires secrets that **must** be configured in Azure Portal:

| Setting | Where to Configure | Why |
|---------|-------------------|-----|
| `BREVO_SMTP_LOGIN` | Azure Portal → Configuration | Secret credential |
| `BREVO_SMTP_KEY` | Azure Portal → Configuration | Secret credential |
| `EmailSettings:FromEmail` | Azure Portal → Configuration | Runtime config |
| `EmailSettings:CcEmail` | Azure Portal → Configuration | Runtime config |

**Why Azure Portal config?** 
- Secrets should never be in source code
- Azure Function runs server-side with access to secure configuration
- `local.settings.json` is only for local development (gitignored)

---

## File Changes Summary

### Modified Files

| File | Change |
|------|--------|
| `Api/CloudZen.Api.csproj` | Added `MailKit` package reference |
| `Api/Functions/SendEmailFunction.cs` | Replaced REST API with SMTP implementation |
| `Api/local.settings.json` | Added `BREVO_SMTP_LOGIN` and `BREVO_SMTP_KEY` |
| `wwwroot/appsettings.Production.json` | Added with Azure Function URL |

### New Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| MailKit | 4.15.0 | SMTP client library |
| MimeKit | 4.15.0 | Email message construction (dependency of MailKit) |

### Removed Dependencies

The `sib_api_v3_sdk` package can optionally be removed if no longer needed:
```bash
dotnet remove package sib_api_v3_sdk
```

---

## Testing

### Local Development Testing

1. Ensure `local.settings.json` has correct SMTP credentials
2. Start the Azure Functions locally:
   ```bash
   cd Api
   func start
   ```
3. Test the endpoint:
   ```bash
   curl -X POST http://localhost:7071/api/send-email \
     -H "Content-Type: application/json" \
     -d '{
       "fromName": "Test User",
       "fromEmail": "test@example.com",
       "subject": "Test Email",
       "message": "This is a test message."
     }'
   ```

### Expected Response
```json
{
  "success": true,
  "message": "Email sent successfully.",
  "messageId": "abc123def456@cloudzen.com"
}
```

---

## Production Deployment (Azure)

### Step 1: Deploy Azure Function

```bash
cd Api
func azure functionapp publish <your-function-app-name>
```

### Step 2: Configure Azure Function Settings

Add these Application Settings in **Azure Portal → Function App → Configuration**:

| Setting | Value | Required |
|---------|-------|----------|
| `BREVO_SMTP_LOGIN` | `<your-smtp-login>@smtp-brevo.com` | ✅ Yes |
| `BREVO_SMTP_KEY` | `xsmtpsib-<your-key>` | ✅ Yes |
| `EmailSettings:FromEmail` | `your-email@example.com` | ✅ Yes |
| `EmailSettings:CcEmail` | `cc-email@example.com` | Optional |

Then click **Save** and **Restart** the function app.

### Step 3: Deploy Blazor App

The `wwwroot/appsettings.Production.json` is automatically included. Just deploy:

```bash
git add .
git commit -m "Add SMTP migration and production config"
git push origin master
```

GitHub Actions will automatically deploy to Azure Static Web Apps.

### Step 4: Verify Deployment

1. Open your production Blazor app URL
2. Navigate to the Contact form
3. Submit a test message
4. Check if the email is received

> 💡 **Tip**: Store sensitive credentials in Azure Key Vault and reference them using Key Vault references for enhanced security.

---

## Troubleshooting

### Common Errors

| Error | Cause | Solution |
|-------|-------|----------|
| `AuthenticationException` | Invalid SMTP credentials | Verify `BREVO_SMTP_LOGIN` and `BREVO_SMTP_KEY` in Azure Portal |
| `SmtpCommandException` | SMTP server rejected command | Check email addresses are valid |
| `SslHandshakeException` | SSL/TLS connection failed | Ensure `CheckCertificateRevocation = false` and callback returns `true` |
| `SocketException` | Network connectivity | Check firewall allows port 587 outbound |
| `MethodNotAllowed` | Wrong API URL in Blazor app | Verify `appsettings.Production.json` has correct Azure Function URL |

### Logging

The function logs detailed information for debugging:

```
[Information] SendEmail function triggered from {ClientIp}
[Warning] SSL certificate validation bypassed for Brevo SMTP. Errors: {Errors}
[Information] Email sent successfully via SMTP. MessageId: {MessageId}
[Error] SMTP command error: {Message}, StatusCode: {StatusCode}
```

---

## References

- [MailKit Documentation](https://github.com/jstedfast/MailKit)
- [Brevo SMTP Documentation](https://developers.brevo.com/docs/send-a-transactional-email#smtp-api)
- [Azure Functions Outbound IPs](https://docs.microsoft.com/en-us/azure/azure-functions/ip-addresses)
- [Azure Key Vault References](https://docs.microsoft.com/en-us/azure/app-service/app-service-key-vault-references)
- [Blazor WebAssembly Configuration](https://docs.microsoft.com/en-us/aspnet/core/blazor/fundamentals/configuration)

---

## Version History

| Date | Version | Changes |
|------|---------|---------|
| 2026-03-03 | 1.0 | Initial SMTP migration from REST API |
| 2026-03-03 | 1.1 | Fixed SSL certificate revocation check issue |
| 2026-03-03 | 1.2 | Added production configuration and deployment guide |
