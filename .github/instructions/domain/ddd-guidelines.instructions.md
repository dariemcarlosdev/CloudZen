---
applyTo: "**/Models/**/*.cs, **/Events/**/*.cs"
---

# Domain-Driven Design Guidelines — Project Domain

## Rich Domain Models

`Order` is the **aggregate root** — all state mutations flow through its public methods. Encapsulate behavior: `Process()`, `Complete()`, `Cancel()`, `AddItem()`. Never expose public setters. Use factory methods or constructors for creation, behavior methods for transitions. Guard every state transition with precondition checks — throw domain-specific exceptions when invariants are violated.

## Value Objects

Use Value Objects for concepts with **no identity** — equality based on structural value. Candidates: `Money` (amount + currency), `Currency`, `EmailAddress`, `PhoneNumber`. Implement as `record` or `readonly struct` with self-validation in constructor. Override equality/hash semantics (records do automatically).

## Aggregate Boundaries

`Order` is the **aggregate root** for order lifecycle. Child entities (`OrderItem`, line items) accessed **only** through aggregate root — never loaded independently. Persist and load entire aggregate in single unit of work for transactional consistency. Keep aggregates small — resist pulling unrelated concepts (e.g., user profiles) inside boundary.

## Domain Events

Raise events **from within aggregate** using base-class `AddDomainEvent()` helper. Events are **past-tense facts**: `OrderCreatedEvent`, `OrderCompletedEvent`. Carry only data needed by handlers (IDs and state), never full graphs. Events are pure data — no service dependencies or async calls. Dispatch **after** persistence (outbox pattern or `SaveChanges` interception) to avoid side effects on rollback.

## Strategy Interfaces

Belong in **Domain layer** — define *what* domain needs, not *how* fulfilled. `IChargeable` — charge funds, `IRefundable` — refund on cancellation, `ICancellable` — void pending charge. Infrastructure provides implementations (e.g., `StripePaymentProcessor`). Aggregate references by interface; Application injects concrete via DI.

## Entity Invariants

Validate **in constructor** — entity must never exist in invalid state. Use guard clauses at method entry mutating state. Required fields enforced at construction, not by external validators. Status transitions follow explicit state machine (document allowed transitions).

```
Created → Processing → Completed | Cancelled
Cancelled → Refunded
```

## Pure Domain — No Framework Dependencies

Domain classes must be **plain C# POCOs** — no EF Core attributes. No references to MediatR, ASP.NET Core, Entity Framework, or infrastructure NuGet packages. Mapping to persistence handled in Infrastructure via Fluent API (`IEntityTypeConfiguration<T>`). Domain events implement thin marker interface (`IDomainEvent`) defined in Domain project — not `INotification` from MediatR.

## Participant Model

`Customer` represents **participant** in order (buyer, seller, or role). Entity with identity but not standalone aggregate root. Store role, display name, reference to auth identity. Associated during creation — never modified independently.

## Address & Contact Value Objects

- Value objects like `Address` and `EmailAddress` encapsulate validated, identity-less data.
- Modeled as `record` types with self-validation in the constructor.
- Use these to avoid primitive obsession — prefer `EmailAddress` over raw `string` for email fields.
- Validate format in the Value Object; validate existence (e.g., uniqueness) at the Application layer.

## General Rules

- Prefer `Guid` for entity identifiers — generated at creation, not by database
- Use `DateTimeOffset` for all timestamps — never `DateTime`
- Collections from aggregates must be `IReadOnlyCollection<T>` — mutation only through aggregate methods
- All domain code must be **synchronous** — async belongs in Application and Infrastructure
