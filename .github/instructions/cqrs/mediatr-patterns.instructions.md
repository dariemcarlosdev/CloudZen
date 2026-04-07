---
applyTo: "**/Features/**/*.cs"
---

# MediatR & CQRS Patterns — Project Conventions

## Vertical Slice Structure

Each use case is a self-contained slice within `Features/{Aggregate}/`:

```
Features/
└── Orders/
    ├── CreateOrder/
    │   ├── CreateOrderCommand.cs          ← IRequest<CreateOrderResult>
    │   ├── CreateOrderCommandValidator.cs ← FluentValidation
    │   ├── CreateOrderHandler.cs          ← IRequestHandler<,>
    │   └── CreateOrderResult.cs           ← Result DTO
    ├── CompleteOrder/
    │   ├── CompleteOrderCommand.cs
    │   ├── CompleteOrderCommandValidator.cs
    │   ├── CompleteOrderHandler.cs
    │   └── CompleteOrderResult.cs
    ├── CancelOrder/
    │   ├── CancelOrderCommand.cs
    │   ├── CancelOrderCommandValidator.cs
    │   ├── CancelOrderHandler.cs
    │   └── CancelOrderResult.cs
    └── GetOrders/
        ├── GetOrdersQuery.cs
        ├── GetOrdersHandler.cs
        └── OrderDto.cs
```

**One command/query, one handler, one result per folder.** No shared handlers.

---

## Command vs Query Separation

| Aspect | Command (Write) | Query (Read) |
|---|---|---|
| Purpose | Mutate state | Return data |
| Naming | `{Verb}{Noun}Command` | `Get{Noun}Query` / `List{Noun}Query` |
| Returns | Result DTO with success/error | DTO or collection |
| Side effects | Yes — DB writes, events, payments | None — read-only |
| Validation | Always — FluentValidation required | Optional |
| Idempotency | Required for mutation commands | N/A |
| EF Tracking | Default tracking | `AsNoTracking()` |

**Examples:**
- Commands: `CreateOrderCommand`, `CompleteOrderCommand`, `CancelOrderCommand`, `RefundOrderCommand`
- Queries: `GetOrdersQuery`, `GetOrderByIdQuery`, `ListCancelledOrdersQuery`

---

## Command Definition

Commands are immutable `record` types implementing `IRequest<TResult>`.

```csharp
namespace MyApp.Features.Orders.CreateOrder;

public sealed record CreateOrderCommand(
    Guid OrderId,
    decimal Amount,
    string Currency,
    string IdempotencyKey) : IRequest<CreateOrderResult>;
```

### Naming Conventions

- `{Action}{Aggregate}Command` — e.g., `CreateOrderCommand`, `CompleteOrderCommand`
- `{Action}{Aggregate}Query` — e.g., `GetOrdersQuery`
- Use the business language, not technical language (`CreateOrder` not `InsertDatabaseRecord`)

---

## Handler Structure

Handlers are `sealed` classes with a **single responsibility**: orchestrate one use case.

```csharp
namespace MyApp.Features.Orders.CreateOrder;

public sealed class CreateOrderHandler(
    IOrderRepository repository,
    IChargeable paymentProcessor,
    IEventBus eventBus,
    ILogger<CreateOrderHandler> logger) : IRequestHandler<CreateOrderCommand, CreateOrderResult>
{
    public async Task<CreateOrderResult> Handle(
        CreateOrderCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Creating order {OrderId}", request.OrderId);

        var order = await repository.GetByIdAsync(
            request.OrderId, cancellationToken);

        if (order is null)
            return CreateOrderResult.NotFound(request.OrderId);

        var chargeResult = await paymentProcessor.ChargeAsync(
            order, request.Amount, request.Currency,
            request.IdempotencyKey, cancellationToken);

        if (!chargeResult.IsSuccess)
            return CreateOrderResult.PaymentFailed(chargeResult.ErrorMessage);

        order.Process();
        await repository.UpdateAsync(order, cancellationToken);

        await eventBus.PublishAsync(
            new OrderCreatedEvent(order.Id, request.Amount), cancellationToken);

        return CreateOrderResult.Success(order.Id);
    }
}
```

### Handler Rules

- **Inject interfaces only** — never concrete types, never `DbContext`
- **Propagate `CancellationToken`** through every async call
- **Log with structured data** — use correlation IDs, never PII
- **One handler per command/query** — no reuse across slices
- **No business logic** — delegate to domain entities and strategy services

---

## Result DTOs

Use result objects for flow control. **Never throw exceptions for business errors.**

```csharp
namespace MyApp.Features.Orders.CreateOrder;

public sealed record CreateOrderResult
{
    public bool IsSuccess { get; init; }
    public Guid? OrderId { get; init; }
    public string? ErrorMessage { get; init; }
    public CreateOrderErrorCode? ErrorCode { get; init; }

    public static CreateOrderResult Success(Guid orderId) =>
        new() { IsSuccess = true, OrderId = orderId };

    public static CreateOrderResult NotFound(Guid orderId) =>
        new() { IsSuccess = false, ErrorCode = CreateOrderErrorCode.NotFound,
                ErrorMessage = $"Order {orderId} not found." };

    public static CreateOrderResult PaymentFailed(string? reason) =>
        new() { IsSuccess = false, ErrorCode = CreateOrderErrorCode.PaymentFailed,
                ErrorMessage = reason ?? "Payment processing failed." };
}

public enum CreateOrderErrorCode
{
    NotFound,
    PaymentFailed,
    InvalidState,
    DuplicateRequest
}
```

### Result Rules

- Include `IsSuccess` boolean for quick checks
- Include typed `ErrorCode` enum for programmatic handling
- Include `ErrorMessage` for human-readable context
- Static factory methods for each outcome — makes handler code readable
- Never expose domain entities in results — map to DTOs

---

## Pipeline Behaviors

Register cross-cutting concerns as MediatR pipeline behaviors.

### Validation Behavior

Runs FluentValidation before the handler executes:

```csharp
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

### Logging Behavior

Logs request entry/exit with elapsed time:

```csharp
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", requestName);

        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        logger.LogInformation(
            "Handled {RequestName} in {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);

        return response;
    }
}
```

### Registration

```csharp
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<CreateOrderCommand>();
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
});
```

---

## Calling from Blazor Components

**Never call services directly from components.** Always go through MediatR.

```csharp
// ✅ Component code-behind — dispatches through MediatR
public sealed partial class CreateOrderPage : ComponentBase
{
    [Inject] private IMediator Mediator { get; set; } = default!;

    private async Task OnCreateOrderAsync()
    {
        var result = await Mediator.Send(new CreateOrderCommand(
            OrderId: _orderId,
            Amount: _amount,
            Currency: "USD",
            IdempotencyKey: Guid.CreateVersion7().ToString()));

        if (result.IsSuccess)
            NavigationManager.NavigateTo("/orders/dashboard");
        else
            _errorMessage = result.ErrorMessage;
    }
}
```

```csharp
// ❌ VIOLATION — calling infrastructure directly from component
public sealed partial class CreateOrderPage : ComponentBase
{
    [Inject] private IOrderRepository Repository { get; set; } = default!;
    [Inject] private IChargeable PaymentProcessor { get; set; } = default!;

    private async Task OnCreateOrderAsync()
    {
        var order = await Repository.GetByIdAsync(_orderId);
        await PaymentProcessor.ChargeAsync(order, _amount, "USD", _key);
        // VIOLATION — bypasses validation, logging, and event publishing
    }
}
```

---

## Idempotency for Mutation Commands

All commands that trigger external operations **must** include an `IdempotencyKey`:

```csharp
public sealed record CreateOrderCommand(
    Guid OrderId,
    decimal Amount,
    string Currency,
    string IdempotencyKey) : IRequest<CreateOrderResult>;

public sealed record CompleteOrderCommand(
    Guid OrderId,
    string IdempotencyKey) : IRequest<CompleteOrderResult>;
```

- Generate keys client-side using `Guid.CreateVersion7().ToString()`
- Check for existing idempotency key in the handler before processing
- Return the cached result for duplicate requests
- Pass the key to the external provider's API for safe retries

---

## Quick Reference

| Concept | Convention |
|---|---|
| Folder structure | `Features/{Aggregate}/{Action}/{Command,Handler,Validator,Result}.cs` |
| Command naming | `{Verb}{Noun}Command` — `CreateOrderCommand` |
| Query naming | `Get{Noun}Query` — `GetOrdersQuery` |
| Handler class | `sealed class`, primary constructor, inject interfaces |
| Result type | `sealed record` with `IsSuccess`, `ErrorCode`, `ErrorMessage` |
| Validation | FluentValidation `AbstractValidator<TCommand>` per command |
| Pipeline | `ValidationBehavior` → `LoggingBehavior` → Handler |
| Component access | `IMediator.Send()` only — never bypass the pipeline |
| Mutation commands | Always include `IdempotencyKey` property |
| CancellationToken | Propagate through every async call in the chain |
