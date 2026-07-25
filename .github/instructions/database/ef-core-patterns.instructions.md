---
applyTo: "**/Data/**/*.cs, **/Migrations/**/*.cs"
---

# Entity Framework Core & PostgreSQL Patterns — Project Data Layer

## PostgreSQL-Specific Conventions

- Use `Npgsql.EntityFrameworkCore.PostgreSQL` as the database provider.
- Map C# `decimal` to `numeric(18,4)` for monetary values — never use `real` or `double precision`.
- Use `jsonb` columns for semi-structured data (e.g., metadata dictionaries) via `.HasColumnType("jsonb")`.
- Use `uuid` for primary keys — PostgreSQL handles `Guid` natively.
- Use `timestamptz` for all `DateTimeOffset` properties.
- If project conventions dictate **snake_case** column names, configure via `UseSnakeCaseNamingConvention()` from `EFCore.NamingConventions` — do not rename manually in Fluent API.

## AppDbContext Configuration

One `DbDbContext` class: `AppDbContext` — registered as scoped. Apply entity configurations via `IEntityTypeConfiguration<T>` in separate files, loaded with `modelBuilder.ApplyConfigurationsFromAssembly(...)`. Define unique constraints on `IdempotencyKey` (prevents duplicates) and composite keys as needed. Define indexes on `Status` (filtered queries), `CreatedAt` (time-range queries), and `ExternalPaymentId` (webhook correlation). Configure relationships explicitly in Fluent API — never rely on convention in DDD models.

## Repository Pattern

Define `IOrderRepository` in Application/Domain layer (expresses domain intent). Implementation (`OrderRepository`) lives in Infrastructure/Data and depends on `AppDbContext`. Provide only operations the domain needs: `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `ExistsByIdempotencyKeyAsync`. Return domain entities (DTOs are mapper's job in handlers). Never expose `IQueryable<T>` from repository — leaks persistence concerns into Application layer.

```csharp
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(Order order, CancellationToken ct);
    Task<bool> ExistsByIdempotencyKeyAsync(string key, CancellationToken ct);
}
```

## Read-Only Query Patterns

Use `AsNoTracking()` on every read-only query (eliminates change-tracker overhead). Prefer projections with `Select()` over loading full entities when consumer needs subset. Use compiled queries (`EF.CompileAsyncQuery`) for hot-path lookups (e.g., transaction status).

## Split Queries

When `Include()` chain loads multiple collections, use `.AsSplitQuery()` to avoid Cartesian explosion. Single-collection includes can stay as single query — split only when needed.

## Migration Conventions

Migration names must be **descriptive**: `AddIdempotencyKeyIndex`, `CreateCustomersTable` (never `Migration1`). Always review generated SQL (`dotnet ef migrations script`) before applying to shared environment. Keep migrations **additive** — avoid destructive changes unless behind planned strategy. Never put seed data or business logic in migrations. Use `migrationBuilder.Sql(...)` sparingly — only for DDL that EF cannot express.

## Connection String Management

Never hardcode connection strings in code or `appsettings.json` for production. Use **Options pattern**: bind `PostgresOptions` from configuration, inject `IOptions<PostgresOptions>`. Development: use `dotnet user-secrets` or `appsettings.Development.json`. Production: use environment variables or Azure Key Vault. Configure connection pooling and timeouts explicitly in connection string.

## Concurrency Control

`Order` must use **optimistic concurrency** with `RowVersion` / `xmin` concurrency token. For PostgreSQL, use `xmin` system column via `.UseXminAsConcurrencyToken()`. Handle `DbUpdateConcurrencyException` in Application layer — retry or return conflict result, never silently overwrite.

## Seeding

Use `HasData()` **only** for reference/lookup data: `OrderStatus` enum table, `Currency` codes. Never seed transactional business data. Seed data must be deterministic and idempotent across migration runs.

## Anti-Patterns to Avoid

| Anti-Pattern | Why It's Harmful | Correct Approach |
|---|---|---|
| `DbContext` in Application/Presentation layers | Bypasses repository abstraction, couples layers | Access data only through `IOrderRepository` |
| Lazy loading enabled | Silent N+1 queries, unpredictable performance | Use eager loading with explicit `Include()` |
| Returning `IQueryable` from repository | Leaks persistence concerns, untestable | Return materialized collections or single entities |
| `SaveChanges()` inside repository methods | Breaks unit-of-work boundaries | Call `SaveChangesAsync()` in the handler or via `IUnitOfWork` |
| String interpolation in raw SQL | SQL injection risk | Use `FromSqlInterpolated` or parameterized queries |
| `Find()` / `FindAsync()` for read-only queries | Pollutes change tracker unnecessarily | Use `AsNoTracking().SingleOrDefaultAsync()` |
