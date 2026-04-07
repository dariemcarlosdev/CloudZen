---
applyTo: "**/*.cs"
---

# Clean Architecture — Project Conventions

## Layer Overview

```
Presentation (Components/)
    ↓
Application (Features/)
    ↓
Domain (Models/, Events/, Services/Strategies/ interfaces)
    ↑
Infrastructure (Data/, Infrastructure/)
```

Inner layers **never** reference outer layers. Dependencies always point inward.

---

## Domain Layer

**Namespaces:** `MyApp.Models`, `MyApp.Events`, `MyApp.Services.Strategies`

Contains the core business logic with zero framework dependencies.

| Directory | Contents | Examples |
|---|---|---|
| `Models/` | Entities, value objects, enums | `Order`, `Customer`, `Address`, `OrderStatus` |
| `Events/` | Domain events | `OrderCreatedEvent`, `OrderCompletedEvent`, `OrderCancelledEvent` |
| `Services/Strategies/` | Strategy interfaces | `IChargeable`, `IRefundable`, `ICancellable` |

**Rules:**
- No references to EF Core, ASP.NET, MediatR, or any infrastructure package
- Entities own their invariants — validate state transitions inside the aggregate
- Use `record` types for value objects and domain events
- Strategy interfaces define **what** can happen, not **how**

```csharp
// ✅ Domain — pure business logic
namespace MyApp.Models;

public sealed class Order
{
    public Guid Id { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal Amount { get; private set; }
    public Customer Buyer { get; private set; } = default!;
    public Customer Seller { get; private set; } = default!;

    public void Process()
    {
        if (Status != OrderStatus.Created)
            throw new InvalidOperationException("Only newly created orders can be processed.");

        Status = OrderStatus.Processing;
    }
}
```

---

## Application Layer

**Namespace:** `MyApp.Features.Orders.*` (vertical slices)

Orchestrates use cases via MediatR commands/queries. Depends on Domain; never on Infrastructure.

| Directory | Contents | Examples |
|---|---|---|
| `Features/Orders/CreateOrder/` | Command, handler, result DTO | `CreateOrderCommand`, `CreateOrderHandler`, `CreateOrderResult` |
| `Features/Orders/CompleteOrder/` | Command, handler, result DTO | `CompleteOrderCommand`, `CompleteOrderHandler` |
| `Features/Orders/CancelOrder/` | Command, handler, result DTO | `CancelOrderCommand`, `CancelOrderHandler` |
| `Services/` | Application service interfaces | `IOrderManagerService` |

**Rules:**
- Inject **interfaces** (`IOrderRepository`, `IEventBus`) — never concrete types
- Never reference `AppDbContext` or any EF Core type
- Return result DTOs — never expose domain entities to outer layers
- FluentValidation validators live next to their commands

```csharp
// ✅ Application — depends on Domain interfaces only
namespace MyApp.Features.Orders.CreateOrder;

public sealed class CreateOrderHandler(
    IOrderRepository repository,
    IEventBus eventBus) : IRequestHandler<CreateOrderCommand, CreateOrderResult>
{
    public async Task<CreateOrderResult> Handle(
        CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(request.OrderId, cancellationToken);
        // ...orchestration logic
    }
}
```

---

## Infrastructure Layer

**Namespaces:** `MyApp.Data`, `MyApp.Infrastructure`

Implements interfaces defined in Domain and Application. Owns all external concerns.

| Directory | Contents | Examples |
|---|---|---|
| `Data/` | EF Core context, repository implementations, migrations | `AppDbContext`, `OrderRepository` |
| `Infrastructure/` | External integrations, auth middleware | Payment service, `InMemoryEventBus` |

**Rules:**
- Implements `IOrderRepository`, `IEventBus`, strategy implementations
- External SDK usage (payment providers, messaging, etc.) is confined to this layer
- EF Core configurations (Fluent API) live in `Data/Configurations/`
- Never expose `DbContext` outside this layer

```csharp
// ✅ Infrastructure — implements Domain interface
namespace MyApp.Data;

public sealed class OrderRepository(AppDbContext context)
    : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(
        Guid id, CancellationToken cancellationToken) =>
        await context.Orders
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
}
```

---

## Presentation Layer

**Namespace:** `MyApp.Components`

Blazor Server pages, layouts, and shared UI components. Depends on Application only.

**Rules:**
- Never inject repositories, `DbContext`, or infrastructure services
- Always go through `IMediator.Send()` or application service interfaces
- Code-behind pattern mandatory (`.razor` + `.razor.cs` + `.razor.css`)
- Use `[CascadingParameter] Task<AuthenticationState>` for auth — not `IHttpContextAccessor`

---

## DI Registration in Program.cs

Register dependencies with interface-to-implementation mapping:

```csharp
// Domain strategy implementations
builder.Services.AddScoped<IChargeable, StripePaymentProcessor>();
builder.Services.AddScoped<IRefundable, StripeRefundProcessor>();
builder.Services.AddScoped<ICancellable, StripeCancellationProcessor>();

// Application services
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<CreateOrderCommand>());
builder.Services.AddScoped<IOrderManagerService, OrderManagerService>();

// Infrastructure
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();
```

---

## Namespace Conventions

| Layer | Namespace Pattern | Example |
|---|---|---|
| Domain | `MyApp.Models`, `MyApp.Events` | `MyApp.Models.Order` |
| Application | `MyApp.Features.{Aggregate}.{Action}` | `MyApp.Features.Orders.CreateOrder` |
| Infrastructure | `MyApp.Data`, `MyApp.Infrastructure` | `MyApp.Data.AppDbContext` |
| Presentation | `MyApp.Components.Pages` | `MyApp.Components.Pages.Dashboard` |

---

## Anti-Patterns — What NOT to Do

```csharp
// ❌ Domain referencing Infrastructure
namespace MyApp.Models;
using MyApp.Data; // VIOLATION — Domain must not know about EF Core

// ❌ Injecting DbContext in Application layer
public sealed class CreateOrderHandler(AppDbContext context) // VIOLATION — use IRepository
    : IRequestHandler<CreateOrderCommand, CreateOrderResult> { }

// ❌ Blazor component calling repository directly
@inject IOrderRepository Repository  // VIOLATION — use IMediator

// ❌ Returning domain entities from handlers to Presentation
return order; // VIOLATION — map to a result DTO

// ❌ Infrastructure types leaking into Application interfaces
public interface IOrderManagerService
{
    Task<DbSet<Order>> GetAll(); // VIOLATION — DbSet is EF Core
}
```

---

## Reference

See `docs/` directory for full architecture documentation and decision records.
