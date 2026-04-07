# Acceptance Criteria (Spec-Writer)

Writing testable acceptance criteria in Given/When/Then format.

## Given/When/Then Format

```
Given {precondition or initial state}
When  {action or trigger}
Then  {expected outcome or observable result}
```

### Rules

1. **Given** — Sets up the scenario (state, data, user role)
2. **When** — A single action the user or system performs
3. **Then** — One or more verifiable outcomes
4. **And** — Extends Given, When, or Then (use sparingly)

## Examples for Project Conventions

### Happy Path

```gherkin
Scenario: Buyer creates a new order
  Given a verified buyer is authenticated
  And the buyer has a linked payment method
  When the buyer creates an order for $5,000 USD with seller "seller@example.com"
  Then a new order is created with status "Pending"
  And the seller receives an email notification
  And the order appears in the buyer's dashboard
```

### Error Path

```gherkin
Scenario: Escrow creation fails with insufficient data
  Given a verified buyer is authenticated
  When the buyer submits an order without specifying an amount
  Then the system returns a validation error "Amount is required"
  And no order is created
  And no notification is sent
```

### Authorization

```gherkin
Scenario: Unauthorized user cannot release order funds
  Given an order exists with status "Funded"
  And a user who is not the buyer, seller, or admin is authenticated
  When the user attempts to release order funds
  Then the system returns 403 Forbidden
  And the order status remains "Funded"
  And the attempt is logged in the audit trail
```

### Edge Cases

```gherkin
Scenario: Escrow expires after timeout period
  Given an order was created 30 days ago with status "Pending"
  And the order has not been funded
  When the system runs the expiration job
  Then the order status changes to "Expired"
  And both buyer and seller are notified
```

## Acceptance Criteria Quality Checklist

| Quality | Good Example | Bad Example |
|---------|-------------|-------------|
| Specific | "Returns 404 Not Found" | "Shows an error" |
| Measurable | "Response within 200ms" | "Should be fast" |
| Testable | "Email sent to buyer@..." | "Buyer is notified somehow" |
| Independent | Tests one behavior | Depends on another test running first |
| Atomic | One Given/When/Then | Multiple scenarios crammed together |

## Anti-Patterns

| Anti-Pattern | Problem | Fix |
|-------------|---------|-----|
| "Works correctly" | Untestable | Specify what "correctly" means |
| "Handles edge cases" | Vague | List each edge case explicitly |
| Multiple When clauses | Tests too much | Split into separate criteria |
| Implementation details | Fragile tests | Focus on behavior, not code |
| Missing error paths | Incomplete | Every happy path needs ≥1 error path |

## Coverage Categories

For comprehensive acceptance criteria, cover these categories:

```
1. Happy path — The expected successful flow
2. Validation — Invalid inputs and boundary values
3. Authorization — Correct access control per role
4. Error handling — External service failures, timeouts
5. Edge cases — Empty data, maximum values, concurrent actions
6. Idempotency — Same action performed twice
7. Audit — Actions are logged for compliance
```

## Mapping to Test Types

| Criteria Category | Test Type | Framework |
|------------------|-----------|-----------|
| Happy path | Integration test | xUnit + WebApplicationFactory |
| Validation | Unit test | xUnit + FluentValidation |
| Authorization | Integration test | xUnit + TestAuthHandler |
| Error handling | Unit test | xUnit + Moq |
| Edge cases | Unit + Integration | xUnit |
| Idempotency | Integration test | xUnit |
