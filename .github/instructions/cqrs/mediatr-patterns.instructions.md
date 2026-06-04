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

Commands are immutable `record` types implementing `IRequest<TResult>`. Naming: `{Action}{Aggregate}Command` (e.g., `CreateOrderCommand`, `CompleteOrderCommand`). Use business language, not technical language. Always include `IdempotencyKey` for mutation commands to prevent duplicate submissions on retry.

---

## Handler Structure

Handlers are `sealed` classes with primary constructor injection. Implement `IRequestHandler<TRequest, TResponse>`. Single responsibility: orchestrate one use case. Log with structured data (correlation IDs, never PII). Propagate `CancellationToken` through every async call. Never inject `DbContext` or repositories directly — use interfaces. Return typed result objects for flow control (never throw for business errors).

---

## Result DTOs

Use result objects for flow control. Include `IsSuccess` boolean, typed `ErrorCode` enum, and `ErrorMessage` string. Static factory methods for each outcome make handler code readable. Never expose domain entities in results — map to DTOs.

---

## Pipeline Behaviors

Register cross-cutting concerns as MediatR pipeline behaviors. **Validation Behavior**: runs FluentValidation before handler, throws `ValidationException` if rules fail. **Logging Behavior**: logs request entry/exit with elapsed time via `Stopwatch`. Compose via `cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>))` in Program.cs.

---

## Calling from Blazor Components

**Never call services directly from components.** Always go through MediatR. Component dispatches via `IMediator.Send(new CreateOrderCommand(...))`. On success, navigate or update state. On failure, display `result.ErrorMessage` to user. This ensures validation, logging, and side effects run consistently.

---

## Idempotency for Mutation Commands

All commands that trigger external operations **must** include an `IdempotencyKey` string property. Generate keys client-side using `Guid.CreateVersion7().ToString()`. Check for existing idempotency key in handler before processing. Return cached result for duplicate requests. Pass key to external provider's API for safe retries.

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
