# README Standards

Template and guidelines for writing module/component READMEs in the the project codebase.

## Module README Template

```markdown
# [Module Name]

One-sentence description of what this module does and its business purpose.

## Overview

2-3 sentences explaining where this module fits in the system architecture,
which Clean Architecture layer it belongs to, and its primary consumers.

## Key Types

| Type | Layer | Purpose |
|------|-------|---------|
| `IOrderService` | Application | Escrow lifecycle operations contract |
| `OrderService` | Infrastructure | Implementation of order operations |
| `CreateOrderCommand` | Application | CQRS command for order creation |
| `EscrowValidator` | Application | FluentValidation rules for order commands |
| `Escrow` | Domain | Escrow aggregate root entity |

## Usage

### Creating an Escrow
```csharp
// Via MediatR
var result = await mediator.Send(new CreateOrderCommand
{
    BuyerId = buyerId,
    SellerId = sellerId,
    Amount = Money.USD(500),
    Description = "Widget purchase",
    Deadline = DateTime.UtcNow.AddDays(30)
}, cancellationToken);
```

### Querying Escrows
```csharp
var order = await mediator.Send(new GetOrderQuery(orderId), ct);
var list = await mediator.Send(new ListEscrowsQuery { Status = OrderStatus.Funded }, ct);
```

## Configuration

```json
{
  "Escrow": {
    "MaxAmount": 1000000,
    "TimeoutDays": 30,
    "FeeRate": 0.025,
    "MinimumFee": 1.00
  }
}
```

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `MaxAmount` | decimal | 1000000 | Maximum order amount in base currency |
| `TimeoutDays` | int | 30 | Days before unfunded order expires |
| `FeeRate` | decimal | 0.025 | Platform fee as percentage of amount |
| `MinimumFee` | decimal | 1.00 | Minimum platform fee charged |

## Dependencies

**This module depends on:**
- `Domain` — Escrow aggregate, value objects
- `Application.Contracts` — Shared interfaces and DTOs
- `Infrastructure.Persistence` — EF Core DbContext

**Consumed by:**
- `WebApi` — REST endpoints for order operations
- `Blazor.Server` — Escrow management dashboard components
- `BackgroundWorkers` — Escrow timeout and auto-release jobs

## Testing

```bash
dotnet test --filter "Category=Escrow"
```

| Test Project | Count | Type |
|-------------|-------|------|
| `Escrow.UnitTests` | 45 | Unit |
| `Escrow.IntegrationTests` | 12 | Integration |
| `Escrow.ApiTests` | 8 | Acceptance |
```

## README Quality Checklist

- [ ] **Title** — Module name matches namespace/folder
- [ ] **One-liner** — Clear purpose statement in first line
- [ ] **Architecture context** — Which layer, what consumes it
- [ ] **Key types table** — Most important types with purposes
- [ ] **Usage examples** — Copy-pasteable code that works
- [ ] **Configuration** — All settings documented with defaults
- [ ] **Dependencies** — Both "depends on" and "consumed by"
- [ ] **Testing** — How to run tests, test count summary
- [ ] **No stale content** — Examples match current API signatures

## Component README (Blazor)

For Blazor components, add these sections:

```markdown
## Component: EscrowDashboard

### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `UserId` | `Guid` | Yes | Current user's ID for filtering |
| `ShowClosed` | `bool` | No | Include closed orders (default: false) |
| `OnEscrowSelected` | `EventCallback<EscrowId>` | No | Fires when user clicks an order row |

### Scoped CSS

Component uses `EscrowDashboard.razor.css` for isolated styling.
Override with `::deep` only when embedding in a parent layout.

### Authorization

Requires `[Authorize(Policy = "EscrowViewer")]` — users must have
the `order:read` claim.
```

## Anti-Patterns in READMEs

| ❌ Anti-Pattern | ✅ Better |
|----------------|----------|
| "See code for details" | Document the API surface and key behaviors |
| Stale examples that don't compile | Keep examples in sync; consider tests for examples |
| Documenting every private method | Focus on public API and key concepts |
| No configuration section | Always document required config keys |
| Missing dependency information | Explicitly state what this module needs and provides |
