# Design Patterns Applied in CloudZen

This folder documents the recurring design patterns used across the CloudZen solution.

---

## Patterns Index

| # | Pattern | Layer | File |
|---|---------|-------|------|
| 01 | Azure Functions Proxy | API / Frontend | [01_azure_functions_proxy_api.md](01_azure_functions_proxy_api.md) |
| 02 | UI Color & Design System | Frontend | [02_ui_color_design_system.md](02_ui_color_design_system.md) |
| 03 | Request/Response — Resource Awareness | API / Frontend | [03_request_response_token_awareness.md](03_request_response_token_awareness.md) |

---

## Core Security Rule

> **API keys and secrets live only in the Functions backend. The WASM client never holds secrets.**

All external service integrations (email, AI, etc.) follow this principle — the Blazor WebAssembly frontend calls Azure Functions endpoints over HTTP, and the Functions backend holds the secrets and communicates with third-party APIs on behalf of the client.

---

*Last Updated: March 2026*
