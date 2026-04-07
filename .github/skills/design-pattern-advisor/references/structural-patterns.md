# Structural Patterns — Adapter, Decorator, Facade, Proxy

## When to Consider Structural Patterns

- You need to compose objects to form larger structures
- You need to adapt an incompatible interface to work with existing code
- You want to add behavior transparently without modifying existing classes

## Decorator Pattern

**Intent:** Attach additional responsibilities to an object dynamically. Decorators provide a flexible alternative to subclassing.

**Use when:** Adding cross-cutting concerns (logging, caching, retry, validation) to existing services.

### .NET Implementation — Caching Decorator for Escrow Repository

```csharp
// Base interface (Domain layer)
public interface IEscrowRepository
{
    Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct);
    Task AddAsync(Order transaction, CancellationToken ct);
}

// Caching decorator (Infrastructure layer)
internal sealed class CachingEscrowRepository(
    IEscrowRepository inner,
    IMemoryCache cache,
    ILogger<CachingEscrowRepository> logger) : IEscrowRepository
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<Order?> GetByIdAsync(
        OrderId id, CancellationToken ct)
    {
        var cacheKey = $"order:{id}";
        if (cache.TryGetValue(cacheKey, out Order? cached))
        {
            logger.LogDebug("Cache hit for order {Id}", id);
            return cached;
        }

        var result = await inner.GetByIdAsync(id, ct);
        if (result is not null)
            cache.Set(cacheKey, result, CacheDuration);

        return result;
    }

    public Task AddAsync(Order transaction, CancellationToken ct)
        => inner.AddAsync(transaction, ct); // Write-through, no caching
}

// DI Registration — order matters: outermost decorator registered last
services.AddScoped<EscrowRepository>();  // concrete
services.AddScoped<IEscrowRepository>(sp =>
    new CachingEscrowRepository(
        sp.GetRequiredService<EscrowRepository>(),
        sp.GetRequiredService<IMemoryCache>(),
        sp.GetRequiredService<ILogger<CachingEscrowRepository>>()));
```

**Tip:** Use Scrutor for cleaner decorator registration:
```csharp
services.AddScoped<IEscrowRepository, EscrowRepository>();
services.Decorate<IEscrowRepository, CachingEscrowRepository>();
services.Decorate<IEscrowRepository, LoggingEscrowRepository>();
```

## Adapter Pattern

**Intent:** Convert the interface of a class into another interface clients expect.

**Use when:** Integrating third-party libraries or legacy APIs that don't match your domain interfaces.

### .NET Implementation — External API Adapter

```csharp
// Your Application interface
public interface IKycVerificationService
{
    Task<KycResult> VerifyAsync(UserId userId, KycDocuments docs, CancellationToken ct);
}

// Third-party SDK has incompatible interface
// ThirdPartyKyc.Client.VerifyIdentity(string, byte[], string)

// Adapter in Infrastructure layer
internal sealed class ThirdPartyKycAdapter(
    ThirdPartyKyc.Client client,
    ILogger<ThirdPartyKycAdapter> logger) : IKycVerificationService
{
    public async Task<KycResult> VerifyAsync(
        UserId userId, KycDocuments docs, CancellationToken ct)
    {
        var response = await client.VerifyIdentity(
            userId.ToString(),
            docs.ToByteArray(),
            docs.DocumentType.ToString());

        return response.Status switch
        {
            "APPROVED" => KycResult.Approved,
            "PENDING" => KycResult.PendingReview,
            _ => KycResult.Rejected(response.Reason)
        };
    }
}
```

## Facade Pattern

**Intent:** Provide a unified interface to a set of interfaces in a subsystem.

**Use when:** A complex subsystem has many moving parts that clients shouldn't need to understand.

```csharp
// Facade simplifying order lifecycle operations
public sealed class EscrowLifecycleFacade(
    IMediator mediator,
    INotificationService notifications,
    IAuditLogger audit)
{
    public async Task<Result<EscrowId>> CreateAndNotifyAsync(
        CreateOrderCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        if (result.IsSuccess)
        {
            await notifications.SendAsync(new EscrowCreatedNotification(result.Value), ct);
            await audit.LogAsync($"Escrow {result.Value} created", ct);
        }
        return result;
    }
}
```

## Proxy Pattern

**Intent:** Provide a surrogate to control access to another object.

**Use when:** You need lazy loading, access control, or remote service abstraction.

```csharp
// Authorization proxy — checks permissions before delegating
internal sealed class AuthorizedEscrowRepository(
    IEscrowRepository inner,
    ICurrentUser currentUser) : IEscrowRepository
{
    public async Task<Order?> GetByIdAsync(
        OrderId id, CancellationToken ct)
    {
        var order = await inner.GetByIdAsync(id, ct);
        if (order is not null && !currentUser.CanAccess(order))
            throw new UnauthorizedAccessException("No access to this order");
        return order;
    }
}
```

## Decision Matrix

| Problem | Pattern | Complexity | When to Use |
|---------|---------|-----------|-------------|
| Add behavior transparently | Decorator | Low-Medium | Caching, logging, retry, metrics |
| Incompatible interface | Adapter | Low | Third-party integrations |
| Complex subsystem | Facade | Low | Simplifying multi-step workflows |
| Access control / lazy load | Proxy | Low-Medium | Authorization, virtual proxies |
