---
applyTo: "**/*.cs, **/*.razor, **/*.razor.cs, **/*.razor.css, **/*.ts, **/*.js"
---

# MVP-First Development Rules

> Ship a working product fast. Iterate from there. These rules override perfectionism.

**Core Principle:** Working software > perfect architecture. Every decision should be filtered through: _"Does this get us closer to a usable product, or is it premature optimization?"_

## 1. The MVP Decision Filter

Before implementing anything, ask: Does the user see or interact with this? (YES → build it) Does the app crash without it? (YES → build it) Is it a security requirement? (YES → build it) Is it "nice to have"? (NO) Building for 10K users with 10 users? (NO) Abstracting one-off code? (NO).

## 2. What MVP Means (and Doesn't)

**MVP IS:** Smallest thing delivering user value with end-to-end vertical slice (UI → API → DB). Hardcoded config instead of admin panels. Direct service calls instead of queues. One database instead of microservices. Manual processes if rare.

**MVP IS NOT:** Buggy without error handling. Missing authentication/validation. Unpayable technical debt. Throwaway code (should be improvable, not disposable).

## 3. Build Order for Any Feature

1. Domain model (entity + value objects) — 30 min
2. Simplest data access (EF Core, direct) — Repository interface + implementation
3. One happy-path API endpoint — MediatR command/query
4. Basic UI that calls it — Blazor page with form
5. Basic validation — FluentValidation on command
6. Basic error handling — Try-catch at handler level
7. One integration test — WebApplicationFactory happy path

**✅ SHIP IT — everything below is v1.1+**

8–14: Edge cases, comprehensive tests, performance, UI polish, caching, background jobs, admin dashboards.

## 4. Anti-Over-Engineering Rules

**MUST NOT in MVP Phase:** Generic repositories (use specific per aggregate; generalize at 5+ entities). CQRS read models (same EF model for read/write until performance fails). Event sourcing (use simple updates; sourcing is v2+). Microservices (start modular monolith; extract when bottleneck proven). Message queues (direct calls; queues for cross-service communication). Custom middleware (use built-in ASP.NET). Abstract factories (inject directly; factory at 3+ runtime implementations). Specification pattern (use LINQ; spec at 5+ reusable filters). Custom result types (use IActionResult/exceptions; Result<T> when pattern emerges). GraphQL (use REST; GraphQL at 10+ client variations).

**MUST DO in MVP Phase:** Clean Architecture layers (free, prevents rewrites). Interfaces for external services (swappability). FluentValidation on every command. `[Authorize]` on every endpoint (default deny). One happy-path test per feature. Code-behind pattern (`.razor` + `.razor.cs` from day one). Parameterized queries (never concatenate SQL). Structured logging (`ILogger<T>`). Dependency injection (no `new SomeService()` in business logic).

## 5. The "Rule of Three" for Abstraction

Don't abstract until written the same pattern three times:
- **1st time:** Write inline. Ship it.
- **2nd time:** Note duplication. Ship it.
- **3rd time:** Extract abstraction. You have 3 real examples to design from.

Prevents abstractions for hypothetical futures that never arrive.

## 6. Time-Boxing Decisions

Database choice (15 min → PostgreSQL). Auth provider (15 min → ASP.NET Identity). CSS framework (10 min → Bootstrap or Tailwind). Architecture pattern (10 min → Clean Architecture + MediatR). ORM (5 min → EF Core). Testing framework (5 min → xUnit + FluentAssertions). State management (10 min → Scoped services). API style (5 min → Minimal APIs). Logging (5 min → Serilog). Caching (skip — add when measuring).

## 7. Definition of "Done" for MVP Features

✅ Happy path end-to-end (UI → API → DB → response). ✅ Input validation prevents bad data. ✅ Authentication required (no anonymous business ops). ✅ Basic error handling (friendly message, not stack trace). ✅ One integration test happy path. ✅ No hardcoded secrets. ✅ Code compiles, zero warnings.

❌ NOT DONE: Only works in Swagger, no UI. Happy path but crashes on empty input. Works but bypasses authentication.

## 8. Iteration Cadence

- Sprint 0: Project scaffold, auth, first entity, CI pipeline
- Sprint 1–3: Core features end-to-end + user feedback
- Sprint 4–5: Polish, edge cases, performance, production readiness
- **✅ MVP RELEASE**
- Sprint 6+: Iterate based on real user feedback, not assumptions

## 9. When to Break These Rules

- **Compliance requirements:** PCI-DSS, SOC2 — build regardless of MVP scope
- **Data integrity:** Getting it wrong means data loss/corruption — invest time
- **Security:** Never cut auth, validation, secret management
- **Irreversible decisions:** Database schema choices that hurt to change deserve more thought

## 10. Red Flags You're Over-Engineering

Stop if you catch yourself: Building admin panels before users exist. Writing "plugin systems" for one implementation. Debating architecture patterns >30 minutes. Creating more interfaces than concrete classes. Unit testing trivial getters/setters. Caching layer without measuring response times. Designing for millions on day one. Spending more time on infrastructure than features. Creating NuGet packages for single-project code.
