# Layer Compliance — Clean Architecture Boundaries

## Purpose

Validate that each layer in a Clean Architecture project adheres to its defined responsibilities and dependency constraints.

## Layer Responsibilities (the project Stack)

| Layer | Projects | Allowed Dependencies | Responsibility |
|-------|----------|---------------------|----------------|
| **Domain** | `MyApp.Domain` | None (innermost) | Entities, Value Objects, Domain Events, Repository interfaces |
| **Application** | `MyApp.Application` | Domain only | CQRS handlers, DTOs, Validators, Application interfaces |
| **Infrastructure** | `MyApp.Infrastructure` | Application, Domain | EF Core DbContext, Repository implementations, External services |
| **Presentation** | `MyApp.Web` | Application, Domain | Blazor components, API controllers, Middleware |

## Compliance Checks

### 1. Domain Layer Purity

The Domain layer must have **zero** outward dependencies:

```csharp
// ✅ Domain entity — no infrastructure references
namespace MyApp.Domain.Entities;

public sealed class Order : AggregateRoot<OrderId>
{
    public Money Amount { get; private set; }
    public OrderStatus Status { get; private set; }

    public void Release(UserId authorizedBy)
    {
        if (Status != OrderStatus.Funded)
            throw new DomainException("Cannot release unfunded order");
        AddDomainEvent(new EscrowReleasedEvent(Id, authorizedBy));
        Status = OrderStatus.Released;
    }
}
```

**Red flags in Domain layer:**
- `using Microsoft.EntityFrameworkCore;` — EF Core leak
- `using System.Net.Http;` — HTTP client dependency
- `using Microsoft.Extensions.Logging;` — Infrastructure concern
- Any `[JsonProperty]` or serialization attributes

### 2. Application Layer Boundaries

Application may reference Domain but NEVER Infrastructure:

```csharp
// ✅ Application handler — depends only on Domain interfaces
public sealed class FundEscrowHandler : IRequestHandler<FundEscrowCommand, Result>
{
    private readonly IEscrowRepository _orderRepo;    // Domain interface
    private readonly IPaymentGateway _paymentGateway;  // Application interface
    private readonly IUnitOfWork _unitOfWork;           // Application interface
}

// ❌ Violation — Application referencing Infrastructure
using MyApp.Infrastructure.Data; // NEVER DO THIS
```

### 3. Infrastructure Implementation Check

Infrastructure implements interfaces defined in Domain/Application:

```csharp
// ✅ Infrastructure implements Domain interface
namespace MyApp.Infrastructure.Persistence;

internal sealed class EscrowRepository : IEscrowRepository
{
    private readonly AppDbContext _context;
    public async Task<Order?> GetByIdAsync(
        OrderId id, CancellationToken ct)
        => await _context.Orders
            .FirstOrDefaultAsync(e => e.Id == id, ct);
}
```

### 4. Presentation Layer Rules

- Blazor components inject Application services, never Infrastructure directly
- API controllers call MediatR, not repositories
- No business logic in components — delegate to Application layer

## Detection Commands

```bash
# Find Domain layer violations (.NET)
grep -rn "using MyApp.Infrastructure" src/MyApp.Domain/
grep -rn "using MyApp.Web" src/MyApp.Domain/
grep -rn "using Microsoft.EntityFrameworkCore" src/MyApp.Domain/

# Find Application layer violations
grep -rn "using MyApp.Infrastructure" src/MyApp.Application/

# Verify .csproj references
dotnet list src/MyApp.Domain/MyApp.Domain.csproj reference
```

## Severity Classification

| Violation | Severity | Example |
|-----------|----------|---------|
| Domain → Infrastructure | CRITICAL | Entity using DbContext |
| Application → Infrastructure | CRITICAL | Handler using concrete repository |
| Presentation → Infrastructure (direct) | WARNING | Component bypassing Application |
| Cross-cutting concern leak | WARNING | Logger in Domain entity |
| Shared kernel misuse | INFO | Utility in wrong layer |
