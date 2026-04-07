---
paths:
  - "**/Domain/**/*.cs"
  - "**/Entities/**/*.cs"
  - "**/ValueObjects/**/*.cs"
description: DDD guidelines — rich models, aggregates, value objects, domain events
---

# Domain-Driven Design

> Auto-loaded by Claude Code when working with matching files.
> Full reference: `.github/instructions/domain/ddd-guidelines.instructions.md`

## Rich Domain Models

- `Order` is the **aggregate root** — all state mutations flow through its public methods
- Encapsulate behavior: `HoldFunds()`, `ReleaseFunds()`, `RaiseDispute()`, `Cancel()`
- No public setters — use factory methods/constructors for creation, behavior methods for transitions
- Guard every state transition with precondition checks — throw typed domain exceptions

```
State Machine: Created → FundsHeld → Released | Disputed | Cancelled
               Disputed → Resolved → Released | Refunded
```

## Value Objects

- For concepts with **no identity** — equality based on structural value
- Candidates: `Money` (amount + currency), `Currency`, `IdempotencyKey`, `WalletAddress`
- Implement as `record` or `readonly struct` with self-validation in constructor
- Reject invalid state at construction time (e.g., negative `Money.Amount`)

## Aggregate Boundaries

- `Order` is the sole aggregate root for domain lifecycle
- Child entities (`Actor`, milestones) accessed only through the aggregate root
- Persist and load the entire aggregate in a single unit of work
- Keep aggregates small — don't pull unrelated concepts inside the boundary

## Domain Events

- Raise from within the aggregate via `AddDomainEvent()` helper
- Past-tense facts: `PaymentReceivedEvent`, `DisputeRaisedEvent`, `FundsReleasedEvent`
- Carry only IDs and relevant state — never full entity graphs
- Pure data (no service dependencies, no async calls inside the event)
- Dispatch **after** persistence to avoid side effects on rollback

## Strategy Interfaces

- Belong in the Domain layer — define *what* the domain needs, not *how*
- `IChargeable`, `IRefundable`, `ICancellable`
- Infrastructure provides concrete implementations (e.g., `StripePaymentProcessor`)

## Pure Domain — No Framework Dependencies

- Plain C# POCOs — no `[Table]`, `[Column]`, `[Required]`, no EF Core attributes
- No references to MediatR, ASP.NET Core, or Entity Framework
- Persistence mapping via Fluent API (`IEntityTypeConfiguration<T>`) in Infrastructure
- Domain events use thin `IDomainEvent` marker — not MediatR's `INotification`

## General Rules

- `Guid` for entity identifiers — generated at creation, not by database
- `DateTimeOffset` for all timestamps — never `DateTime`
- Collections: expose `IReadOnlyCollection<T>` — mutate only through aggregate methods
- All domain code must be **synchronous** — async belongs in Application/Infrastructure

---

*Deep-dive: Read `.github/instructions/domain/ddd-guidelines.instructions.md` for complete patterns and examples.*
