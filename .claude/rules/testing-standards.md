---
paths:
  - "**/*.Tests/**/*.cs"
  - "**/*Test*.cs"
  - "**/*Tests*.cs"
description: Testing standards — AAA pattern, naming, mocking, coverage targets
---

# Testing Standards

> Auto-loaded by Claude Code when working with matching files.
> Full reference: `.github/instructions/testing/testing-standards.instructions.md`

## Framework & Tooling

- **xUnit** — `[Fact]` for single cases, `[Theory]` + `[InlineData]`/`[MemberData]` for parameterized
- **FluentAssertions** — `.Should().Be()`, `.Should().Throw<T>()` over `Assert.*`
- **Moq or NSubstitute** — pick one per project, don't mix
- **WebApplicationFactory** — API-level integration tests
- **Testcontainers** — real PostgreSQL per test class for integration tests

## Naming Convention

**`MethodName_Scenario_ExpectedResult`**

```csharp
HoldFunds_ValidTransaction_ReturnsSuccess()
HoldFunds_InsufficientBalance_ThrowsPaymentException()
CancelOrder_InvalidState_ThrowsInvalidOrderStateException()
```

## Arrange-Act-Assert (AAA)

Every test has clearly separated AAA sections with blank lines between them.

## Unit Tests

### Handler Tests
- Test each handler in isolation with mocked dependencies
- Mock `IOrderRepository` and all strategy interfaces
- Verify correct repository/strategy calls with expected arguments
- Test both success and failure paths

### Domain Model Tests
- Test aggregate root methods directly (`HoldFunds()`, `RaiseDispute()`)
- Verify domain events raised after state transitions
- Verify invariant violations throw expected exceptions
- Test Value Object validation (e.g., `Money` rejects negative amounts)

### Validation Tests
- Test FluentValidation validators independently via `validator.TestValidateAsync(model)`
- Cover required fields, boundary values, format constraints

## Integration Tests

- `WebApplicationFactory<Program>` bootstraps the application
- **Testcontainers** for real PostgreSQL — fresh database per fixture
- Test full pipeline: routing → binding → validation → handler → persistence → response
- Override DI registrations for test doubles where needed

## Test Data — Builder Pattern

Use builders for complex domain objects to keep tests readable:

```csharp
new OrderBuilder()
    .WithStatus(OrderStatus.Created)
    .WithAmount(new Money(500m, Currency.USD))
    .Build();
```

## Coverage Targets

- Critical payment flows (hold, release, cancel, dispute): **>90%**
- Domain model invariants: **100%** — every state transition path tested
- API endpoints: every documented status code has at least one test

## General Rules

- Tests must be **deterministic** — no wall-clock time, random data, or external services
- `CancellationToken.None` in unit tests; test cancellation explicitly in integration tests
- Clean up resources in `Dispose`/`IAsyncDisposable`
- Run tests in parallel (xUnit default) — no shared mutable state between classes
- ❌ Don't test private methods, framework behavior, or third-party internals

---

*Deep-dive: Read `.github/instructions/testing/testing-standards.instructions.md` for complete patterns and examples.*
