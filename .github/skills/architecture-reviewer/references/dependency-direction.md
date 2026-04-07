# Dependency Direction — Validating Dependency Inversion

## Purpose

Ensure all dependencies flow inward (Presentation → Infrastructure → Application → Domain) and that the Dependency Inversion Principle (DIP) is correctly applied at architectural boundaries.

## The Dependency Rule

```
┌─────────────────────────────────┐
│         Presentation            │  ← Outermost (Blazor, APIs)
│   ┌─────────────────────────┐   │
│   │      Infrastructure     │   │  ← Implements interfaces
│   │   ┌─────────────────┐   │   │
│   │   │   Application   │   │   │  ← Orchestrates use cases
│   │   │   ┌─────────┐   │   │   │
│   │   │   │ Domain  │   │   │   │  ← Innermost (entities, rules)
│   │   │   └─────────┘   │   │   │
│   │   └─────────────────┘   │   │
│   └─────────────────────────┘   │
└─────────────────────────────────┘

Arrows point INWARD only. Never outward.
```

## Validation Checklist

### Step 1: Verify .csproj References

```xml
<!-- ✅ Domain.csproj — NO ProjectReferences -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <!-- No <ProjectReference> elements -->
</Project>

<!-- ✅ Application.csproj — References Domain ONLY -->
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\MyApp.Domain\MyApp.Domain.csproj" />
  </ItemGroup>
</Project>

<!-- ❌ VIOLATION: Application referencing Infrastructure -->
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\MyApp.Domain\MyApp.Domain.csproj" />
    <ProjectReference Include="..\MyApp.Infrastructure\MyApp.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

### Step 2: Scan Using Statements

```bash
# Domain must not reference any other project namespace
grep -rn "using MyApp\.\(Application\|Infrastructure\|Web\)" src/MyApp.Domain/

# Application must not reference Infrastructure or Web
grep -rn "using MyApp\.\(Infrastructure\|Web\)" src/MyApp.Application/
```

### Step 3: Check DI Registration (Composition Root)

The **Composition Root** (typically `Program.cs` or a DI extension class) is the ONLY place where concrete types are wired to abstractions:

```csharp
// ✅ Composition Root in Web project — the only place concrete types appear
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IEscrowRepository, EscrowRepository>();
        services.AddScoped<IPaymentGateway, StripePaymentGateway>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}
```

## Common Violations and Fixes

### Violation 1: Domain References Infrastructure Package

```csharp
// ❌ Domain entity using EF Core annotations
using System.ComponentModel.DataAnnotations.Schema;

[Table("orders")]
public class Order { }

// ✅ Fix: Use Fluent API configuration in Infrastructure
// Infrastructure/Persistence/Configurations/OrderConfiguration.cs
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
    }
}
```

### Violation 2: Application Creates Infrastructure Types

```csharp
// ❌ Application handler creating Infrastructure concern
public sealed class SendNotificationHandler : IRequestHandler<SendNotificationCommand>
{
    public async Task Handle(SendNotificationCommand request, CancellationToken ct)
    {
        var client = new SmtpClient("smtp.server.com"); // ❌ Infrastructure leak
    }
}

// ✅ Fix: Inject abstraction defined in Application
public sealed class SendNotificationHandler : IRequestHandler<SendNotificationCommand>
{
    private readonly INotificationService _notifier;
    public SendNotificationHandler(INotificationService notifier) => _notifier = notifier;
    
    public async Task Handle(SendNotificationCommand request, CancellationToken ct)
        => await _notifier.SendAsync(request.Message, ct);
}
```

### Violation 3: Presentation Bypasses Application

```csharp
// ❌ Blazor component directly using repository
@inject IEscrowRepository Repository  // Bypasses Application layer

// ✅ Fix: Go through MediatR
@inject IMediator Mediator
var result = await Mediator.Send(new GetOrderQuery(orderId));
```

## Automated Enforcement

### ArchUnit-Style Tests (.NET)

```csharp
[Fact]
public void Domain_Should_Not_Reference_Application()
{
    var domainAssembly = typeof(Order).Assembly;
    var referencedAssemblies = domainAssembly.GetReferencedAssemblies();
    
    referencedAssemblies.Should().NotContain(a => 
        a.Name!.Contains("Application") || 
        a.Name!.Contains("Infrastructure") || 
        a.Name!.Contains("Web"));
}
```

## Severity Guide

| Violation | Severity | Why |
|-----------|----------|-----|
| Domain → any outer layer | CRITICAL | Corrupts the core model |
| Application → Infrastructure | CRITICAL | Breaks testability and portability |
| Presentation → Infrastructure (direct) | WARNING | Bypasses business rules |
| Shared utility in wrong layer | INFO | Organizational issue |
