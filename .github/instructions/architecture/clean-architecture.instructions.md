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

Contains core business logic with zero framework dependencies. No references to EF Core, ASP.NET, MediatR, or infrastructure packages. Entities own their invariants — validate state transitions inside aggregates. Use `record` types for value objects and domain events. Strategy interfaces define **what** can happen, not **how**. Example: `Order.Process()` validates status before transition and throws `InvalidOperationException` on violations.

| Directory | Contents | Examples |
|---|---|---|
| `Models/` | Entities, value objects, enums | `Order`, `Customer`, `Address`, `OrderStatus` |
| `Events/` | Domain events | `OrderCreatedEvent`, `OrderCompletedEvent`, `OrderCancelledEvent` |
| `Services/Strategies/` | Strategy interfaces | `IChargeable`, `IRefundable`, `ICancellable` |

---

## Application Layer

**Namespace:** `MyApp.Features.Orders.*` (vertical slices)

Orchestrates use cases via MediatR commands/queries. Depends on Domain; never on Infrastructure. Inject **interfaces** only (`IOrderRepository`, `IEventBus`) — never concrete types or `AppDbContext`. Return result DTOs — never expose domain entities to outer layers. FluentValidation validators live next to their commands.

| Directory | Contents | Examples |
|---|---|---|
| `Features/Orders/CreateOrder/` | Command, handler, result DTO | `CreateOrderCommand`, `CreateOrderHandler`, `CreateOrderResult` |
| `Features/Orders/CompleteOrder/` | Command, handler, result DTO | `CompleteOrderCommand`, `CompleteOrderHandler` |
| `Features/Orders/CancelOrder/` | Command, handler, result DTO | `CancelOrderCommand`, `CancelOrderHandler` |
| `Services/` | Application service interfaces | `IOrderManagerService` |

---

## Infrastructure Layer

**Namespaces:** `MyApp.Data`, `MyApp.Infrastructure`

Implements interfaces defined in Domain and Application. Owns all external concerns. External SDK usage (payment providers, messaging, etc.) is confined to this layer. EF Core configurations (Fluent API) live in `Data/Configurations/`. Never expose `DbContext` outside this layer. Repository implementations use `FirstOrDefaultAsync()` with parameterized predicates (never raw SQL concatenation).

| Directory | Contents | Examples |
|---|---|---|
| `Data/` | EF Core context, repository implementations, migrations | `AppDbContext`, `OrderRepository` |
| `Infrastructure/` | External integrations, auth middleware | Payment service, `InMemoryEventBus` |

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

Register dependencies with interface-to-implementation mapping: strategy implementations (domain interfaces), MediatR with assembly scanning, application services, `AppDbContext` with provider config, repositories, and event bus. Use `AddScoped` for request-scoped services, `AddSingleton` for stateless shared services.

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

- Domain must never reference EF Core (`using MyApp.Data` is a violation)
- Application layer must never inject `AppDbContext` — use `IRepository` instead
- Blazor components must never inject repositories directly — use `IMediator.Send()`
- Handlers must never return domain entities to Presentation — map to result DTOs
- Application interfaces must never expose infrastructure types (e.g., `DbSet<T>` is EF Core only)

---

## Reference

See `docs/` directory for full architecture documentation and decision records.
