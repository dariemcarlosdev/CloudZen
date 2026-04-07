---
paths:
  - "**/Commands/**/*.cs"
  - "**/Queries/**/*.cs"
  - "**/Handlers/**/*.cs"
description: CQRS with MediatR — command/query separation, handler structure, pipeline behaviors
---

# CQRS & MediatR Patterns

> Auto-loaded by Claude Code when working with matching files.
> Full reference: `.github/instructions/cqrs/mediatr-patterns.instructions.md`

## Vertical Slice Structure

```
Features/{Domain}/{Action}/
├── {Action}Command.cs          ← IRequest<TResult> (record)
├── {Action}CommandValidator.cs  ← FluentValidation
├── {Action}Handler.cs           ← IRequestHandler<,> (sealed class)
└── {Action}Result.cs            ← Result DTO (sealed record)
```

One command/query, one handler, one result per folder. No shared handlers.

## Command vs Query

| Aspect | Command (Write) | Query (Read) |
|--------|----------------|--------------|
| Naming | `{Verb}{Noun}Command` | `Get{Noun}Query` |
| Returns | Result DTO with `IsSuccess`/`ErrorCode` | DTO or collection |
| Validation | FluentValidation required | Optional |
| EF Tracking | Default | `AsNoTracking()` |
| Idempotency | Required for payment commands | N/A |

## Handler Rules

- `sealed` class with primary constructor — inject **interfaces only**, never `DbContext`
- Propagate `CancellationToken` through every async call
- Log with structured data — correlation IDs, never PII
- Delegate business logic to domain entities and strategy services
- Never throw exceptions for business errors — return typed result DTOs

## Result DTO Pattern

- Include `IsSuccess` boolean, typed `ErrorCode` enum, `ErrorMessage` string
- Static factory methods: `Success(...)`, `NotFound(...)`, `PaymentFailed(...)`
- Never expose domain entities in results — map to DTOs

## Pipeline Behaviors

Registration order: `ValidationBehavior` → `LoggingBehavior` → Handler

```csharp
cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
```

## Calling from Blazor

- Always `IMediator.Send()` — never call services or repositories directly from components
- Generate idempotency keys client-side: `Guid.CreateVersion7().ToString()`

## Forbidden

- ❌ Calling infrastructure directly from components (bypasses validation, logging, events)
- ❌ Sharing handlers across slices
- ❌ Injecting concrete types or `DbContext` in handlers

---

*Deep-dive: Read `.github/instructions/cqrs/mediatr-patterns.instructions.md` for complete patterns and examples.*
