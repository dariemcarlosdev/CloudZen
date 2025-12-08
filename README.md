# CloudZen

A modern Blazor WebAssembly portfolio and consulting showcase built with .NET 8, demonstrating expertise in building scalable, secure cloud applications with Azure integration.

## 🚀 Features

- Professional portfolio with project showcase and filtering
- Modern, responsive UI with Tailwind CSS
- Component-based architecture for maintainability
- Resume download and contact form
- GitHub Actions CI/CD pipeline
- Azure Static Web Apps deployment ready

## 🛠️ Tech Stack

**Frontend:** Blazor WebAssembly (.NET 8), C#, Tailwind CSS  
**Backend:** Azure Functions, Azure Blob Storage  
**Security:** Azure Key Vault for secrets management  
**DevOps:** GitHub Actions, Azure Static Web Apps  
**Services:** Brevo Email API, Application Insights

## 📚 Documentation

This project includes comprehensive documentation to help you understand the architecture, deploy to Azure, and maintain security:

- **[Component Architecture](COMPONENT_ARCHITECTURE.md)** - Detailed breakdown of the component-based design, including the WhoIAm page refactoring that reduced code by 90%. Learn about ProfileHeader, ProfileApproach, ProfileHighlights, ProjectCard, and ProjectFilter components, plus the ProjectService data layer.

- **[Deployment Guide](DEPLOYMENT_GUIDE.md)** - Complete step-by-step instructions for deploying to Azure, including Static Web Apps, Blob Storage, Key Vault, Azure Functions backend setup, and CORS configuration. Essential reading before deployment.

- **[Deployment Checklist](DEPLOYMENT_CHECKLIST.md)** - Quick reference checklist with all tasks, common issues, and success criteria. Perfect for tracking your deployment progress and troubleshooting.

- **[Security Alert](SECURITY_ALERT.md)** - Critical security information about Blazor WebAssembly limitations and proper secret management. **Read this first** to avoid exposing API keys and understand the required Azure Functions architecture.

## ⚡ Quick Start

```bash
# Clone the repository
git clone https://github.com/dariemcarlosdev/CloudZen.git

# Navigate to project
cd CloudZen

# Restore dependencies
dotnet restore

# Run locally
dotnet run
```

## 🔐 Security First

**Important:** Blazor WebAssembly runs entirely in the browser. Never store secrets in `appsettings.json`. Use Azure Functions backend with Key Vault for secure operations. See [SECURITY_ALERT.md](SECURITY_ALERT.md) for details.

## 🏗️ Architecture

```
Blazor WASM (Client) → Azure Functions (Backend) → Azure Services
                              ↓
                       Azure Key Vault (Secrets)
```

See [COMPONENT_ARCHITECTURE.md](COMPONENT_ARCHITECTURE.md) for detailed component breakdown and data flow.

## 📦 Project Structure

```
CloudZen/
├── Models/              # Data models (ProjectInfo, ProjectParticipant)
├── Services/            # Business logic (ProjectService, EmailService)
├── Shared/              # Reusable components
│   ├── Profile/        # Profile components
│   ├── Projects/       # Project display components
│   └── WhoIAm.razor    # Main portfolio page
├── wwwroot/            # Static assets and configuration
└── Program.cs          # Application entry point
```

## 🚀 Deployment

Ready to deploy? Follow these steps:

1. Read [SECURITY_ALERT.md](SECURITY_ALERT.md) - Critical security information
2. Follow [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md) - Complete setup instructions
3. Use [DEPLOYMENT_CHECKLIST.md](DEPLOYMENT_CHECKLIST.md) - Track your progress

The GitHub Actions workflow automatically deploys to Azure Static Web Apps on push to `master`.

## 📊 Project Highlights

- **90% code reduction** in main page through component refactoring
- **4 reusable components** with clear separation of concerns
- **Centralized data management** via ProjectService
- **Type-safe filtering** with EventCallback pattern
- **Modern UI/UX** with Tailwind CSS and responsive design

## 📄 License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## 👤 Author

**Dariem C. Macias**  
Principal Consultant, CloudZen Inc.  
[LinkedIn](https://www.linkedin.com/in/dariemcmacias) | [GitHub](https://github.com/dariemcarlosdev)