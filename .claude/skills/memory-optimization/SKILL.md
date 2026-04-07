---
name: memory-optimization
description: Context window and token optimization rules — load less, achieve more. Apply to every session.
---

# Memory & Context Optimization

> **Bridge to universal instruction.** The full rules live in
> `.github/instructions/memory/memory-optimization.instructions.md`.

This skill teaches token-efficient AI behavior: progressive disclosure, context budgeting,
selective loading, and output compression. Apply these rules to **every session**.

## Instructions

1. **Read the full rules:** Open `.github/instructions/memory/memory-optimization.instructions.md`
2. **Internalize the 7 sections** — they apply to all tasks, not just specific workflows
3. **Key principles to always follow:**
   - Load only what you need (grep first, then read matched files)
   - Use `view_range` instead of reading entire files
   - Suppress verbose output (`--quiet`, pipe to `head`)
   - Batch parallel reads in a single turn
   - Progressive disclosure: SKILL.md first, references only when needed
   - Token budget awareness: <30% normal, 30-60% selective, 60-80% delegate, >80% compact

## Quick Start

```
Read .github/instructions/memory/memory-optimization.instructions.md
```
