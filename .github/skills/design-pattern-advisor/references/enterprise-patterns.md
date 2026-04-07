# Enterprise Patterns — Repository, UoW, CQRS, Event Sourcing

## When to Consider Enterprise Patterns

- Data access needs abstraction for testability and portability
- Read and write workloads have different performance characteristics
- You need full audit trail / event history of domain state changes
- Complex domain logic needs transactional consistency boundaries

## Repository Pattern

**Intent:** Mediate between the domain and data mapping layers using a collection-like interface for accessing domain objects.

### .NET Implementation

```csharp
// Domain layer — repository interface (per aggregate root)
public interface IEscrowRepository
{
    Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct);
    Task<IReadOnlyList<Order>> GetByBuyerAsync(UserId buyerId, CancellationToken ct);
    Task AddAsync(Order transaction, CancellationToken ct);
    Task UpdateAsync(Order transaction, CancellationToken ct);
}

// Infrastructure layer — EF Core implementation
internal sealed class EscrowRepository(AppDbContext context) : IEscrowRepository
{
    public async Task<Order?> GetByIdAsync(
        OrderId id, CancellationToken ct)
        => await context.Orders
            .Include(e => e.Milestones)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<Order>> GetByBuyerAsync(
        UserId buyerId, CancellationToken ct)
        => await context.Orders
            .AsNoTracking()
            .Where(e => e.BuyerId == buyerId)
            .ToListAsync(ct);

    public async Task AddAsync(Order transaction, CancellationToken ct)
        => await context.Orders.AddAsync(transaction, ct);

    public Task UpdateAsync(Order transaction, CancellationToken ct)
    {
        context.Orders.Update(transaction);
        return Task.CompletedTask;
    }
}
```

**Anti-patterns to avoid:**
- Generic `IRepository<T>` that exposes `IQueryable<T>` — leaks EF Core details
- Repository with methods for every conceivable query — use Specification pattern instead
- Repository that handles its own transactions — use Unit of Work

## Unit of Work Pattern

**Intent:** Maintain a list of objects affected by a business transaction and coordinate the writing out of changes.

```csharp
// Application layer interface
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
}

// Infrastructure — EF Core DbContext IS the Unit of Work
internal sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Order> Orders => Set<Order>();

    public override async Task<int> SaveChangesAsync(CancellationToken ct)
    {
        // Dispatch domain events before saving
        var events = ChangeTracker.Entries<AggregateRoot>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        var result = await base.SaveChangesAsync(ct);

        // Publish events after successful save
        foreach (var domainEvent in events)
            await _mediator.Publish(domainEvent, ct);

        return result;
    }
}
```

## CQRS — Command Query Responsibility Segregation

**Intent:** Use separate models for reading and writing data.

### Implementation with MediatR

```csharp
// COMMAND — Write path (full domain model, validation, business rules)
public sealed record FundEscrowCommand(
    OrderId EscrowId, Money Amount) : IRequest<Result>;

public sealed class FundEscrowHandler(
    IEscrowRepository repo, IUnitOfWork uow)
    : IRequestHandler<FundEscrowCommand, Result>
{
    public async Task<Result> Handle(FundEscrowCommand request, CancellationToken ct)
    {
        var order = await repo.GetByIdAsync(request.EscrowId, ct);
        if (order is null) return Result.NotFound();

        order.Fund(request.Amount);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// QUERY — Read path (optimized, no tracking, projection)
public sealed record GetOrderSummaryQuery(
    OrderId EscrowId) : IRequest<EscrowSummaryDto?>;

public sealed class GetOrderSummaryHandler(AppDbContext context)
    : IRequestHandler<GetOrderSummaryQuery, EscrowSummaryDto?>
{
    public async Task<EscrowSummaryDto?> Handle(
        GetOrderSummaryQuery request, CancellationToken ct)
        => await context.Orders
            .AsNoTracking()
            .Where(e => e.Id == request.EscrowId)
            .Select(e => new EscrowSummaryDto(
                e.Id, e.Status, e.Amount, e.CreatedAt))
            .FirstOrDefaultAsync(ct);
}
```

## Event Sourcing (Advanced)

**Intent:** Store the state of a domain entity as a sequence of state-changing events.

**Use when:** Full audit trail is legally required (fintech compliance), or you need temporal queries ("what was the order state on Jan 15?").

```csharp
// Event store concept
public interface IEventStore
{
    Task AppendAsync(Guid streamId, IReadOnlyList<IDomainEvent> events, CancellationToken ct);
    Task<IReadOnlyList<IDomainEvent>> GetStreamAsync(Guid streamId, CancellationToken ct);
}

// Rebuilding state from events
public Order Rehydrate(IReadOnlyList<IDomainEvent> events)
{
    var order = new Order();
    foreach (var @event in events)
        order.Apply(@event); // Each event mutates state
    return order;
}
```

**⚠️ YAGNI Warning:** Event Sourcing adds significant complexity. Only use when you have a regulatory requirement for complete audit trails or need temporal queries. For most applications, CQRS without Event Sourcing is sufficient.

## Decision Matrix

| Need | Pattern | Complexity | the project Guidance |
|------|---------|-----------|---------------------|
| Abstract data access | Repository | Low | ✅ Always use per aggregate root |
| Transactional consistency | Unit of Work | Low | ✅ Use EF Core DbContext as UoW |
| Separate read/write | CQRS | Medium | ✅ Use with MediatR |
| Complete audit trail | Event Sourcing | High | ⚠️ Only for compliance-critical flows |
