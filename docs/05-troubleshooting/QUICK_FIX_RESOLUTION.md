# Quick Fix Resolution Guide

This document indexes common issues encountered in the CloudZen project. Each issue has been split into its own file for clarity.

---

## Issues Index

| # | Issue | Layer | File |
|---|-------|-------|------|
| 01 | CORS Error — Blazor Contact Form Cannot Call Azure Function | API | [01_cors_error_api.md](01_cors_error_api.md) |
| 02 | TimeSpan Configuration Error in host.json | API | [02_timespan_config_api.md](02_timespan_config_api.md) |
| 03 | ECONNREFUSED — Cannot Connect to Azure Function | API | [03_econnrefused_api.md](03_econnrefused_api.md) |
| 04 | File Locked by .NET Host | Build | [04_file_locked_build.md](04_file_locked_build.md) |
| 05 | Brevo API Key Not Configured | API | [05_brevo_apikey_api.md](05_brevo_apikey_api.md) |
| 06 | Blazor App Not Loading Development Configuration | Frontend | [06_dev_config_frontend.md](06_dev_config_frontend.md) |
| 07 | Rate Limit Exceeded (429 Error) | API | [07_rate_limit_api.md](07_rate_limit_api.md) |
| 08 | Azurite Storage Emulator Not Running | Infrastructure | [08_azurite_emulator_infrastructure.md](08_azurite_emulator_infrastructure.md) |
| 09 | Azure Functions "0 Functions Found" | Deployment | [09_zero_functions_found_deployment.md](09_zero_functions_found_deployment.md) |
| 10 | CSP Blocks CDN Resources on First Load (Service Worker) | Frontend | [10_csp_blocks_cdn_frontend.md](10_csp_blocks_cdn_frontend.md) |
| 11 | CORS Error — N8N Booking Workflow | Frontend | [11_cors_n8n_booking_frontend.md](11_cors_n8n_booking_frontend.md) |
| 12 | Blazor Component Parameter Mismatch | Frontend | [12_component_parameter_mismatch_frontend.md](12_component_parameter_mismatch_frontend.md) |

---

## Concepts Reference

### Service Workers in Azure Static Web Apps

A **service worker** is a JavaScript file that runs in the background of the browser, separate from the web page. In a Blazor WebAssembly PWA, it serves two purposes:

- **Offline support**: On install, it caches all app assets (`.dll`, `.wasm`, `.html`, `.css`, `.js`) listed in the assets manifest. On subsequent visits, it serves cached responses instead of hitting the network.
- **Fetch interception**: It listens to the `fetch` event and intercepts every HTTP request the page makes — including requests for external CDN resources.

**Key behavior that causes CSP issues:** When a service worker calls `fetch()`, those requests are governed by the **`connect-src`** CSP directive — not `script-src`, `style-src`, or `default-src`. This means even if `default-src` allows a CDN origin, the service worker's fetch to that same origin will be blocked if `connect-src` doesn't include it.

**Development vs. Production:** Blazor uses two service worker files:
- `service-worker.js` — Used in development; does nothing (empty fetch handler).
- `service-worker.published.js` — Used in production; implements full caching and fetch interception. This is why the issue only appears in production.

Azure Static Web Apps applies the CSP headers defined in `staticwebapp.config.json` to all responses. The service worker, running within that CSP context, must comply with all directives — particularly `connect-src` for any `fetch()` calls it makes.

---

## Quick Reference: Development URLs

| Component | Default URL |
|-----------|-------------|
| Blazor App (IIS Express) | `https://localhost:44370` |
| Blazor App (Kestrel) | `https://localhost:5001` |
| Azure Function | `http://localhost:7071` or `http://localhost:7257` |
| Azurite Blob | `http://127.0.0.1:10000` |
| Azurite Queue | `http://127.0.0.1:10001` |
| Azurite Table | `http://127.0.0.1:10002` |

---

## Quick Reference: Key Files

| Purpose | File Path |
|---------|-----------|
| Azure Function Config | `Api/local.settings.json` |
| Azure Function Host Config | `Api/host.json` |
| Blazor Dev Config | `wwwroot/appsettings.Development.json` |
| Blazor Prod Config | `wwwroot/appsettings.json` |
| CORS Settings | `Api/Security/InputValidator.cs` |
| Email Function | `Api/Functions/SendEmailFunction.cs` |
| Chat Function | `Api/Functions/ChatFunction.cs` |
| Email Service (Blazor) | `Services/ApiEmailService.cs` |
| CI/CD Workflow | `.github/workflows/azure-functions.yml` |

---

*Last Updated: March 2026*
