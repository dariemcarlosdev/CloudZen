---
paths:
  - "**/Infrastructure/**/*.cs"
  - "**/Services/**/*.cs"
  - "**/*HttpClient*.cs"
description: Polly resilience patterns — retry, circuit breaker, timeout, bulkhead for Stripe
---

# Polly Resilience Patterns

> Auto-loaded by Claude Code when working with matching files.
> Full reference: `.github/instructions/resilience/polly-patterns.instructions.md`

## Retry — Stripe API

- Exponential backoff with jitter (avoid thundering herd)
- 3 retries, base delay 1s, exponential multiplier 2x, random jitter 0-1000ms
- Retry on: `429`, `500`, `502`, `503`, `HttpRequestException`, `TimeoutRejectedException`
- Never retry on `4xx` client errors (except `429`) — they will never succeed

## Circuit Breaker — Stripe Availability

- Break after 5 consecutive failures in a 30s sampling window
- Open state for 60s, then half-open (1 probe request)
- Fail fast with `BrokenCircuitException` when open — don't queue
- Log every state transition for operational visibility

## Timeout

- Always pass and honor `CancellationToken` on every async call
- Optimistic timeout: 15s per Stripe API call
- Pessimistic timeout: 30s for entire payment operation
- Handle `TimeoutRejectedException` — return timeout-specific error result

## Bulkhead Isolation

- 10 concurrent executions, queue depth 5
- Return `503 Service Unavailable` with `Retry-After` on rejection
- Separate bulkheads for payment-critical vs. non-critical operations

## Policy Composition

Order (outermost → innermost): **Bulkhead → Circuit Breaker → Retry → Timeout**

## HttpClient Integration

- Use `IHttpClientFactory` with named/typed clients — never `new HttpClient()`
- Attach policies via `.AddPolicyHandler()` in registration chain
- Set `client.Timeout = Timeout.InfiniteTimeSpan` — let Polly control timeout

## Idempotency Keys for Safe Retries

- Every payment mutation must include `Idempotency-Key` header
- Generate deterministically: `{TransactionId}:{Operation}:{Attempt}`
- Stripe honors keys for 24 hours — retries return original response

## Configuration

- Never hardcode policy values — use `IOptions<StripeResilienceOptions>`
- Allow environment-specific overrides (shorter timeouts in tests)

## Fallback

- Define fallback for every policy chain — never let unhandled exceptions propagate
- Log final failure at `Error` level with full context and correlation ID
- Return structured error result — never swallow exceptions

---

*Deep-dive: Read `.github/instructions/resilience/polly-patterns.instructions.md` for complete patterns and examples.*
