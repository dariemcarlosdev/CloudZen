---
paths:
  - "**/Infrastructure/**/*.cs"
  - "**/Migrations/**/*.cs"
  - "**/*DbContext*.cs"
  - "**/*Repository*.cs"
description: EF Core & PostgreSQL patterns — repositories, queries, migrations, concurrency
---

# EF Core & PostgreSQL Patterns

> Auto-loaded by Claude Code when working with matching files.
> Full reference: `.github/instructions/database/ef-core-patterns.instructions.md`

## PostgreSQL Conventions

- Provider: `Npgsql.EntityFrameworkCore.PostgreSQL`
- Monetary values: `numeric(18,4)` — never `real` or `double precision`
- Primary keys: `uuid` (Guid natively supported)
- Timestamps: `timestamptz` for all `DateTimeOffset` properties
- Semi-structured data: `jsonb` via `.HasColumnType("jsonb")`

## DbContext Rules

- One context: `AppDbContext` — registered **scoped**
- Entity configs via `IEntityTypeConfiguration<T>` in separate files, loaded with `ApplyConfigurationsFromAssembly`
- Configure relationships explicitly — never rely on convention for DDD navigation properties
- Define unique constraints (e.g., `IdempotencyKey`) and indexes (`Status`, `CreatedAt`)

## Repository Pattern

- Interface in Application/Domain layer (`IOrderRepository`)
- Implementation in Infrastructure (`OrderRepository`)
- Return domain entities — mapping to DTOs happens in handlers
- Provide only needed operations: `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `ExistsByIdempotencyKeyAsync`
- Never expose `IQueryable<T>` — it leaks persistence concerns

## Read-Only Queries

- `AsNoTracking()` on **every** read-only query
- Prefer projections with `Select()` over full entity loads
- Use `AsSplitQuery()` when `Include()` chains load multiple collections
- Use compiled queries (`EF.CompileAsyncQuery`) for hot-path lookups

## Migrations

- Descriptive names: `AddIdempotencyKeyIndex`, not `Migration1`
- Keep migrations additive — avoid destructive changes
- Review generated SQL before applying to shared environments
- No seed data or business logic in migrations

## Concurrency

- `Order` uses optimistic concurrency via `UseXminAsConcurrencyToken()`
- Handle `DbUpdateConcurrencyException` in Application layer — retry or return conflict

## Connection Strings

- Never hardcode — use Options pattern with `IOptions<PostgresOptions>`
- Dev: `dotnet user-secrets` | Prod: Azure Key Vault / env vars

## Anti-Patterns

- ❌ `DbContext` in Application/Presentation layers
- ❌ Lazy loading enabled (silent N+1)
- ❌ Returning `IQueryable` from repositories
- ❌ `SaveChanges()` inside repository methods (breaks unit-of-work)
- ❌ String interpolation in raw SQL (injection risk)
- ❌ `Find()`/`FindAsync()` for read-only queries (pollutes change tracker)

---

*Deep-dive: Read `.github/instructions/database/ef-core-patterns.instructions.md` for complete patterns and examples.*
