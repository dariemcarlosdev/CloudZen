using CloudZen.Models;

namespace CloudZen.Services;

/// <summary>
/// Service for managing and retrieving project portfolio data.
/// This service centralizes project data management and can be extended to load from external sources (API, database, JSON files, etc.).
/// </summary>
public class ProjectService
{
    /// <summary>
    /// Retrieves all projects in the portfolio, sorted by status (Completed, In Progress, Planning).
    /// </summary>
    /// <returns>A list of ProjectInfo objects representing the portfolio.</returns>
    public List<ProjectInfo> GetAllProjects()
    {
        var projects = GetProjectsData();
        
        // Sort by status order
        var statusOrder = new List<string> { "Completed", "In Progress", "Planning" };
        return projects.OrderBy(p => statusOrder.IndexOf(p.Status)).ToList();
    }

    /// <summary>
    /// Retrieves projects filtered by status.
    /// </summary>
    /// <param name="status">The status to filter by (e.g., "Completed", "In Progress", "Planning").</param>
    /// <returns>A list of projects matching the specified status.</returns>
    public List<ProjectInfo> GetProjectsByStatus(string status)
    {
        return GetProjectsData().Where(p => p.Status == status).ToList();
    }

    /// <summary>
    /// Retrieves projects filtered by type (Side Project, Customer work, etc.).
    /// </summary>
    /// <param name="projectType">The project type to filter by.</param>
    /// <returns>A list of projects matching the specified type.</returns>
    public List<ProjectInfo> GetProjectsByType(string projectType)
    {
        return GetProjectsData().Where(p => p.ProjectType == projectType).ToList();
    }

    /// <summary>
    /// Gets featured/highlighted projects (typically completed projects with high impact).
    /// </summary>
    /// <returns>A list of featured projects.</returns>
    public List<ProjectInfo> GetFeaturedProjects()
    {
        return GetProjectsData()
            .Where(p => p.Status == "Completed" && p.Progress == 100)
            .Take(3)
            .ToList();
    }

    /// <summary>
    /// Central method containing all project data.
    /// TODO: In future, this can be replaced with loading from:
    /// - JSON file (wwwroot/data/projects.json)
    /// - Database (via Entity Framework)
    /// - External API
    /// </summary>
    /// <returns>Complete list of projects.</returns>
    private List<ProjectInfo> GetProjectsData()
    {
        return new List<ProjectInfo>
        {
               //      new ProjectInfo
   //      {
			// Name = "AI-Powered Document Processing System for Financial Services",
   //          Status = "Completed",
			// Description = "Developed an AI-driven document processing system using .NET 8, Azure OpenAI, and Cognitive Services to automate data extraction and validation from financial documents, significantly reducing manual effort and improving accuracy.",
   //      },
  //       new ProjectInfo
  //               {
  //           Name = "Inventory Management System with Azure Functions and OpenAI Integration",
  //           Status = "Completed",
  //           Description = "Built a cloud-native inventory management system using .NET 8, Azure Functions, and Azure OpenAI to automate stock tracking, order processing, and predictive restocking, enhancing operational efficiency and reducing stockouts.",
  //           TechStack = new[] { ".NET 8.0 SDK", "C#", "Azure Functions", "Azure OpenAI", "Azure Blob Storage", "Azure SQL Database", "Docker", "CI/CD Workflow", "Serverless Architecture" },
  //           Progress = 100,
  //           Results = new List<string>
  //           {
  //               "Automated inventory tracking and order processing using Azure Functions.",
  //               "Integrated Azure OpenAI for predictive restocking recommendations.",
  //               "Containerized services with Docker for consistent deployment across environments.",
  //               "Designed RESTful APIs for seamless integration with other systems.",
  //               "Implemented robust error handling and retry logic in serverless functions.",
  //               "Established CI/CD pipelines to automate build, test, and deployment processes."
  //           },
  //           Participants = new[]
  //           {
  //               new ProjectParticipant { Name = "Dariem C. Macias", ImageUrl = "/images/dariem-avatar.png" },
  //           },
  //           Role = "Principal Consultant / Solution Architect",
  //           Challenges = new List<string>
  //           {
  //               "Designing a scalable serverless architecture using Azure Functions.",
  //               "Integrating AI-driven predictive analytics with Azure OpenAI.",
  //               "Implementing reliable error handling and retry logic in serverless functions.",
  //               "Establishing CI/CD pipelines for automated deployments."
  //           }
		// },
  //       new ProjectInfo
  //       {
  //           Name = "AI-Enhanced Customer Support Chatbot for E-Commerce",
  //           Status = "Completed",
  //           Description = "Implemented an AI-powered chatbot using .NET 8, Azure OpenAI, and Cognitive Services to provide 24/7 customer support for an e-commerce platform, improving response times and customer satisfaction.",
  //           TechStack = new[] { ".NET 8.0 SDK", "C#", "Azure Bot Service", "Azure OpenAI", "Azure Cognitive Services", "Azure SQL Database", "Docker", "CI/CD Workflow" },
  //           Progress = 100,
  //           Results = new List<string>
  //           {
  //               "Deployed an AI-powered chatbot to handle common customer inquiries.",
  //               "Integrated Azure OpenAI for natural language understanding and response generation.",
  //               "Containerized services with Docker for consistent deployment across environments.",
  //               "Designed RESTful APIs for seamless integration with the e-commerce platform.",
  //               "Implemented robust error handling and fallback mechanisms.",
  //               "Established CI/CD pipelines to automate build, test, and deployment processes."
  //           },
  //           Participants = new[]
  //           {
  //               new ProjectParticipant { Name = "Dariem C. Macias", ImageUrl = "/images/dariem-avatar.png" },
  //           },
  //           Role = "Principal Consultant / Solution Architect",
  //           Challenges = new List<string>
  //           {
  //               "Designing an AI-driven chatbot architecture using Azure Bot Service.",
  //               "Integrating natural language processing with Azure OpenAI.",
  //               "Implementing reliable error handling and fallback mechanisms.",
  //               "Establishing CI/CD pipelines for automated deployments."
  //           }
		// },
  //       new ProjectInfo {
  //           Name = "AI-Driven Sales Forecasting System for Retail",
  //           Status = "Completed",
  //           Description = "Developed an AI-powered sales forecasting system using .NET 8, Azure OpenAI, and Cognitive Services to analyze historical sales data and predict future trends, enabling data-driven decision-making for inventory management and marketing strategies.",
  //           TechStack = new[] { ".NET 8.0 SDK", "C#", "Azure Functions", "Azure OpenAI", "Azure Cognitive Services", "Azure SQL Database", "Power BI", "Docker", "CI/CD Workflow" },
  //           Progress = 100,
  //           Results = new List<string>
  //           {
  //               "Implemented AI-driven sales forecasting using Azure OpenAI.",
  //               "Automated data processing and analysis with Azure Functions.",
  //               "Integrated Power BI for interactive sales dashboards and reports.",
  //               "Containerized services with Docker for consistent deployment across environments.",
  //               "Designed RESTful APIs for seamless integration with other systems.",
  //               "Established CI/CD pipelines to automate build, test, and deployment processes."
  //           },
  //           Participants = new[]
  //           {
  //               new ProjectParticipant { Name = "Dariem C. Macias", ImageUrl = "/images/dariem-avatar.png" },
  //           },
  //           Role = "Principal Consultant / Solution Architect",
  //           Challenges = new List<string>
  //           {
  //               "Designing a scalable AI-driven forecasting architecture using Azure services.",
  //               "Integrating predictive analytics with Azure OpenAI.",
  //               "Implementing automated data processing workflows.",
  //               "Establishing CI/CD pipelines for automated deployments."
  //           }
		// },

            new ProjectInfo
            {
                Name = "Secure Clean Api With JWT and Role-Based Access",
                Status = "Completed",
                Description = " Developed a a Blazor Server application built for secure, scalable, and performant deployment on Azure. It demonstrates best practices in architecture, security, configuration management, API design, caching, error handling, and CI/CD automation. The solution integrates third-party APIs and leverages modern .NET 8 features.",
                TechStack = new[] { ".NET 8.0 SDK", "C#", "Blazor Server", "ASP.NET Core Web API", "JWT Authentication", "Role-Based Access Control", "Clean Architecture","Polly Resilience", "Azure App Services", "Azure SQL Database", "Docker", "CI/CD Workflow", "CQRS + MediatR" },
                Progress = 100,
                Results = new List<string>
                {
                    "Implemented secure JWT authentication with role-based access control.",
                    "Applied Clean Architecture + DDD principles for maintainable codebase.",
                    "Designed RESTful APIs with proper versioning and documentation.",
                    "CQRS Pattern: Segregated read and write operations using MediatR for improved scalability and maintainability.",
                    "Caching Strategies: Implemented in-memory and distributed caching to enhance application performance and reduce database load.",
                    "Api Integration: Seamlessly integrated third-party APIs to extend application functionality.",
                    "API Rate Limiting: Applied rate limiting policies to protect APIs from abuse and ensure fair usage.",
                    "Integrated third-party APIs for extended functionality.",
                    "Optimized performance with caching strategies and async programming.",
                    "Containerized application with Docker for consistent deployment.",
                    "Established CI/CD pipelines to automate build, test, and deployment processes."
                },
                Participants = new[]
                {
                    new ProjectParticipant { Name = "Dariem C. Macias", ImageUrl = "/images/dariem-avatar.png" },
                },
                Role = "Principal Consultant / Solution Architect",
                Challenges = new List<string>
                {
                    "Designing a secure authentication and authorization system using JWT and role-based access control.",
                    "Implementing Clean Architecture and DDD principles for maintainability.",
                    "Applying CQRS pattern with MediatR for scalability.",
                    "Implementing caching strategies to optimize performance.",
                    "Establishing CI/CD pipelines for automated deployments.",
                    "Integrating third-party APIs seamlessly.",
                    "Implementing API rate limiting to protect against abuse.",
                    "Domain-Driven Design (DDD): Structured the application around core business domains to enhance clarity and maintainability.",
                    "Clean Architecture: Segregated application layers to promote separation of concerns and facilitate testing.",
                    "Architecting for deployment on Azure App Services."
                },
                GithubUrl = "https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate",
                ProjectType = "Side Project"
            },
            new ProjectInfo
            {
                Name = "Order Processing Microservice with RabbitMQ Pub/Sub",
                Status = "Completed",
                Description = "Production-ready .NET 8 microservice demonstrating event-driven architecture with RabbitMQ Pub/Sub, Docker orchestration, clean architecture, and distributed messaging patterns.",
                TechStack = new[] { ".NET 8.0 SDK", "C#", "Docker", "RabbitMQ", "ASP.NET Core Web API", "Clean Architecture", "CI/CD Workflow", "Microservices", "Event-Driven", "Pub/Sub Pattern" },
                Progress = 100,
                Results = new List<string>
                {
                    "Implemented robust order processing microservice using RabbitMQ Pub/Sub for decoupled communication.",
                    "Containerized services with Docker for consistent deployment across environments.",
                    "Background workers process orders asynchronously, improving scalability and responsiveness.",
                    "Designed RESTful APIs for seamless integration with other services.",
                    "Connection Resiliency: Implemented retry logic and error handling for RabbitMQ connections to ensure reliable message delivery.",
                    "Designed idempotent message processing to prevent duplicate order handling.",
                    "Health Monitoring: Integrated health checks for RabbitMQ and microservices to ensure system reliability.",
                    "Applied Clean Architecture principles for maintainable, testable codebase.",
                    "Established CI/CD pipelines to automate build, test, and deployment processes."
                },
                Participants = new[]
                {
                    new ProjectParticipant { Name = "Dariem C. Macias", ImageUrl = "/images/dariem-avatar.png" },
                },
                Role = "Principal Consultant / Solution Architect",
                Challenges = new List<string>
                {
                    "Designing a scalable, event-driven architecture using RabbitMQ Pub/Sub.",
                    "Ensuring reliable message delivery with connection resiliency and idempotent processing.",
                    "Implementing health monitoring for system reliability.",
                    "Establishing CI/CD pipelines for automated deployments.",
                    "Containerizing microservices with Docker for consistent environments.",
                    "Applying Clean Architecture principles for maintainability."
                },
                GithubUrl = "https://github.com/dariemcarlosdev/OrderProcessing-RabbitMQ-Microservices",
                ProjectType = "Side Project"
            },
            new ProjectInfo
            {
                Name = "WPBT Modernization:Scalable Assessment Services for MDCPS",
                Status = "Completed",
                Description = "Modernized WPBT Assessment Services platform for MDCPS by migrating from legacy ASP.NET Web Forms to modular ASP.NET Core architecture—enhanced scalability, security, and user experience while accelerating assessment cycles by 50%",
                TechStack = new[] { "LinQ", "T-SQL Server", "EF", "BootStrap", "ASP.NET Web-Form", ".NET Core",".NET 8", "C#" },
                Progress = 100,
                Results = new List<string>
                {
                    "Streamlined assessment cycle cutting turnaround times by roughly 50%.",
                    "Delivered a responsive, modular interface for principals and administrators.",
                    "Enhanced traceability and precision in assessment records, minimizing errors and boosting confidence in reporting.",
                    "Supported increasing teachers and program volumes without performance degradation, ensuring long-term viability.",
                    "Delivered a responsive, intuitive interface for Principals and administrators, increasing productivity and satisfaction.",
                    "Overcame rigid architecture and outdated UI that hindered scalability, performance, and user engagement"
                },
                Participants = new[]
                {
                    new ProjectParticipant { Name = "Dariem C. Macias", ImageUrl = "/images/dariem-avatar.png" },
                },
                Role = "Principal Consultant / Lead Developer / Solution Architect",
                Challenges = new List<string>
                {
                    "Migrating complex legacy business logic from Web Forms to ASP.NET Core.",
                    "Ensuring data integrity and traceability during platform transition.",
                    "Redesigning UI/UX for modern accessibility and scalability."
                },
                ProjectType = "Customer: MDCPS"
            },
            new ProjectInfo
            {
                Name = "Data-Driven ETL Optimization for SAP Maintenance Master Data",
                Status = "Completed",
                Description = "Engineered delta-based SSIS ETL pipeline for SAP Maintenance Data, replacing full daily loads with ABAP-driven delta extraction and lookup-based deduplication.",
                TechStack = new[] { "SSIS ETL pipelines", "T-SQL Server", "EF", "LinQ", "Job Automation/Orchestration"},
                Progress = 100,
                Results = new List<string>
                {
                    "Streamlined SAP Data Ingestion by 70% Runtime Reduction through Delta Processing.",
                    "Resolved performance bottlenecks in the downstream Data Warehouse (DW)",
                    "Scales-Out efficiently with large datasets",
                    "Modular SSIS Architecture for Scalable, Audit-Compliant SAP ETL.",
                    "Reinforced best practices in recursive ETL design and stakeholder-aligned architecture"
                },
                Participants = new[]
                {
                    new ProjectParticipant { Name = "Dariem C. Macias", ImageUrl = "/images/dariem-avatar.png" },
                },
                Role = "Lead ETL Engineer / Solution Architect",
                Challenges = new List<string>
                {
                    "Designing delta-based extraction logic for SAP data.",
                    "Optimizing ETL for large-scale, high-volume data loads.",
                    "Ensuring audit compliance and traceability in ETL workflows."
                },
                ProjectType = "Customer: MDCPS"
            },
            new ProjectInfo
            {
                Name = "Smart Menu Optimizer - Cloud-native and AI-Powered app.",
                Status = "In Progress",
                Description = "Smart Menu Optimizer is a cloud-native and AI-powered recommendation engine designed to help restaurants owners make data-driven decisions about their menus maximizing profitability and reduce waste.",
                TechStack = new[] { 
                    ".NET 8.0/9.0 SDK",
                    "C#",
                    "Docker",
                    "Azure Functions",
                    "ML.NET models",
                    "AI NLP",
                    "Azure Cognitive Services",
                    "Azure OpenAI",
                    "Power BI dashboards",
                    "ASP.NET Core Web API",
                    "Blazor Server",
                    "PostgreSQL",
                    "Azure Redis",
                    "Azure Blob Storage",
                    "Multi-Tenant SaaS Architecture",
                    "GitHub Actions CI/CD Workflow"
                },
                Progress = 30,
                Results = new List<string>
                {
                    "Increase profit margin per order by +18% through optimized menu recommendations.",
                    "Reduce food waste by 22% by aligning menu offerings with real-time inventory signals.",
                    "Enable automated daily menu refreshes with zero manual intervention from staff.",
                    "Empower chefs and reataurant Owners and Managers with actionable insights through intuitive dashboards."
                },
                Participants = new[]
                {
                    new ProjectParticipant { Name = "Dariem C. Macias", ImageUrl = "/images/dariem-avatar.png" }
                },
                Role = "Principal Consultant / AI Solution Architect",
                Challenges = new List<string>
                {
                    "Integrating AI/ML models for real-time menu optimization.",
                    "Building scalable cloud-native architecture for multi-tenant SaaS.",
                    "Automating data ingestion and dashboard reporting."
                },
                GithubUrl = "https://github.com/dariemcarlosdev/SmartMenuOptim",
                ProjectType = "Side Project"
            },
            new ProjectInfo
            {
                Name = "VPKFILEPROCESSOR – Cloud-Enabled ETL Pipeline Modernization",
                Status = "In Progress",
                Description = "VPKFILEPROCESSOR is a cloud-native solution that modernizes ETL workflows for MDCPS by migrating SSIS pipelines from on-premise to Azure. It allows users to upload files, automate processing via Azure Data Factory, and retrieve results through a Blazor Server interface. The system's modular microservices communicate asynchronously using Azure Event Grid, enabling scalable, secure, and fault-tolerant operations.",
                TechStack = new[] {
                    ".NET 8.0 SDK",
                    "ASP.NET Core (Blazor Server)",
                    "Git & GitHub Actions",
                    "SOLID Principles",
                    "Azure Functions",
                    "Azure Blob Storage",
                    "Azure Data Factory",
                    "Azure SQL Database",
                    "Azure Key Vault",
                    "Azure Event Grid",
                    "Azure Monitor & Application Insights",
                    "Docker",
                    "CI/CD Workflow",
                    "Azure Logic Apps"
                },
                Progress = 80,
                Results = new List<string>
                {
                    "Designed a scalable, decoupled system using Azure Event Grid",
                    "Containerized core services using Docker",
                    "Delivered a responsive Blazor Server interface",
                    "Implemented robust error handling and retry logic in Azure Functions",
                    "Established CI/CD pipelines with GitHub Actions",
                    "Enabled automated email notifications via Logic Apps",
                    "Implemented reliable Azure Event Grid publishing/subscription",
                    "Prepared architecture for Azure SignalR integration"
                },
                Participants = new[]
                {
                    new ProjectParticipant { Name = "Dariem C. Macias", ImageUrl = "/images/dariem-avatar.png" }
                },
                Role = "Principal Consultant / Cloud Solution Architect",
                Challenges = new List<string>
                {
                    "Migrating on-premise SSIS ETL workflows to Azure cloud.",
                    "Implementing reliable event-driven microservices communication.",
                    "Ensuring security and scalability for sensitive data processing."
                },
                GithubUrl = "https://github.com/dariemcarlosdev/VPKFILEPROCESSORAPP",
                ProjectType = "Customer: MDCPS"
            },
            new ProjectInfo
            {
                Name = "DineJoy - Cloud and AI-Driven aplications.",
                Status = "Planning",
                Description = "DineJoy empowers restaurant owners to launch branded loyalty rewards programs without technical overhead. By blending AI-driven personalization with seamless POS/loyalty integration.it helps restaurants to Boost repeat visits through customer-centric rewards, Increase campaign effectiveness with AI personalization, Save time via auto-suggested promotions and insights,Strengthen guest relationships with tailored experiences. ",
                TechStack = new[] {
                    ".NET 8.0/9.0 SDK",
                    "Blazor WASM + WPA",
                    "ASP.NET Core Web API",
                    "C#",
                    "PostgreSQL",
                    "Azure ML",
                    "Azure Cognitive Services",
                    "Azure OpenIA",
                    "Multi-Tenant SaaS Architecture",
                    "Power BI",
                    "Docker",
                    "GitHub Actions CI/CD workflow"
                },
                Progress = 10,
                Results = new List<string>
                {
                    "Boost repeat customer visits by 27% with branded loyalty programs.",
                    "Improve campaign conversion rates by 35% via AI-driven personalization.",
                    "Save 10+ staff hours monthly by automating promotion scheduling and recommendations.",
                    "Empower owners with guest insights through AI dashboards and predictive analytics."
                },
                Participants = new[]
                {
                    new ProjectParticipant { Name = "Dariem C. Macias", ImageUrl = "/images/dariem-avatar.png" }
                },
                Role = "Principal Consultant / Product Architect",
                Challenges = new List<string>
                {
                    "Integrating AI-driven personalization with POS/loyalty systems.",
                    "Designing scalable multi-tenant SaaS architecture.",
                    "Automating campaign management and analytics reporting."
                },
                GithubUrl = "https://github.com/dariemcarlosdev/DineJoyApp",
                ProjectType = "Side Project"
            },
            new ProjectInfo
            {
                Name ="Blazor TicketMaster - Event Booking App Api Integration",
                Status = "Completed",
                Description ="Blazor TicketMaster is a Blazor Server application that integrates with the TicketMaster API to provide users with a seamless event browsing and ticket booking experience. The app features a responsive UI, secure authentication, and real-time data fetching from the TicketMaster API.",
                TechStack = new[] { ".NET 8.0 SDK", "C#", "Blazor Server", "ASP.NET Core Web API", "TicketMaster API", "Entity Framework Core", "SQLite", "Docker", "CI/CD Workflow" },
                Progress = 100,
                Results = new List<string>
                {
                    "Integrated TicketMaster API for real-time event data fetching.",
                    "Implemented secure user authentication and authorization.",
                    "Designed a responsive UI for seamless event browsing and ticket booking.",
                    "Containerized application with Docker for consistent deployment.",
                    "Established CI/CD pipelines to automate build, test, and deployment processes."
                },
                Participants = new[]
                {
                    new ProjectParticipant { Name = "Dariem C. Macias", ImageUrl = "/images/dariem-avatar.png" },
                },
                Role = "Principal Consultant / Solution Architect",
                Challenges = new List<string>
                {   
                    "Enables users to search for attractions using the Ticketmaster API",
                    "Dinamically displays event details and ticket availability.",
                    "Integrating with the TicketMaster API for real-time data.",
                    "Implementing secure authentication and authorization.",
                    "Designing a responsive and user-friendly UI."
                },
                GithubUrl = "https://github.com/dariemcarlosdev/BlazorTicketmasterApiIntegration",
                ProjectType = "Side Project"
            }
        };
    }
}
