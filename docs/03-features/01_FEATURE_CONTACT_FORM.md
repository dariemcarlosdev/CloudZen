> **Document**: Contact Form Feature
> **Scope**: Email contact form — UI, validation, API integration, Brevo SMTP delivery
> **Audience**: AI assistants, developers
> **Last Updated**: March 2026

# Contact Form Feature

## Table of Contents

- [Overview](#overview)
- [Quick Reference](#quick-reference)
- [User Flow](#user-flow)
- [Components](#components)
- [API Integration](#api-integration)
- [Email Delivery](#email-delivery)
- [Configuration](#configuration)
- [Request/Response](#requestresponse)
- [Validation](#validation)
- [Error Handling](#error-handling)
- [Entry Points](#entry-points)
- [Related Docs](#related-docs)

---

## Overview

Email contact form allowing users to send inquiries directly to CloudZen business email via Azure Functions and Brevo SMTP.

### Scope Boundaries

This document covers the contact form UI, client-side validation, API integration, and email delivery flow. It does **not** cover:

- SMTP provider setup, migration steps, or MailKit configuration details — see [04_FEATURE_BREVO_SMTP_MIGRATION.md](04_FEATURE_BREVO_SMTP_MIGRATION.md)
- Rate limiting internals — see [API_ENDPOINTS.md](../01-architecture/API_ENDPOINTS.md)
- General IOptions configuration patterns — see [CONFIGURATION.md](../01-architecture/CONFIGURATION.md)

---

## Quick Reference

| Item | Value |
|------|-------|
| **Endpoint** | `POST /api/send-email` |
| **Frontend Component** | `Features/Contact/Components/ContactForm.razor` |
| **Backend Function** | `Api/Features/Contact/SendEmailFunction.cs` |
| **Transport** | Brevo SMTP (MailKit) |
| **Entry Point** | Hero "Get in Touch" button → `#contact` anchor |

---

## User Flow

| Step | Action | Component |
|------|--------|-----------|
| 1 | User clicks "Get in Touch" in Hero | `Hero.razor` |
| 2 | Page scrolls to `#contact` section | Anchor navigation |
| 3 | User fills form (name, email, subject, message) | `ContactForm.razor` |
| 4 | User submits form | `ApiEmailService` |
| 5 | Success/error feedback displayed | `ContactForm.razor` |

---

## Components

| Component | Location | Purpose |
|-----------|----------|---------|
| `ContactForm.razor` | `Features/Contact/Components/` | Form UI with validation |
| `ContactForm.razor.cs` | `Features/Contact/Components/` | Form logic, submission handling |
| `ApiEmailService` | `Features/Contact/Services/` | HTTP client for email API |
| `IEmailService` | `Features/Contact/Services/` | Service interface |
| `ContactFormModel` | `Features/Contact/Models/` | Form model with validation |
| `EmailResult` | `Features/Contact/Models/` | Result type with `Ok()`/`Fail()` |

---

## API Integration

| Property | Value |
|----------|-------|
| **Endpoint** | `POST /api/send-email` |
| **Function** | `Api/Features/Contact/SendEmailFunction.cs` |
| **Transport** | Brevo SMTP (`smtp-relay.brevo.com:587`) |
| **Library** | MailKit + MimeKit |

> See [04_FEATURE_BREVO_SMTP_MIGRATION.md](04_FEATURE_BREVO_SMTP_MIGRATION.md) for SMTP implementation details.

---

## Email Delivery

| Setting | Value |
|---------|-------|
| **From Address** | `cloudzen.inc@gmail.com` |
| **CC** | `softevolutionsl@gmail.com` |
| **Format** | Multipart MIME (HTML + plain text) |
| **User content** | HTML-encoded for security |

---

## Configuration

### Frontend (Blazor)

| Setting | File | Purpose |
|---------|------|---------|
| `EmailService.ApiBaseUrl` | `appsettings.json` | API endpoint base URL |
| `EmailService.SendEmailEndpoint` | `appsettings.json` | Endpoint path |

### Backend (Azure Function)

| Setting | Location | Purpose |
|---------|----------|---------|
| `BREVO_SMTP_LOGIN` | Azure Portal / Key Vault | SMTP username |
| `BREVO_SMTP_KEY` | Azure Portal / Key Vault | SMTP password |
| `EmailSettings:FromEmail` | Azure Portal | Sender address |
| `EmailSettings:CcEmail` | Azure Portal | CC address |

---

## Request/Response

### Request Body

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| `subject` | string | Yes | Max 200 chars |
| `message` | string | Yes | Max 5000 chars |
| `fromName` | string | Yes | Max 100 chars |
| `fromEmail` | string | Yes | Valid email, max 254 chars |

### Response

| Field | Type | Description |
|-------|------|-------------|
| `success` | boolean | Operation result |
| `message` | string | User-friendly message |
| `messageId` | string | Email message ID (on success) |

---

## Validation

| Type | Implementation |
|------|----------------|
| **Client-side** | Data annotations on `ContactFormModel` |
| **Server-side** | `InputValidator` XSS/SQL injection patterns |
| **Email format** | RFC 5321, max 254 chars |

---

## Error Handling

| Error | User Message | Logged |
|-------|--------------|--------|
| Network failure | "Unable to send. Please try again." | Yes |
| Timeout | "Request timed out. Please try again." | Yes |
| Validation | Specific field errors | No |
| SMTP failure | "Something went wrong." | Yes (details) |

---

## Entry Points

| Location | CTA Text | Icon | Action |
|----------|----------|------|--------|
| Hero | "Get in Touch" | `bi-envelope` | Scrolls to `#contact` |

---

## Related Docs

- [API_ENDPOINTS.md](../01-architecture/API_ENDPOINTS.md) — Full endpoint specification
- [04_FEATURE_BREVO_SMTP_MIGRATION.md](04_FEATURE_BREVO_SMTP_MIGRATION.md) — SMTP technical details
- [CONFIGURATION.md](../01-architecture/CONFIGURATION.md) — IOptions pattern

---

*Last Updated: March 2026*
