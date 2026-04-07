# C# XML Documentation Reference

Complete guide for generating XML doc comments for .NET public APIs.

## Required Tags for Public Members

### Class / Interface / Record
```csharp
/// <summary>
/// Manages the order lifecycle from creation through funding, release, and dispute resolution.
/// </summary>
/// <remarks>
/// Registered as <see cref="ServiceLifetime.Scoped"/>. Requires
/// <see cref="IEscrowRepository"/> and <see cref="IPaymentGateway"/> in DI.
/// Thread-safe for concurrent access within a single request scope.
/// </remarks>
public sealed class OrderService : IOrderService
```

### Method
```csharp
/// <summary>
/// Creates a new order transaction between buyer and seller with the specified terms.
/// </summary>
/// <param name="command">
/// The creation command containing buyer ID, seller ID, amount, currency, and deadline.
/// </param>
/// <param name="cancellationToken">Token to cancel the operation.</param>
/// <returns>
/// A <see cref="Result{T}"/> containing the new <see cref="EscrowId"/> on success,
/// or validation errors on failure.
/// </returns>
/// <exception cref="DuplicateEscrowException">
/// Thrown when an order with identical terms already exists within the cooldown period.
/// </exception>
/// <example>
/// <code>
/// var result = await orderService.CreateAsync(
///     new CreateOrderCommand(buyerId, sellerId, Money.USD(500), deadline),
///     cancellationToken);
/// if (result.IsSuccess)
///     logger.LogInformation("Escrow {Id} created", result.Value);
/// </code>
/// </example>
public async Task<Result<EscrowId>> CreateAsync(
    CreateOrderCommand command,
    CancellationToken cancellationToken = default)
```

### Property
```csharp
/// <summary>
/// Gets the current order status in the lifecycle state machine.
/// </summary>
/// <value>
/// One of <see cref="OrderStatus.Draft"/>, <see cref="OrderStatus.Funded"/>,
/// <see cref="OrderStatus.Released"/>, or <see cref="OrderStatus.Disputed"/>.
/// Defaults to <see cref="OrderStatus.Draft"/> on creation.
/// </value>
public OrderStatus Status { get; private set; }
```

### Enum
```csharp
/// <summary>
/// Represents the lifecycle stages of an order transaction.
/// </summary>
public enum OrderStatus
{
    /// <summary>Escrow created but not yet funded by the buyer.</summary>
    Draft = 0,

    /// <summary>Buyer has deposited funds; awaiting seller fulfillment.</summary>
    Funded = 1,

    /// <summary>Funds released to seller after buyer confirmation.</summary>
    Released = 2,

    /// <summary>Transaction under dispute; funds held pending resolution.</summary>
    Disputed = 3,

    /// <summary>Escrow cancelled; funds returned to buyer.</summary>
    Cancelled = 99
}
```

### Constructor
```csharp
/// <summary>
/// Initializes a new <see cref="OrderService"/> with required dependencies.
/// </summary>
/// <param name="repository">The order persistence store.</param>
/// <param name="paymentGateway">The payment processing gateway.</param>
/// <param name="logger">The structured logger instance.</param>
/// <exception cref="ArgumentNullException">
/// Any parameter is <see langword="null"/>.
/// </exception>
public OrderService(
    IEscrowRepository repository,
    IPaymentGateway paymentGateway,
    ILogger<OrderService> logger)
```

## Cross-Reference Patterns

```csharp
/// <see cref="OrderService"/>                    — link to type
/// <see cref="OrderService.CreateAsync"/>         — link to method
/// <see cref="Result{T}"/>                         — link to generic type
/// <see langword="null"/>                          — keyword reference
/// <see langword="true"/>                          — keyword reference
/// <paramref name="command"/>                      — reference to parameter
/// <typeparamref name="T"/>                        — reference to type parameter
/// <inheritdoc/>                                   — inherit from interface/base
/// <inheritdoc cref="IOrderService.CreateAsync"/> — inherit from specific member
```

## Documentation Priority Matrix

| Priority | Target | Example |
|----------|--------|---------|
| P0 | Public API endpoints | Controller actions, Minimal API handlers |
| P0 | Public interfaces | `IOrderService`, `IEscrowRepository` |
| P1 | Public classes | `OrderService`, `EscrowValidator` |
| P1 | Public methods with params | `CreateAsync(command, ct)` |
| P2 | Public properties | Non-obvious computed or validated properties |
| P2 | Public enums | Domain status enums with business meaning |
| P3 | Complex private methods | Only when logic is genuinely non-obvious |

## Enabling XML Doc Warnings

```xml
<!-- In .csproj — treat missing docs as build warnings -->
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);CS1591</NoWarn> <!-- Remove to enforce -->
</PropertyGroup>
```
