# Copilot Pattern-Matched Instructions

Markdown files that inject context into the AI agent when it works on files matching specific glob patterns.

## At a Glance

| Aspect | Detail |
|--------|--------|
| **Mechanism** | `applyTo` frontmatter with glob patterns |
| **Format** | Markdown with YAML frontmatter |
| **Trigger** | Automatically injected when the agent edits/creates a file matching the pattern |
| **Scope** | Per-file-pattern — only loads when relevant files are touched |

## File Format

```markdown
---
applyTo: "**/*.cs"
---

# My Instruction Title

Rules and patterns the AI should follow when working on matching files...
```

## Current Instruction Categories

| Directory | `applyTo` Pattern | Purpose |
|-----------|-------------------|---------|
| `architecture/` | `**/*.cs` | Clean Architecture layer rules and dependency direction |
| `blazor/` | `**/*.razor, **/*.razor.cs, **/*.razor.css` | Code-behind pattern, CSS isolation, component lifecycle |
| `cqrs/` | `EscrowApp/Features/**/*.cs` | MediatR vertical slice structure and handler patterns |
| `database/` | `EscrowApp/Data/**/*.cs, EscrowApp/Migrations/**/*.cs` | EF Core and PostgreSQL conventions |
| `development/` | `**/*` | MVP-first development rules and anti-over-engineering |
| `domain/` | `EscrowApp/Models/**/*.cs, EscrowApp/Events/**/*.cs` | DDD guidelines — rich models, value objects, aggregates |
| `memory/` | `**/*` | Context window optimization and token budget rules |
| `resilience/` | `EscrowApp/Services/**/*.cs, EscrowApp/Infrastructure/**/*.cs` | Polly retry, circuit breaker, timeout patterns |
| `security/` | `**/*.cs, **/*.razor` | OWASP Top 10 security rules for fintech |
| `testing/` | `**/*Tests*/**/*.cs, **/*Test*/**/*.cs` | xUnit + FluentAssertions testing standards |
| `planning.instructions.md` | *(standalone file)* | Planning doc sync trigger |

## How to Create a New Instruction

1. Choose the appropriate category folder (or create a new one for a new concern).
2. Create a `.md` file with `applyTo` frontmatter specifying the glob pattern.
3. Write focused, actionable rules — the AI follows these as constraints.

## Key Rules

- **Keep instructions concise** — they consume context window tokens every time they fire.
- **Use narrow `applyTo` patterns** — `EscrowApp/Features/**/*.cs` is better than `**/*.cs` to avoid prompt bloat.
- **One concern per file** — don't mix security rules with testing standards.
- **Actionable over informational** — write rules the AI can follow, not background essays.
- **Test your patterns** — overly broad patterns cause instructions to fire on unrelated edits.

## How It Works

```
Agent edits EscrowApp/Features/HoldFunds/Handler.cs
    ↓
Pattern match: **/*.cs → architecture/ instructions load
Pattern match: EscrowApp/Features/**/*.cs → cqrs/ instructions load
    ↓
Agent receives both instruction sets as additional context
    ↓
Agent follows the combined rules while generating code
```

## See Also

- [`.github/skills/`](../skills/) — On-demand methodology files (loaded explicitly, not pattern-matched)
- [`.github/extensions/`](../extensions/) — Runtime tools and hooks (code, not instructions)
- [`AGENTS.md`](../../AGENTS.md) — Base instructions for all AI agents (always loaded)
- [`CLAUDE.md`](../../CLAUDE.md) — Claude-specific reasoning guidance
