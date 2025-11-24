# CloudZen Doc.

CloudZen is a modern Blazor WebAssembly application designed as a professional portfolio and consulting showcase for Dariem C. Macias, founder and principal consultant at CloudZen Inc.

## Project Description
CloudZen demonstrates expertise in building scalable, secure, and modern web applications using .NET 8, Blazor, and Azure Cloud. The project highlights real-world results in application modernization, DevOps, AI-driven automation, and enterprise backend systems. It features a clean, responsive UI and integrates best practices for performance, maintainability, and cloud readiness.

## Features
- Professional portfolio and consulting showcase
- Modern, responsive UI with Tailwind CSS
- Blazor WebAssembly SPA architecture
- Downloadable resume and case studies
- Contact form and testimonials
- Highlights of key results and core expertise
- Integration with Azure and AI services (showcased in content)

## Services Tech Stack
- Blazor WebAssembly (.NET 8)
- C#
- ASP.NET Core
- Azure Cloud Services
- Entity Framework Core
- DevOps: Azure DevOps, GitHub Actions
- Power BI & SSIS (for reporting/ETL)
- Azure OpenAI & Cognitive Services
- SQL & Relational Databases
- RESTful Web APIs
- Tailwind CSS
- JavaScript (for interop, if applicable)

---

## Azure Deployment Summary

- Deploy using Azure Static Web Apps and GitHub Actions workflow.
- Add `staticwebapp.config.json` to `wwwroot/` for routing and security headers.
- Store public configuration only in `appsettings.json` (never secrets).
- Use Azure Blob Storage with SAS tokens for public files (e.g., resume download).
- For secure operations (email, uploads), create an Azure Functions backend and store secrets in Azure Key Vault.
- Configure CORS for Blob Storage to allow access from your Static Web App domain.
- Application Insights recommended for monitoring.

### Key Steps
1. Create Azure Static Web App and link to GitHub.
2. Create Azure Blob Storage and configure containers and CORS.
3. Create Azure Key Vault and store secrets (API keys, connection strings).
4. Create Azure Functions for backend operations (email, uploads) and grant Key Vault access via Managed Identity.
5. Remove all secrets from `wwwroot/appsettings.json`.
6. Test locally and deploy via GitHub Actions.

---

## Critical Security Best Practices

- **Never store API keys or secrets in Blazor WebAssembly config files.**
- All files in `wwwroot/` are public and downloadable by anyone.
- Use Azure Functions backend to handle sensitive operations and access secrets securely from Key Vault.
- Rotate any exposed API keys immediately and remove them from the repository history.
- Add sensitive config files (e.g., `appsettings.Development.json`) to `.gitignore`.
- Review and follow the security checklist in `SECURITY_ALERT.md` and `deployment_guide.md`.

---

## License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.