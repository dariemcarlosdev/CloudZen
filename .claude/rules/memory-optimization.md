---
paths:
  - "**/*"
description: Context window optimization — load only what's needed, minimize waste
---

# Memory & Context Optimization

> Auto-loaded by Claude Code when working with matching files.
> Full reference: `.github/instructions/memory/memory-optimization.instructions.md`

## Load Only What You Need

- Never bulk-read directories — use `glob`/`grep` to find files first, then read relevant ones
- Use `view_range` for specific line ranges instead of full files
- Prefer `grep` with `files_with_matches` for discovery, then read only matched files
- Batch parallel reads — multiple files in a single tool-call turn

## Avoid Context Pollution

- Suppress verbose output — use `--quiet`, `--no-pager`, pipe to `head`
- Don't re-read files already seen in this session (unless modified)
- On build/test success: report "Build succeeded" / "All N tests passed" — don't paste full logs
- Summarize errors before pasting full stack traces

## Search Efficiency — Progressive Disclosure

1. **Find files** — `glob` or `grep` with `files_with_matches`
2. **Count matches** — `grep` with `count` to assess scope
3. **Read specific matches** — `grep` with `content` and `-n` on targeted files
4. **Deep dive** — `view` with `view_range` on the most relevant result

## File Access Priority

When investigating a feature, read in this order:
1. `docs/{feature}/README.md` — cheapest context
2. Interface/contract files — API surface
3. MediatR command/handler — business flow
4. Implementation — only if needed
5. Tests — only if verifying or writing new tests

## Scoped Searches

Narrow grep/glob to the relevant layer:
- UI → `Components/` | Business logic → `Features/`
- Data access → `Data/` | Payment flow → `Services/Strategies/`
- Domain model → `Models/`, `Events/`

## Token Budget Awareness

| Usage | Action |
|-------|--------|
| < 30% | Read freely |
| 30-60% | Be selective — use `view_range`, prefer summaries |
| 60-80% | Delegate to sub-agents, summarize findings |
| > 80% | Suggest `/compact`, stop reading new files |

## Anti-Patterns

- ❌ Reading entire files just to search them — use grep
- ❌ Exploratory full reads without a specific question
- ❌ Re-reading files you just edited
- ❌ Verbose confirmations — say "Created X" not "Here's the full content"
- ❌ Sequential single-file reads — batch parallel reads
- ❌ Global unrestricted grep — always scope to relevant directories

---

*Deep-dive: Read `.github/instructions/memory/memory-optimization.instructions.md` for complete patterns and examples.*
