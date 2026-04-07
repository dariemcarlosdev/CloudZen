---
applyTo: "**/*Tests*/**/*.cs, **/*Test*/**/*.cs"
---

# Testing Standards — Project Conventions

## Framework & Tooling

- **Test framework:** xUnit — use `[Fact]` for single cases, `[Theory]` with `[InlineData]` or `[MemberData]` for parameterized tests.
- **Assertions:** FluentAssertions — prefer `.Should().Be()`, `.Should().Throw<T>()` over xUnit's `Assert.*`.
- **Mocking:** Moq or NSubstitute — pick one per project, do not mix.
- **Integration:** `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`) for API-level tests.
- **Database:** Testcontainers for PostgreSQL — spin up a real database per test class for integration tests.

## Naming Convention

Use the pattern: **MethodName_Scenario_ExpectedResult**

```csharp
// ✅ Clear intent
public async Task CreateOrder_ValidInput_ReturnsSuccess()
public async Task CreateOrder_InsufficientBalance_ThrowsPaymentException()
public void CancelOrder_OrderNotInActiveState_ThrowsInvalidOrderStateException()
public async Task CreateOrder_DuplicateIdempotencyKey_ReturnsConflict()

// ❌ Vague or undescriptive
public void Test1()
public async Task TestCreateOrder()
```

## Arrange-Act-Assert (AAA)

Every test must have **clearly separated** AAA sections. Use blank lines and optional comments for readability.

```csharp
[Fact]
public async Task CreateOrder_ValidInput_ReturnsSuccess()
{
    // Arrange
    var order = new OrderBuilder()
        .WithStatus(OrderStatus.Created)
        .WithAmount(new Money(500m, Currency.USD))
        .Build();

    var mockRepo = new Mock<IOrderRepository>();
    mockRepo.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(order);

    var mockPayment = new Mock<IChargeable>();
    mockPayment.Setup(s => s.ChargeAsync(order, It.IsAny<CancellationToken>()))
        .ReturnsAsync(OrderResult.Success("pay_123"));

    var handler = new CreateOrderCommandHandler(mockRepo.Object, mockPayment.Object);

    // Act
    var result = await handler.Handle(new CreateOrderCommand(order.Id), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.IsSuccess.Should().BeTrue();
    mockRepo.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
}
```

## Unit Tests

### MediatR Handler Tests

- Test each command/query handler **in isolation** — inject mocked dependencies.
- Mock `IOrderRepository` and all strategy interfaces (`IChargeable`, `IRefundable`, `ICancellable`).
- Verify that the handler calls the correct repository/strategy methods with expected arguments.
- Test both success and failure paths — assert thrown exceptions with FluentAssertions.

### Domain Model Tests

- Test aggregate root methods directly — `Order.Cancel()`, `Order.Complete()`.
- Verify that **domain events** are raised correctly after state transitions.
- Verify that **invariant violations** throw the expected domain exceptions.
- Test Value Object validation: `Money` rejects negative amounts, `IdempotencyKey` rejects empty strings.

### Validation Rule Tests

- Test FluentValidation validators independently — call `validator.TestValidateAsync(model)`.
- Cover required fields, boundary values, format constraints, and cross-field rules.

## Integration Tests

### API / Endpoint Tests

- Use `WebApplicationFactory<Program>` to bootstrap the application.
- Override DI registrations to swap real infrastructure with test doubles where appropriate.
- Use **Testcontainers** for PostgreSQL so integration tests run against a real database engine.
- Test the full request pipeline: routing → model binding → validation → handler → persistence → response.

```csharp
public sealed class OrderApiTests : IClassFixture<AppWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OrderApiTests(AppWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostOrder_ValidPayload_Returns201()
    {
        // Arrange
        var payload = new CreateOrderRequest(/* ... */);

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
```

### Database Integration Tests

- Verify EF Core mappings, constraints, and indexes against a real PostgreSQL instance.
- Test repository implementations end-to-end: persist, retrieve, verify.
- Each test class gets a **fresh database** (Testcontainers per fixture) — never share mutable state across tests.

## What to Test

| Layer | What to Test |
|---|---|
| Domain Models | Constructor validation, behavior methods, state transitions, domain event emission, Value Object equality |
| MediatR Handlers | Business logic orchestration, correct repository/strategy calls, error handling |
| Strategy Implementations | `StripePaymentProcessor` with mocked Stripe SDK, correct PaymentIntent parameters |
| FluentValidation Rules | Required fields, boundary values, format constraints |
| API Endpoints (integration) | Full HTTP request/response cycle, status codes, response bodies, error payloads |

## What NOT to Test

- **EF Core mappings directly** — these are validated by integration tests against a real database.
- **Private methods** — test through the public interface that exercises them.
- **Framework behavior** — do not test that ASP.NET Core routing works or that DI resolves correctly (unless custom logic is involved).
- **Third-party library internals** — mock the boundary, don't test Stripe SDK behavior.

## Test Data — Builder Pattern

Use builders for complex domain objects to keep tests readable and decoupled from constructor changes.

```csharp
public sealed class OrderBuilder
{
    private Guid _id = Guid.NewGuid();
    private OrderStatus _status = OrderStatus.Created;
    private Money _amount = new(100m, Currency.USD);
    private Customer? _buyer;
    private Customer? _seller;

    public OrderBuilder WithStatus(OrderStatus status) { _status = status; return this; }
    public OrderBuilder WithAmount(Money amount) { _amount = amount; return this; }
    public OrderBuilder WithBuyer(Customer buyer) { _buyer = buyer; return this; }
    public OrderBuilder WithSeller(Customer seller) { _seller = seller; return this; }

    public Order Build() => new(_id, _amount, _buyer!, _seller!, _status);
}
```

## Coverage Targets

- **Critical business flows** (create, complete, cancel, refund): **>90% line coverage**.
- **Domain model invariants**: **100%** — every state transition path must be tested.
- **API endpoints**: every documented status code (201, 400, 404, 409, 500) must have at least one test.
- Coverage is a guideline, not a goal — a well-tested critical path is more valuable than chasing a vanity metric across utility code.

## General Rules

- Tests must be **deterministic** — no dependency on wall-clock time, random data, or external services.
- Use `CancellationToken.None` in unit tests; integration tests should test cancellation behavior explicitly.
- Clean up resources in `Dispose` / `IAsyncDisposable` — especially Testcontainers and `HttpClient` instances.
- Run tests in parallel by default (xUnit's default) — ensure no shared mutable state between test classes.
