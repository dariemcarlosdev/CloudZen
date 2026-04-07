---
applyTo: "**/Models/**/*.cs, **/Events/**/*.cs"
---

# Domain-Driven Design Guidelines — Project Domain

## Rich Domain Models

- `Order` is the **aggregate root** — all state mutations flow through its public methods.
- Encapsulate behavior inside the entity: `Process()`, `Complete()`, `Cancel()`, `AddItem()`.
- Never expose public setters. Use factory methods or constructors for creation, behavior methods for transitions.
- Guard every state transition with precondition checks — throw `DomainException` (or a typed subclass) when an invariant is violated.

```csharp
// ✅ Rich model — behavior lives on the entity
public void Cancel(Customer initiator, string reason)
{
    if (Status is not OrderStatus.Processing)
        throw new InvalidOrderStateException(Id, Status, OrderStatus.Processing);

    Status = OrderStatus.Cancelled;
    AddDomainEvent(new OrderCancelledEvent(Id, initiator.Id, reason));
}

// ❌ Anemic — logic scattered across services
order.Status = OrderStatus.Cancelled; // bypasses invariants
```

## Value Objects

- Use Value Objects for concepts that have **no identity** — equality is based on structural value.
- Candidates: `Money` (amount + currency), `Currency`, `EmailAddress`, `PhoneNumber`.
- Implement as `record` or `readonly struct` with self-validation in the constructor.
- Override equality/hash semantics (records do this automatically).

```csharp
public sealed record Money(decimal Amount, Currency Currency)
{
    public Money
    {
        if (Amount < 0) throw new ArgumentOutOfRangeException(nameof(Amount));
    }
}
```

## Aggregate Boundaries

- `Order` is the **aggregate root** for the order lifecycle.
- Child entities (`OrderItem`, line items, etc.) are accessed **only** through the aggregate root — never loaded independently via a repository.
- Persist and load the entire aggregate in a single unit of work to maintain transactional consistency.
- Keep aggregates small — resist the urge to pull unrelated concepts (e.g., user profiles) inside the boundary.

## Domain Events

- Raise events **from within the aggregate** using a base-class `AddDomainEvent()` helper.
- Events are **past-tense facts**: `OrderCreatedEvent`, `OrderCompletedEvent`, `OrderCancelledEvent`.
- Events carry only the data needed by handlers — IDs and relevant state, never full entity graphs.
- Domain events must be **pure data** (no service dependencies, no async calls inside the event itself).
- Dispatch events **after** the aggregate is persisted (outbox pattern or EF Core `SaveChanges` interception) to avoid side effects on rollback.

```csharp
public sealed record OrderCreatedEvent(
    Guid OrderId,
    Money Amount,
    string ExternalReference) : IDomainEvent;
```

## Strategy Interfaces

- Strategy interfaces belong in the **Domain layer** — they define *what* the domain needs, not *how* it's fulfilled.
- `IChargeable` — charge funds from the buyer's payment source.
- `IRefundable` — refund funds to the buyer upon cancellation or return.
- `ICancellable` — void/cancel a pending charge before capture.
- Infrastructure provides concrete implementations (e.g., `StripePaymentProcessor`).
- The aggregate references strategies by interface; the Application layer injects the concrete implementation via DI.

```csharp
// Domain — pure interface
public interface IChargeable
{
    Task<ChargeResult> ChargeAsync(Order order, CancellationToken ct);
}
```

## Entity Invariants

- Validate **in the constructor** — an entity must never exist in an invalid state.
- Use guard clauses at the top of every public method that mutates state.
- Required fields are enforced at construction time, not by external validators.
- Status transitions follow an explicit state machine — document allowed transitions.

```
Created → Processing → Completed | Cancelled
Cancelled → Refunded
```

## Pure Domain — No Framework Dependencies

- Domain classes must be **plain C# POCOs**: no `[Table]`, `[Column]`, `[Required]`, or EF Core attributes.
- No references to MediatR, ASP.NET Core, Entity Framework, or any infrastructure NuGet package.
- Mapping to persistence is handled in the Infrastructure layer via Fluent API (`IEntityTypeConfiguration<T>`).
- Domain events implement a thin marker interface (`IDomainEvent`) defined in the Domain project — not `INotification` from MediatR.

## Participant Model

- `Customer` represents a **participant** in an order (buyer, seller, or other role).
- A participant is an entity within the aggregate — it has identity but is not a standalone aggregate root.
- Store the participant's role, display name, and reference to their authentication identity.
- Participants are associated during aggregate creation — never modified independently.

## Address & Contact Value Objects

- Value objects like `Address` and `EmailAddress` encapsulate validated, identity-less data.
- Modeled as `record` types with self-validation in the constructor.
- Use these to avoid primitive obsession — prefer `EmailAddress` over raw `string` for email fields.
- Validate format in the Value Object; validate existence (e.g., uniqueness) at the Application layer.

## General Rules

- Prefer `Guid` for entity identifiers — generated at creation time, not by the database.
- Use `DateTimeOffset` for all timestamps — never `DateTime`.
- Collections exposed from aggregates must be `IReadOnlyCollection<T>` — mutation only through aggregate methods.
- All domain code must be **synchronous** — async belongs in Application and Infrastructure layers.
