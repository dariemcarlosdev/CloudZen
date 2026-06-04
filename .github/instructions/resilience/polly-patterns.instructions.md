---
applyTo: "**/Services/**/*.cs, **/Infrastructure/**/*.cs"
---

# Polly Resilience Patterns — External API Integration

## Retry Policies — External API Calls

Use **exponential backoff with jitter** for transient failures. Retry on HTTP `429`, `500`, `502`, `503`, and on `HttpRequestException` / `TimeoutRejectedException`. Start with **3 retries**, base delay 1 second, exponential multiplier 2, plus random jitter. Never retry `4xx` client errors (except `429`) — they indicate invalid requests that won't succeed on retry.

## Circuit Breaker — External API Availability

Break after **5 consecutive failures** in **30-second sampling window**. Stay in **open** state for **60 seconds** before **half-open**. In half-open, allow **one probe request** — succeeds → close, fails → re-open. When open, fail fast with `BrokenCircuitException` — don't queue. Log every state transition for visibility.

## Timeout Policies

**Always** pass and honor `CancellationToken` on every async method in call chain. Apply **optimistic timeout** of **15 seconds** per external API call (cancels underlying `HttpClient`). Apply **pessimistic timeout** of **30 seconds** as outer policy for entire operation. Handle `TimeoutRejectedException` explicitly — return timeout-specific error result.

## Bulkhead Isolation

Limit **concurrent external API operations** to prevent resource exhaustion cascades. Configure bulkhead of **10 concurrent executions** with **queue depth 5** for burst absorption. When rejected, return `503 Service Unavailable` with `Retry-After` header. Use separate bulkheads for critical operations vs. non-critical queries.

## IHttpClientFactory + Polly Integration

Register **named or typed `HttpClient`** via `IHttpClientFactory` — never instantiate manually. Attach policies using `.AddPolicyHandler()`. Compose policies via `Policy.WrapAsync()` — order matters: **Bulkhead → Circuit Breaker → Retry → Timeout** (outermost → innermost).

## Idempotency Keys — Safe Retries

**Every** state-changing mutation (create, capture, refund) must include `Idempotency-Key` header. Generate **deterministically** from domain operation: `{OrderId}:{Operation}:{Attempt}`. Store on aggregate — check for duplicates before initiating. Payment APIs honor idempotency keys for 24h — retries return original response, preventing duplicates.

## Fallback Policy

Define fallback for **every** policy chain — never let unhandled exceptions propagate silently. On failure after retries exhausted, return structured error result with context. Log final failure at `Error` level with exception details, correlation ID, operation context. Never swallow exceptions — fallback must re-throw domain exception or return typed failure.

## Health Checks

Expose circuit breaker state as ASP.NET Core `IHealthCheck`. Report `Degraded` when half-open, `Unhealthy` when open, `Healthy` when closed. Register at `/health/external-api` for infrastructure monitoring. Include circuit state in structured logs for incident correlation.

## Configuration via Options Pattern

Never hardcode policy values (retry count, timeout, concurrency limits). Bind settings from configuration using `IOptions<ExternalApiResilienceOptions>`. Allow environment-specific overrides (shorter timeouts in tests, higher retry counts in production).

## General Rules

- Compose policies in a **PolicyWrap** — do not apply policies ad-hoc in individual service methods.
- Use `Context` to pass correlation IDs and operation metadata through the policy chain for structured logging.
- Test resilience behavior: use Simmy (Polly's chaos engineering library) to inject faults in integration tests.
- Review Polly policy telemetry in production — alert on elevated retry rates or frequent circuit breaks.
