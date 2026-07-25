> **Directory**: `docs/03-features/`  
> **Purpose**: Feature documentation for all CloudZen user-facing capabilities  
> **Audience**: AI assistants, developers  
> **Last Updated**: March 2026  

# Features Documentation

This directory contains comprehensive documentation for each CloudZen feature. Each document is self-contained and structured for consumption by AI models (ChatGPT, Claude, Gemini) as context or knowledge base input.

---

## Document Index

| # | Document | Scope | Key Endpoint |
|---|----------|-------|--------------|
| 01 | [Contact Form](./01_FEATURE_CONTACT_FORM.md) | Email contact form — UI, validation, API, Brevo SMTP delivery | `POST /api/send-email` |
| 02 | [Appointment System](./02_FEATURE_APPOINTMENT_SYSTEM.md) | Multi-step booking — schedule, cancel, reschedule via n8n | `POST /api/book-appointment` |
| 03 | [AI Chatbot](./03_FEATURE_CHATBOT.md) | AI virtual assistant — Anthropic Claude proxy, security, lead gen | `POST /api/chat` |
| 04 | [Brevo SMTP Migration](./04_FEATURE_BREVO_SMTP_MIGRATION.md) | Migration from Brevo REST API to SMTP — problem, solution, deploy | N/A (supplements 01) |

---

## How to Use These Docs

**For AI model context**: Each document includes a metadata block (scope, audience, date) and is structured with tables, clear hierarchies, and explicit cross-references. Feed individual docs or the full directory as knowledge base input.

**For developers**: Start with the feature doc you need. Each doc covers user flows, components, API contracts, configuration, and error handling for its feature.

## Cross-References

| Topic | Location |
|-------|----------|
| API endpoint specifications | `docs/01-architecture/API_ENDPOINTS.md` |
| Component architecture patterns | `docs/01-architecture/COMPONENT_ARCHITECTURE.md` |
| Configuration (IOptions pattern) | `docs/01-architecture/CONFIGURATION.md` |
| Azure Functions proxy pattern | `docs/06-patterns/01_azure_functions_proxy_api.md` |
| UI color and design system | `docs/06-patterns/02_ui_color_design_system.md` |
| Security (rate limiting, validation) | `docs/04-security/` |

---

*Last Updated: March 2026*
