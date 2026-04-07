---
paths:
  - "**/*"
description: MVP-first development — ship working software fast, defer non-essentials
---

# MVP-First Development

> Auto-loaded by Claude Code when working with matching files.
> Full reference: `.github/instructions/development/mvp-first.instructions.md`

## Core Principle

**Working software > Perfect architecture.** Filter every decision through:
_"Does this get us closer to a usable product, or is it premature optimization?"_

## MVP Decision Filter

| Question | Answer |
|----------|--------|
| Does the user see/interact with this? | Build it |
| Does the app crash without this? | Build it |
| Is this a security requirement? | Build it |
| Is this "nice to have" for v1? | **Defer it** |
| Building for 10K users when we have 10? | **Stop** |
| Abstracting something used in one place? | **Stop** |

## Build Order for Any Feature

1. Domain model (entity + value objects) — 30 min max
2. Simplest data access (repository interface + implementation)
3. One happy-path MediatR command/query
4. Basic Blazor UI that calls it
5. FluentValidation on the command
6. Basic error handling (try-catch in handler)
7. One integration test (happy path)
8. **✅ SHIP IT** — everything below is v1.1+

## MUST NOT in MVP Phase

- ❌ Generic repositories (`IRepository<T>`) — use specific per aggregate
- ❌ CQRS read models — same EF model for reads/writes until perf proves otherwise
- ❌ Event sourcing, microservices, message queues
- ❌ Custom middleware, abstract factories, specification pattern
- ❌ GraphQL — use REST

## MUST DO in MVP Phase

- ✅ Clean Architecture layers (separation of concerns is free)
- ✅ Interfaces for external services (`IPaymentService`)
- ✅ FluentValidation on every command
- ✅ `[Authorize]` on every endpoint — default deny
- ✅ One happy-path test per feature
- ✅ Code-behind pattern from day one
- ✅ Parameterized queries — never concatenate SQL
- ✅ `ILogger<T>` with structured parameters
- ✅ Dependency injection always

## Rule of Three

Don't abstract until you've written the same pattern **three times**. 1st: inline. 2nd: note duplication. 3rd: extract.

## Red Flags — Stop and Reassess

- Building an admin panel before having users
- Writing a "plugin system" for one implementation
- Debating patterns for >30 minutes
- More interfaces than concrete classes
- Spending more time on infrastructure than features

---

*Deep-dive: Read `.github/instructions/development/mvp-first.instructions.md` for complete patterns and examples.*
