# Issue #13 — OWASP Top 10 Security Audit and Remediation

**Date:** April 2026
**Status:** Resolved
**Severity:** Critical / High
**Affected files:** `Api/Properties/launchSettings.json`, `Api/Features/Contact/SendEmailFunction.cs`, `Api/Features/Chat/ChatFunction.cs`, `Api/Shared/Security/InputValidator.cs`, `staticwebapp.config.json`, `wwwroot/index.html`, `Features/Chat/Components/CloudZenChatbot.razor.cs`

---

## Table of Contents

- [Description](#description)
- [Findings and Fixes](#findings-and-fixes)
- [Files Changed](#files-changed)
- [Verification](#verification)

---

## Description

A full OWASP Top 10 security audit was performed against the CloudZen codebase (Blazor WASM frontend and Azure Functions backend). The audit identified two critical and six high-severity issues. All eight were remediated in this pass.

---

## Findings and Fixes

### Critical Fixes

| # | OWASP Category | Issue | Resolution |
|---|----------------|-------|------------|
| 1 | A02 — Cryptographic Failures | **Hardcoded certificate password** (`CloudZenDev123!`) in `Api/Properties/launchSettings.json`, committed to Git history in 2 commits | Removed password from `commandLineArgs`. Scrubbed entire Git history with `git filter-branch` and force-pushed both `development` and `master` branches. HTTPS profile now relies on `func start --useHttps` auto-generated dev cert. |
| 2 | A02 — Cryptographic Failures | **SSL certificate validation bypass** in `SendEmailFunction.cs` — `ServerCertificateValidationCallback` unconditionally returned `true`, disabling all TLS chain validation for SMTP | Removed the `ServerCertificateValidationCallback` entirely. The .NET default TLS stack now performs full certificate chain validation. Only `CheckCertificateRevocation = false` is retained (revocation servers may be unreachable in restricted Azure networks). |

### High Fixes

| # | OWASP Category | Issue | Resolution |
|---|----------------|-------|------------|
| 3 | A05 — Security Misconfiguration | **No CSP or HSTS** in root `staticwebapp.config.json` (the file Azure Static Web Apps uses in production) | Added hardened `Content-Security-Policy` with scoped directives (`script-src`, `style-src`, `font-src`, `img-src`, `connect-src`, `frame-ancestors 'none'`, `base-uri 'self'`, `form-action 'self'`). Added `Strict-Transport-Security: max-age=31536000; includeSubDomains; preload`. |
| 4 | A08 — Software and Data Integrity | **No Subresource Integrity (SRI)** on CDN resources in `wwwroot/index.html` | Added `integrity` and `crossorigin="anonymous"` to Bootstrap Icons CSS. Documented that Tailwind CSS CDN cannot use SRI (JIT compiler output varies per request). |
| 5 | A03 — Injection (XSS) | **Unvalidated URLs in MarkupString** — `CloudZenChatbot.razor.cs` `HighlightContactInfo()` injected regex-matched URLs into `href` without scheme validation, allowing potential `javascript:` URIs | Added `Uri.TryCreate` validation requiring `http` or `https` scheme. Non-matching URLs are returned as plain text. The href value is normalized via `Uri.AbsoluteUri`. |
| 6 | A03 — Injection | **Chat messages skipped dangerous-content checks** — `ChatFunction.cs` validated message length and role but did not call `InputValidator.ContainsDangerousContent()`, unlike the email and booking functions | Replaced the length-only check with `InputValidator.ValidateTextInput()`, which applies XSS pattern detection, SQL injection detection, and length validation in one call. |
| 7 | A05 — Security Misconfiguration | **Wildcard CORS origin** — `CorsSettings.IsOriginAllowed()` accepted `"*"` with only a code comment warning against production use | Added a runtime environment guard. If `"*"` is configured outside the `Development` environment, `IsOriginAllowed()` throws `InvalidOperationException` to fail fast. |
| 8 | A05 — Security Misconfiguration | **Overly permissive CSP in API security headers** — `InputValidator.AddSecurityHeaders()` used a flat CSP with `'unsafe-inline'` in `style-src` and no resource-type directives | Replaced with scoped per-type directives: explicit `font-src`, `img-src`, `connect-src`, plus `frame-ancestors 'none'`, `base-uri 'self'`, and `form-action 'self'`. |

---

## Files Changed

| File | Change |
|------|--------|
| `Api/Properties/launchSettings.json` | Removed `--cert certificate.pfx --password CloudZenDev123!` from HTTPS command args |
| `Api/Features/Contact/SendEmailFunction.cs` | Removed `ServerCertificateValidationCallback` (11 lines) |
| `staticwebapp.config.json` | Added `Content-Security-Policy` and `Strict-Transport-Security` headers |
| `wwwroot/index.html` | Added SRI to Bootstrap Icons CSS; added note on Tailwind CDN SRI limitation |
| `Features/Chat/Components/CloudZenChatbot.razor.cs` | Added URL scheme validation (`http`/`https` only) with `Uri.TryCreate` |
| `Api/Features/Chat/ChatFunction.cs` | Replaced length check with `InputValidator.ValidateTextInput()` |
| `Api/Shared/Security/InputValidator.cs` | Hardened CSP in `AddSecurityHeaders()`; added environment guard for wildcard CORS |

---

## Verification

- Full solution build: zero errors
- Git history search for scrubbed password: zero matches (`git log --all -S "CloudZenDev123"`)
- Force-pushed rewritten history to `origin/development` and `origin/master`
