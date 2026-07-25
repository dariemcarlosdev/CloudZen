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

Use pattern: **MethodName_Scenario_ExpectedResult**. Example: `CreateOrder_ValidInput_ReturnsSuccess()`, `CreateOrder_InsufficientBalance_ThrowsPaymentException()`.

## Arrange-Act-Assert (AAA)

Every test must have **clearly separated** AAA sections using blank lines and optional comments. Arrange: set up test data with builders. Act: execute single operation. Assert: verify results and mock invocations.

## Unit Tests

### MediatR Handler Tests

Test each command/query handler **in isolation** with mocked dependencies. Mock `IOrderRepository` and all strategy interfaces. Verify correct repository/strategy calls with expected arguments. Test both success and failure paths — assert thrown exceptions.

### Domain Model Tests

Test aggregate root methods directly. Verify domain events raised after state transitions. Verify invariant violations throw expected domain exceptions. Test Value Object validation (negative amounts, empty strings rejected).

### Validation Rule Tests

Test FluentValidation validators independently — call `validator.TestValidateAsync(model)`. Cover required fields, boundary values, format constraints, and cross-field rules.

## Integration Tests

### API / Endpoint Tests

Use `WebApplicationFactory<Program>` to bootstrap application. Override DI registrations to swap real infrastructure with test doubles. Use **Testcontainers** for PostgreSQL (real database). Test full pipeline: routing → binding → validation → handler → persistence → response.

### Database Integration Tests

Verify EF Core mappings, constraints, indexes against real PostgreSQL. Test repository implementations end-to-end: persist, retrieve, verify. Each test class gets **fresh database** (Testcontainers per fixture) — never share mutable state.

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

Use builders for complex domain objects to keep tests readable and decoupled from constructor changes. Builders with fluent interface allow test-specific configuration.

## Coverage Targets

- **Critical business flows** (create, complete, cancel, refund): **>90% line coverage**.
- **Domain model invariants**: **100%** — every state transition path must be tested.
- **API endpoints**: every documented status code (201, 400, 404, 409, 500) must have at least one test.
- Coverage is a guideline, not a goal — a well-tested critical path is more valuable than chasing a vanity metric across utility code.

## General Rules

Tests must be **deterministic** — no dependency on wall-clock time, random data, or external services. Use `CancellationToken.None` in unit tests; integration tests should test cancellation explicitly. Clean up resources in `Dispose` / `IAsyncDisposable`. Run tests in parallel by default — ensure no shared mutable state.
