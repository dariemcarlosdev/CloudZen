---
paths:
  - "**/*.cs"
description: Clean Architecture layer rules and dependency direction for all C# files
---

# Clean Architecture

> Auto-loaded by Claude Code when working with matching files.
> Full reference: `.github/instructions/architecture/clean-architecture.instructions.md`

## Layer Dependency Direction

```
Presentation (Components/) → Application (Features/) → Domain (Models/, Events/)
                                                          ↑
                                               Infrastructure (Data/, Infrastructure/)
```

Inner layers **never** reference outer layers. Dependencies always point inward.

## Layer Rules

### Domain (`MyApp.Models`, `MyApp.Events`, `MyApp.Services.Strategies`)
- Zero framework dependencies — no EF Core, ASP.NET, MediatR references
- Entities own their invariants; validate state transitions inside the aggregate
- Use `record` types for value objects and domain events
- Strategy interfaces define **what**, not **how**

### Application (`MyApp.Features.*`)
- Inject **interfaces only** — never concrete types, never `DbContext`
- Return result DTOs — never expose domain entities to outer layers
- FluentValidation validators live next to their commands

### Infrastructure (`MyApp.Data`, `MyApp.Infrastructure`)
- Implements repository interfaces and strategy implementations
- EF Core Fluent API configs in `Data/Configurations/`
- Never expose `DbContext` outside this layer

### Presentation (`MyApp.Components`)
- Never inject repositories, `DbContext`, or infrastructure services
- Always go through `IMediator.Send()` or application service interfaces
- Code-behind pattern mandatory (`.razor` + `.razor.cs` + `.razor.css`)

## DI Registration

- Register interface→implementation mappings in `Program.cs`
- Domain strategies: `AddScoped<IChargeable, StripePaymentProcessor>()`
- MediatR: `AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateOrderCommand>())`
- Infrastructure: `AddDbContext<AppDbContext>()`, `AddScoped<IOrderRepository, OrderRepository>()`

## Forbidden Patterns

- ❌ Domain referencing Infrastructure (`using MyApp.Data` in Models/)
- ❌ `DbContext` injection in Application layer handlers
- ❌ Blazor components calling repositories directly
- ❌ Returning domain entities from handlers to Presentation
- ❌ Infrastructure types (e.g., `DbSet<T>`) leaking into Application interfaces

---

*Deep-dive: Read `.github/instructions/architecture/clean-architecture.instructions.md` for complete patterns and examples.*
