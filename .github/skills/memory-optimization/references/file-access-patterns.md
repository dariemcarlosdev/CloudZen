# File Access Patterns — Deep Dive

## Read Order Priority

When investigating a feature, read files in this order (most context-efficient first):

1. **docs/{feature}/README.md** — High-level understanding, cheapest context
2. **Interface/contract files** — Understand the API surface
3. **MediatR command/handler** — Understand the business flow
4. **Implementation** — Only if you need to understand internals
5. **Tests** — Only if verifying behavior or writing new tests

## Write Order Priority

When implementing, minimize context churn:

1. **Plan first** — Outline changes before opening files
2. **Edit bottom-up** — Domain → Application → Infrastructure → Presentation
3. **Batch edits per file** — Make all edits to one file in a single turn
4. **Don't interleave reads and writes** — Read once, plan edits, apply all

## Cost-Per-Action Estimates

| Action | Relative Context Cost | Notes |
|--------|----------------------|-------|
| `grep` (files_with_matches) | Very Low | Just file paths |
| `glob` | Very Low | Just file paths |
| `grep` (content, 5 matches) | Low | Small snippets |
| `view` (50 lines) | Low | Targeted read |
| `view` (full file, 200 lines) | Medium | Only when necessary |
| `powershell` (build output) | Medium-High | Suppress verbose output |
| `view` (full file, 500+ lines) | High | Avoid — use view_range |
| Multiple full file reads | Very High | Batch and parallelize |

## Anti-Patterns (Never Do These)

- ❌ **Cat-then-grep**: Don't read an entire file just to search it — use grep directly
- ❌ **Exploratory full reads**: Don't read files "just to understand" without a specific question
- ❌ **Re-reading after edit**: Don't view a file you just edited — you know what's in it
- ❌ **Sequential single-file reads**: Don't read files one-per-turn. Batch parallel reads
- ❌ **Ignoring docs/**: Don't explore source code when docs/ has a README for that feature
