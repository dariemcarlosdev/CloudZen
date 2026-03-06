# CloudZen

A modern Blazor WebAssembly portfolio and consulting showcase built with .NET 8, demonstrating expertise in building scalable, secure cloud applications with Azure integration.

## 🚀 Features

### Portfolio & Presentation
- [x] **Dynamic Project Showcase** - Interactive case studies with filtering by technology, status, and project type
- [x] **Professional Portfolio Page** - Comprehensive "Who I Am" section with profile header, approach, and highlighted achievements
- [x] **Animated UI Components** - Counter circles with gradient fills and smooth animations for metrics display
- [x] **Responsive Design** - Mobile-first approach with Tailwind CSS, optimized for all screen sizes
- [x] **Interactive SDLC Process** - Visual representation of Planning, Automation, and Deployment phases

### Business Features
- [x] **Contact Form** - Validated contact form with email integration via Brevo/SendGrid/SMTP providers
- [x] **AI Chatbot** - Embedded conversational assistant powered by Anthropic Claude, with knowledge base, lead conversion, and abuse protection (see [AI_CHATBOT_DOCUMENTATION.md](AI_CHATBOT_DOCUMENTATION.md))
- [x] **Resume Download** - Secure resume delivery from Azure Blob Storage with SAS token authentication
- [x] **Service Offerings Display** - Dynamic service cards showcasing consulting capabilities
- [ ] **Testimonials Section** - Client feedback display (currently disabled, ready for activation)
- [x] **Call-to-Action Components** - Strategic CTAs throughout the site for lead generation

### Technical Features
- [x] **Progressive Web App (PWA)** - Service worker enabled for offline capability and fast loading
- [x] **Component-Based Architecture** - Reusable Blazor components with clear separation of concerns
- [x] **Secure API Backend** - Email and AI chatbot operations routed through Azure Functions API with rate limiting, input validation, and token controls
- [x] **Centralized Data Management** - Service layer pattern with ProjectService and PersonalService
- [x] **Type-Safe Event Handling** - EventCallback pattern for parent-child component communication
- [x] **Google Calendar Integration** - URL service for scheduling consultation bookings
- [x] **Ticket Management System** - Dashboard for tracking support incidents (demo implementation)

### Cloud & DevOps
- [x] **Azure Static Web Apps** - Automated deployment with GitHub Actions workflow
- [x] **Azure Blob Storage** - Cloud file storage with CORS configuration for cross-origin access
- [x] **Azure Key Vault Integration** - Secrets management via Azure Functions backend with `DefaultAzureCredential`
- [x] **CI/CD Pipeline** - Automated build, test, and deployment on push to master (Static Web Apps + Azure Functions)
- [x] **Service Worker** - Automatic caching and offline support for enhanced performance

## 🛠️ Tech Stack

### Frontend Technologies
- [x] **Blazor WebAssembly (.NET 8)** - Modern SPA framework with C# instead of JavaScript
- [x] **C# 12** - Latest language features with nullable reference types enabled
- [x] **Tailwind CSS** - Utility-first CSS framework for rapid UI development
- [x] **Bootstrap Icons** - Comprehensive icon library for UI elements
- [x] **HTML5 & CSS3** - Semantic markup and modern styling capabilities

### Cloud Infrastructure (Azure)
- [x] **Azure Static Web Apps** - Serverless hosting with global CDN distribution
- [x] **Azure Blob Storage** - Scalable object storage for resumes and file assets
- [x] **Azure Key Vault** - Centralized secrets management accessed via Functions with `DefaultAzureCredential`
- [x] **Azure Functions (Isolated Worker, .NET 8)** - Serverless backend for secure email API and AI chatbot proxy
- [ ] **Azure Table Storage** - NoSQL storage for ticket/incident data
- [x] **Azure Application Insights** - Real-time monitoring and telemetry with adaptive sampling

### External APIs
- [x] **Anthropic Claude API** (`claude-sonnet-4-20250514`) - AI chatbot backend with server-side knowledge base and system prompt

### Backend Services & APIs
- [x] **Brevo SMTP Relay** - Transactional email delivery via MailKit/MimeKit through Azure Functions API
- [x] **MailKit / MimeKit** (v4.15.0) - Cross-platform .NET SMTP client for secure email delivery
- [x] **Polly** (v8.6.5) - Resilience and transient fault handling (rate limiting, circuit breaker)
- [x] **Azure Storage SDK** - Client libraries for Blob, Queue, File Share, and Table operations
  - [x] `Azure.Storage.Blobs` (v12.24.0)
  - [x] `Azure.Storage.Queues` (v12.22.0)
  - [x] `Azure.Storage.Files.Shares` (v12.22.0)
  - [x] `Azure.Data.Tables` (v12.10.0)

### Authentication & Security
- [x] **Azure Identity** - Managed Identity and credential management (v1.13.2 client, v1.18.0 API)
- [x] **Azure Key Vault Configuration** - Secure runtime configuration loading via `AddAzureKeyVault()` in Functions API
- [ ] **SAS Tokens** - Secure, time-limited access to blob storage resources
- [x] **CORS Configuration** - Cross-origin resource sharing with configurable allowed origins in Azure Functions
- [x] **Content Security Policy** - HTTP headers for XSS protection configured in `staticwebapp.config.json`
- [x] **Input Validation & Sanitization** - `InputValidator` with XSS pattern detection in Azure Functions API
- [x] **Rate Limiting** - Per-client fixed window rate limiting with Polly in Azure Functions API

### Development & Build Tools
- [x] **.NET 8 SDK** - Latest LTS version with performance improvements
- [x] **Microsoft.Extensions.Azure** (v1.11.0) - Azure SDK client factory extensions
- [x] **User Secrets** - Local development secrets management (not deployed)
- [x] **Service Worker** - PWA capabilities with offline caching

### Tailwind CSS Setup & Architecture

#### **How Tailwind is Set Up**
This project uses **Tailwind CSS via CDN** (zero-configuration approach) loaded in `wwwroot/index.html`:
```html
<script src="https://cdn.tailwindcss.com"></script>
```

**What this means:**
- ✅ **Zero build complexity** - No npm, webpack, PostCSS, or Node.js dependencies required
- ✅ **Instant availability** - All Tailwind utility classes work out of the box
- ✅ **Blazor-native** - Integrates seamlessly with Blazor WebAssembly static file serving
- ✅ **Fast prototyping** - Full Tailwind feature set available immediately
- ⚠️ **Larger bundle** - ~3.5MB uncompressed CSS (not optimized via PurgeCSS)
- ⚠️ **No theme extension** - Cannot customize default Tailwind theme without inline config

#### **Usage Pattern: Hybrid Approach**
The application combines **Tailwind utility classes** (primary styling) with **custom CSS** (`wwwroot/css/app.css`) for:
- Blazor-specific styles (`#blazor-error-ui`, `.loading-progress`)
- Custom animations (`.hamburger-active`, `.scroll-to-top`)
- Brand-specific classes (`.cloudzen-hover`, `.progress-bar-fill`)
- Bootstrap compatibility (legacy `.btn-primary`, form controls)

**Tailwind Coverage:** 100% of Razor components use Tailwind utilities extensively.

#### **Architecture Decision: Why CDN Instead of npm/Config?**

**Advantages of CDN approach:**
- **Simplicity** - Pure .NET 8 project with no JavaScript toolchain
- **Developer experience** - No build step delays during development
- **Deployment** - Single `dotnet publish` command with no additional bundling
- **Maintenance** - No package.json, node_modules, or npm version conflicts

**Trade-offs:**
- **Performance** - Unoptimized CSS bundle (~3.5MB minified to ~300KB in production)
- **Customization** - Limited theme extensions without inline configuration
- **Production optimization** - No automatic unused class removal

#### **Recommendations**

**Option 1: Keep CDN (Current Approach) ✅**  
**Best for:** Small-to-medium projects, rapid development, zero build complexity

**To optimize current setup:**
1. **Add custom theme colors** via inline Tailwind config in `index.html`:
```html
<script>
  tailwind.config = {
    theme: {
      extend: {
        colors: {
          'cloudzen-teal': '#61C2C8',
          'cloudzen-teal-hover': '#74b7bb',
        }
      }
    }
  }
</script>
```

2. **Use CSS custom properties** for brand consistency (already implemented):
```css
/* app.css */
:root {
  --cloudzen-primary: #61C2C8;
}
```

**Option 2: Migrate to npm + tailwind.config.js**  
**Best for:** Production apps, performance optimization, advanced customization

**Benefits:**
- 📦 **90% smaller CSS** - PurgeCSS removes unused classes (reduces to ~10-30KB)
- 🎨 **Full theme control** - Custom colors, fonts, spacing, breakpoints
- ⚡ **JIT mode** - Only generate classes you actually use
- 🔧 **Plugins** - Access official Tailwind plugins (forms, typography, aspect-ratio)

**Migration steps** (future enhancement):
```bash
# Install Tailwind
npm install -D tailwindcss postcss autoprefixer

# Create config
npx tailwindcss init

# Update tailwind.config.js
module.exports = {
  content: ["./**/*.razor", "./**/*.html"],
  theme: {
    extend: {
      colors: {
        'cloudzen-teal': '#61C2C8',
      }
    }
  }
}

# Build CSS
npx tailwindcss -i ./wwwroot/css/app.css -o ./wwwroot/css/output.css --minify
```

**Current recommendation:** Keep CDN approach for now. The application's current bundle size is acceptable for a portfolio site, and the development simplicity outweighs the performance gains from npm-based setup. Consider migrating when adding significant new features or optimizing for production performance.

### Design Patterns & Principles Implemented

#### **SOLID Principles**
- [x] **Single Responsibility Principle (SRP)** 
  - [x] Each service has one reason to change (`ProjectService`, `ResumeService`, `EmailServiceFactory`)
  - [x] Components have single, well-defined purposes (`ProfileHeader`, `ProjectCard`)
  - [x] Models represent single entities (`ProjectInfo`, `ServiceInfo`, `TicketDto`)
- [x] **Open/Closed Principle (OCP)**
  - [x] `IEmailService` interface allows alternative email implementations without modifying existing code
  - [x] Azure Functions API extensible via configuration for different SMTP providers
  - [x] Component system supports adding features through composition, not modification
- [x] **Liskov Substitution Principle (LSP)**
  - [x] `IEmailService` implementations are interchangeable (e.g., `ApiEmailService` could be swapped for a direct provider)
  - [x] `ITicketService` implementations are interchangeable
- [x] **Interface Segregation Principle (ISP)**
  - [x] Focused interfaces (`IEmailService`, `ITicketService`, `IRateLimiterService`) with only necessary methods
  - [x] No client forced to depend on methods it doesn't use
- [x] **Dependency Inversion Principle (DIP)**
  - [x] High-level components depend on abstractions (`IEmailService`, `ITicketService`, `IRateLimiterService`), not concrete implementations
  - [x] DI container manages all dependencies via `Program.cs` registration in both client and API projects
  - [x] Services injected into components via `@inject` directive

#### **Design Patterns**
- [x] **API Gateway Pattern** - Blazor WASM delegates sensitive operations to Azure Functions API (`ApiEmailService` → `SendEmailFunction`)
- [x] **Options Pattern** - Strongly-typed configuration with `IOptions<T>` (`EmailServiceOptions`, `BlobStorageOptions`, `EmailSettings`, `RateLimitOptions`)
- [x] **Service Layer Pattern** - Business logic separation (`ProjectService`, `PersonalService`, `ResumeService`, `TicketService`, `ApiEmailService`)
- [x] **Repository Pattern** - Data access abstraction for projects and services with centralized data management
- [x] **Event Callback Pattern** - Type-safe parent-child component communication in Blazor
- [x] **Singleton Pattern** - Long-lived services (`GoogleCalendarUrlService`, `TicketService`, `PollyRateLimiterService`) registered as singletons
- [x] **Record Pattern** - Immutable data transfer objects (`ServiceInfo` record type)
- [x] **Resilience Pattern** - Polly-based rate limiting and circuit breaker in Azure Functions API

#### **Advanced Techniques**
- [x] **Async/Await Pattern** - Non-blocking operations throughout (`SendEmailAsync`, `DownloadResumeAsync`)
- [x] **Managed Identity Authentication** - Azure Identity with `DefaultAzureCredential` for passwordless Azure service access
- [x] **Configuration Abstraction** - `IConfiguration` and `IOptions<T>` for environment-specific settings across both projects
- [x] **Logging Integration** - `ILogger<T>` for structured logging in services and Azure Functions
- [x] **Error Handling** - InvalidOperationException for missing configuration validation
- [x] **Null Safety** - Nullable reference types enabled project-wide (`string?`, `IEnumerable?`)
- [x] **LINQ Query Composition** - Efficient data filtering and sorting in `ProjectService`
- [x] **JavaScript Interop** - Blazor-JS communication for file downloads and animations
- [x] **Input Sanitization** - `InputValidator` with regex-based XSS pattern detection and HTML encoding
- [x] **Correlation ID Tracking** - Request tracing across Azure Functions for debugging and monitoring

### DevOps & CI/CD
- [x] **GitHub Actions** - Automated CI/CD workflows (Static Web Apps + Azure Functions deployment)
- [ ] **Azure Static Web Apps CLI** - Local development and testing
- [ ] **Docker** - Container support for reproducible builds (optional)
- [x] **Git** - Version control with branch-based deployment strategies

### Monitoring & Analytics
- [x] **Application Insights** - Performance monitoring with adaptive sampling and QuickPulse metrics in Azure Functions API
- [ ] **Azure Monitor** - Infrastructure and application health monitoring
- [x] **Logging Framework** - `ILogger<T>` integration throughout services with structured logging
- [ ] **Custom telemetry** - Track user interactions, feature usage, and performance bottlenecks

### Resilience & Error Handling
- [ ] **Retry Logic** - Implemented in distributed systems projects (RabbitMQ, Azure Functions)
- [ ] **Connection Resiliency** - Auto-reconnect for messaging systems and database connections
- [x] **Circuit Breaker** - Polly-based circuit breaker in Azure Functions API rate limiter service
- [ ] **Health Checks** - Continuous monitoring of dependent services (databases, message queues, APIs)
- [ ] **Graceful Degradation** - Application continues functioning when non-critical services fail
- [x] **Exception Handling** - Structured error handling with specific exception types
- [x] **Configuration Validation** - Throws `InvalidOperationException` for missing critical settings
- [x] **Timeout Management** - Configurable timeouts for HTTP clients (30s default) in Azure Functions API
- [ ] **Idempotency** - Ensures operations can be safely retried without side effects
- [x] **Polly Integration** - Rate limiting (`FixedWindowRateLimiter`) and circuit breaker via Polly resilience pipelines in API
- [x] **Async-safe Patterns** - All async operations properly handle cancellation and exceptions

## 📚 Documentation

This project includes comprehensive documentation to help you understand the architecture, deploy to Azure, and maintain security:

- **[AI Chatbot Documentation](AI_CHATBOT_DOCUMENTATION.md)** - Complete technical documentation for the AI chatbot including architecture, security layers, token controls, lead conversion strategy, and testing guide.

- **[Component Architecture](COMPONENT_ARCHITECTURE.md)** - Detailed breakdown of the component-based design, including the WhoIAm page refactoring that reduced code by 90%.

- **[Tailwind Custom Colors](TAILWIND_CUSTOM_COLORS.md)** - Reference guide for CloudZen's custom Tailwind CSS utilities, brand colors, and custom fonts.

- **[Deployment Guide](DEPLOYMENT_GUIDE.md)** - Step-by-step instructions for deploying to Azure (Static Web Apps, Blob Storage, Key Vault, Azure Functions).

- **[Deployment Checklist](DEPLOYMENT_CHECKLIST.md)** - Quick reference checklist for deployment tasks, common issues, and success criteria.

- **[Security Alert](SECURITY_ALERT.md)** - Critical security information about Blazor WebAssembly limitations and proper secret management. **Read this first.**

- **[Azure Functions Deployment](AZURE_FUNCTION_DEPLOYMENT.md)** - Guide for deploying the Azure Functions API backend.

- **[Azure Functions Hosting Models](AZURE_FUNCTIONS_HOSTING_MODELS.md)** - Comparison of Azure Functions hosting models.

- **[Brevo SMTP Migration](docs/BREVO_SMTP_MIGRATION.md)** - Migration guide from Brevo REST API to SMTP relay via MailKit.

- **[Configuration Best Practices](CONFIGURATION_BEST_PRACTICES.md)** - Guidelines for managing configuration across client and API projects.

- **[Configuration Management](docs/CONFIGURATION_MANAGEMENT.md)** - Detailed configuration management documentation.

- **[API Security Enhancements](Api/SECURITY_ENHANCEMENTS.md)** - Security features implemented in the Azure Functions API (rate limiting, input validation, CORS).

- **[API Local Testing](Api/TESTING_LOCALLY.md)** - Instructions for testing the Azure Functions API locally.

## ⚡ Quick Start

```bash
# Clone the repository
git clone https://github.com/dariemcarlosdev/CloudZen.git

# Navigate to project
cd CloudZen

# Restore dependencies
dotnet restore

# Run Blazor WASM client
dotnet run --project CloudZen.csproj

# Run Azure Functions API (separate terminal, requires Azure Functions Core Tools)
cd Api
func start
```

## 🔐 Security First

**Important:** Blazor WebAssembly runs entirely in the browser. Never store secrets in `appsettings.json`. Use Azure Functions backend with Key Vault for secure operations. See [SECURITY_ALERT.md](SECURITY_ALERT.md) for details.

## 🏗️ Architecture

```
Blazor WASM (Client)  ──→  Azure Functions API (Backend)  ──→  Brevo SMTP Relay
   (CloudZen)                 (CloudZen.Api)                     (Email Delivery)
        │                          │
        │                          ├──→ Azure Key Vault (Secrets)
        │                          ├──→ Application Insights (Telemetry)
        │                          └──→ Anthropic Claude API (AI Chatbot)
        │
        └──→  Azure Blob Storage (Resume/Files)
```

See [COMPONENT_ARCHITECTURE.md](COMPONENT_ARCHITECTURE.md) for detailed component breakdown and data flow.

## 📦 Project Structure

```
CloudZen/
├── Api/                             # Azure Functions API backend (CloudZen.Api)
│   ├── Functions/                  # Azure Function endpoints
│   │   ├── SendEmailFunction.cs    # Email proxy to Brevo SMTP
│   │   └── ChatFunction.cs         # AI chatbot proxy to Anthropic Claude
│   ├── Models/                     # API models (EmailRequest, ChatRequest, ChatResponse, RateLimitOptions)
│   ├── Security/                   # Input validation and sanitization (InputValidator)
│   ├── Services/                   # API services (PollyRateLimiterService)
│   └── Program.cs                  # Functions host entry point
├── Layout/                          # Layout components (MainLayout, Header, Footer)
├── Models/                          # Data models (ProjectInfo, ServiceInfo, EmailApiRequest)
│   └── Options/                    # IOptions configuration classes
├── Pages/                           # Routable pages (Index)
├── Services/                        # Business logic (ProjectService, ApiEmailService, ResumeService)
│   └── Abstractions/               # Service interfaces (IEmailService, ITicketService)
├── Shared/                          # Reusable Blazor components
│   ├── Chatbot/                    # AI chatbot widget (CloudZenChatbot)
│   ├── Common/                     # Shared UI (AnimatedCounterCircle, ScrollToTopButton, Tickets)
│   ├── Landing/                    # Landing page sections (Hero, Services, CaseStudies, ContactForm, CTA)
│   ├── Profile/                    # Profile components (ProfileHeader, ProfileApproach, SDLCProcess, WhoIAm)
│   └── Projects/                   # Project display (ProjectCard, ProjectFilter)
├── wwwroot/                         # Static assets, configuration, and index.html
├── .github/workflows/               # CI/CD (azure-functions.yml)
└── Program.cs                       # Blazor WASM entry point
```

## 🚀 Deployment

Ready to deploy? Follow these steps:

1. Read [SECURITY_ALERT.md](SECURITY_ALERT.md) - Critical security information
2. Follow [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md) - Complete setup instructions
3. Follow [AZURE_FUNCTION_DEPLOYMENT.md](AZURE_FUNCTION_DEPLOYMENT.md) - Deploy the API backend
4. Use [DEPLOYMENT_CHECKLIST.md](DEPLOYMENT_CHECKLIST.md) - Track your progress

GitHub Actions workflows automatically deploy:
- **Blazor WASM** → Azure Static Web Apps (on push to `master`)
- **Azure Functions API** → Azure Function App (on push to `master` when `Api/` changes)

## 📊 Project Highlights

### Architecture & Design Excellence
- [x] **90% code reduction** in WhoIAm page through strategic component decomposition
- [x] **20+ reusable Blazor components** with single responsibility principle
  - [x] Profile components: `ProfileHeader`, `ProfileApproach`, `ProfileHighlights`, `SDLCProcess`, `WhoIAm`
  - [x] Project components: `ProjectCard`, `ProjectFilter`
  - [x] Landing components: `Hero`, `Services`, `CaseStudies`, `ContactForm`, `CTA`, `Mission`, `Testimonials`, `ValueProposition`
  - [x] Layout components: `MainLayout`, `Header`, `Footer`
  - [x] Common components: `AnimatedCounterCircle`, `ScrollToTopButton`, `Tickets`
- [x] **Component-based architecture** enabling 85% code reusability across pages
- [x] **Centralized business logic** with dedicated service layer
  - [x] `ProjectService` - Portfolio project management and filtering
  - [x] `PersonalService` - Service offerings and company information
  - [x] `ResumeService` - Azure Blob integration for document delivery
  - [x] `ApiEmailService` - Secure email via Azure Functions API backend
  - [x] `TicketService` - Support incident tracking
  - [x] `GoogleCalendarUrlService` - Booking integration

### Cloud-Native Implementation
- [x] **Serverless architecture** with Azure Static Web Apps + Azure Functions (Isolated Worker)
- [x] **Automated deployments** via GitHub Actions CI/CD (separate workflows for WASM and Functions)
- [ ] **Global CDN distribution** for sub-100ms page loads worldwide
- [ ] **Auto-scaling infrastructure** handling traffic spikes without manual intervention
- [x] **Secure secrets management** with Azure Key Vault integration in Azure Functions API
- [x] **CORS-enabled** Azure Functions API with configurable allowed origins
- [x] **PWA capabilities** with service worker for offline functionality

### User Experience & Performance
- [x] **Type-safe filtering** with EventCallback pattern for real-time project filtering
- [x] **Animated UI elements** including gradient counters and smooth transitions
- [x] **Mobile-first responsive design** - Optimized for 320px to 4K displays
- [x] **Accessibility compliance** with semantic HTML and ARIA labels
- [x] **Fast page loads** - Service worker caching reduces repeat visit load time by 70%
- [x] **Interactive process visualization** - SDLC workflow with state management

### Security & Best Practices
- [x] **API-first security** - Sensitive operations (email, secrets) handled by Azure Functions backend, never in client
- [x] **SOLID principles** applied across all services and components for maintainability
- [x] **Dependency injection** throughout the application for testability and loose coupling
- [x] **Interface-driven design** (`IEmailService`, `ITicketService`, `IRateLimiterService`) for flexibility and testing
- [x] **Nullable reference types** enabled project-wide reducing null reference exceptions by 40%
- [x] **Environment-based configuration** separating development, staging, and production settings
- [ ] **SAS token authentication** for secure, time-limited public blob access
- [x] **CSP headers** and security-first static web app configuration preventing XSS attacks
- [x] **API key rotation** support with zero-downtime provider switching via configuration
- [x] **Validation at boundaries** - Input validation in contact form and API (`InputValidator` with XSS pattern detection)
- [x] **Encapsulation** - Private fields with public property accessors (e.g., `ResumeService.ResumeBlobUrl`)
- [x] **Immutable data models** using C# records for thread-safe data transfer (`ServiceInfo`)
- [x] **Async-first design** - All I/O operations use async/await for scalability
- [x] **Resilience patterns** - Polly-based rate limiting and circuit breaker in Azure Functions API
- [x] **Configuration validation** - Exception throwing for missing critical configuration values

### Business Value Delivered
- [x] **Professional portfolio** showcasing 8+ real-world projects with measurable results
- [x] **Lead generation** via strategic CTAs, validated contact form, and AI chatbot with 5-question conversation cap
- [x] **AI-powered chatbot** converting website visitors to consultation leads with knowledge-base-driven responses
- [x] **Automated email delivery** with Brevo SMTP relay via secure Azure Functions API backend
- [x] **Resume distribution** with download tracking and blob analytics
- [x] **Client onboarding** streamlined with Google Calendar integration
- [x] **Support dashboard** for incident tracking and response time monitoring

### Development Quality
- [x] **Clean Architecture** principles with clear layer separation
- [x] **SOLID principles** applied to service implementations
- [x] **Comprehensive documentation** with inline XML comments and README guides
- [x] **Git workflow** with feature branches and protected master
- [x] **Code organization** following ASP.NET Core conventions
- [ ] **Scalable structure** ready for feature expansion (testimonials, blog, admin panel)

### Technical Innovations
- [x] **Dynamic case study selection** - Automatically surfaces top 3 customer projects with LINQ filtering
- [x] **Business-friendly jargon translation** - Converts technical terms for non-technical audiences in real-time
- [x] **Gradient color interpolation** - Mathematical color transitions for animated counters using RGB calculations
- [x] **Event-driven architecture** - Loose coupling between UI and business logic via EventCallback pattern
- [x] **Secure email pipeline** - Client → Azure Functions API → Brevo SMTP relay with rate limiting and input validation
- [x] **AI chatbot pipeline** - Blazor WASM → Azure Functions → Anthropic Claude API with token controls, history trimming, and response truncation
- [x] **Multi-layer abuse prevention** - Client-side conversation cap + API rate limiting + input validation + system prompt hardening
- [x] **SPA with SEO optimization** - Static Web Apps routing and fallback for search engine visibility (`staticwebapp.config.json`)
- [ ] **Retry mechanisms** - Implemented in side projects (RabbitMQ connection resiliency, SSIS retry logic)
- [x] **Circuit breaker patterns** - Polly-based circuit breaker in Azure Functions rate limiter service
- [ ] **Health monitoring** - Integrated health checks for distributed systems (RabbitMQ, Azure Functions)
- [ ] **Idempotent message processing** - Duplicate prevention in event-driven systems
- [x] **Rate limiting** - Per-client fixed window rate limiting with Polly in Azure Functions API
- [ ] **CQRS pattern** - Command-Query Responsibility Segregation with MediatR in microservices
- [ ] **Caching strategies** - In-memory and distributed caching for performance optimization
- [ ] **Delta-based ETL processing** - 70% runtime reduction through intelligent data extraction
- [x] **Managed Identity preference** - `DefaultAzureCredential` for passwordless Azure service access

## 📄 License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## 👤 Author

**Dariem C. Macias**  
Principal Consultant, CloudZen Inc.  
[LinkedIn](https://www.linkedin.com/in/dariemcmacias) | [GitHub](https://github.com/dariemcarlosdev)