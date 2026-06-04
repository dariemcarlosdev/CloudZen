---
applyTo: "**/*.cs, **/*.razor, **/*.razor.cs, **/*.razor.css, **/*.ts, **/*.js"
---

# Memory & Context Window Optimization Rules

> Universal rules for all AI models working on this codebase.
> Goal: maximize useful context, minimize waste, maintain continuity across sessions.

## 1. Context Window Discipline

**Load Only What You Need:** Never bulk-read directories; use `glob`/`grep` first, then read only matched files. Use `view_range` for specific line ranges. Batch parallel reads in a single turn. Suppress verbose output (`--quiet`, `--no-pager`, pipe to `head`). Don't re-read files you've seen unless modified. Don't echo content back unless asked. Trim build/test output — report "Build succeeded" not full logs.

**Structured Over Verbose:** Use tables, bullet points, and summaries over prose when reporting findings. Show only relevant code snippets with 5–10 lines context, not entire files.

## 2. Session Priming Strategy

**First Turn Efficiency:** Use `project_summary` tool instead of reading multiple files. Check `docs/` first to find relevant feature documentation. Narrow `grep`/`glob` to layer directories: `Components/` (UI), `Features/` (business logic), `Data/` (access), `Services/Strategies/` (external providers), `Models/` / `Events/` (domain).

**Context Checkpoints:** After completing logical units, summarize what's done. Use `/compact` when context grows large. Before compacting, ensure decisions/findings are captured in plan.md or todos.

## 3. File Access Patterns

**Read Order (Most Efficient First):**
1. `docs/{feature}/README.md` — high-level understanding
2. Interface/contract files — API surface
3. MediatR command/handler — business flow
4. Implementation — only if needed
5. Tests — only for verification

**Write Order:** Plan first. Edit bottom-up (Domain → Application → Infrastructure → Presentation). Batch edits per file in a single turn. Don't interleave reads/writes.

## 4. Search Efficiency

Fast: `grep pattern:"IPaymentService" glob:"**/*.cs" output_mode:"files_with_matches"` (file paths only)

Wasteful: `grep pattern:"IPaymentService" output_mode:"content" -A:50` (loads unnecessary context)

**Progressive Disclosure:** Find files → Count matches (`count`) → Read specific matches (`content` + `-n`) → Deep dive with `view_range`.

## 5. Sub-Agent Delegation

**When to Delegate:** Read 1-3 files yourself. Search symbols yourself. Delegate 5+ independent areas to explore agents (parallel benefit). Delegate complex multi-file refactors to general-purpose agents. Delegate build/test to task agents (summary-only return).

**Context Rules:** Give complete context to sub-agents (no memory sharing). Don't re-read their findings. Trust their status (pass/fail), verify only if suspicious.

## 6. Memory Across Sessions

**Session Store Usage:** Before starting major work, check session history with DuckDB session_store_sql. Find prior approaches to similar problems. Check plan.md for unfinished work. Query todos for pending items (`WHERE status != 'done'`). Reference previous sessions on "continue" requests.

## 7. Token Budget Guidelines

| Context % | Action |
|-----------|--------|
| < 30% | Normal — read freely |
| 30-60% | Selective — use view_range, prefer summaries |
| 60-80% | Conservative — delegate, summarize |
| > 80% | Critical — suggest /compact, stop reading new files |

**Cost Estimates:** `grep`/`glob` = very low. `grep` (5 matches) = low. `view` (50 lines) = low. `view` (200 lines) = medium. `view` (500+ lines) = high. Multiple full reads = very high.

## 8. Anti-Patterns (Never Do These)

Cat-then-grep (use grep directly). Exploratory full reads without specific question. Re-reading edited files. Verbose confirmations (say "Created X" not full content). Sequential single-file reads (batch parallel). Ignoring docs/ when it has a README. Global unrestricted grep (always scope to directory/type).
