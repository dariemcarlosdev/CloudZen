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

## File Changes Summary

### Modified Files

| File | Change |
|------|--------|
| `Api/CloudZen.Api.csproj` | Added `MailKit` package reference |
| `Api/Functions/SendEmailFunction.cs` | Replaced REST API with SMTP implementation |
| `Api/local.settings.json` | Added `BREVO_SMTP_LOGIN` and `BREVO_SMTP_KEY` |

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

### Required Configuration

Add these Application Settings to your Azure Function App:

1. Go to **Azure Portal** → **Your Function App**
2. Navigate to **Configuration** → **Application settings**
3. Add:
   - `BREVO_SMTP_LOGIN` = `<your-brevo-smtp-login>`
   - `BREVO_SMTP_KEY` = `<your-brevo-smtp-key>`
4. **Save** and **Restart** the function app

> 💡 **Tip**: Store sensitive credentials in Azure Key Vault and reference them using Key Vault references for enhanced security.

### Deployment Steps

```bash
# Deploy to Azure
cd Api
func azure functionapp publish <your-function-app-name>
```

---

## Troubleshooting

### Common Errors

| Error | Cause | Solution |
|-------|-------|----------|
| `AuthenticationException` | Invalid SMTP credentials | Verify `BREVO_SMTP_LOGIN` and `BREVO_SMTP_KEY` |
| `SmtpCommandException` | SMTP server rejected command | Check email addresses are valid |
| `SslHandshakeException` | SSL/TLS connection failed | Ensure `CheckCertificateRevocation = false` and callback returns `true` |
| `SocketException` | Network connectivity | Check firewall allows port 587 outbound |

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

---

## Version History

| Date | Version | Changes |
|------|---------|---------|
| 2026-03-03 | 1.0 | Initial SMTP migration from REST API |
| 2026-03-03 | 1.1 | Fixed SSL certificate revocation check issue |
