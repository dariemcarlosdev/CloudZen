---
name: memory-optimization
description: "Context window and token optimization rules — load less, achieve more. Apply to every session."
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  version: "2.0.0"
  domain: workflow
  triggers: optimize context, reduce tokens, context window, memory management, token budget, context optimization
  role: expert
  scope: optimization
  platforms: copilot-cli, claude, gemini
  output-format: guidelines
  related-skills: agent-orchestrator, codebase-explorer, deep-context-generator
---

# Memory & Context Optimization

A workflow skill that enforces context window discipline, progressive file disclosure, and token-efficient search patterns across AI-assisted coding sessions. Apply these rules to every session to maximize useful context while minimizing waste.

## When to Use This Skill

- At the start of every coding session (session priming)
- When context window usage exceeds 50%
- Before delegating work to sub-agents
- When exploring large or unfamiliar codebases
- After receiving a `/compact` suggestion

## Reference Guide

| Topic | Reference File | Load When |
|-------|---------------|-----------|
| File Access Patterns | `references/file-access-patterns.md` | Deciding read order for files during investigation |
| Search Efficiency | `references/search-efficiency.md` | Optimizing grep/glob/LSP usage patterns |
| Sub-Agent Delegation | `references/delegation-rules.md` | Deciding when to delegate vs. do it yourself |
| Session Continuity | `references/session-continuity.md` | Resuming work across sessions, checking history |

## Core Workflow

1. **Assess Context Budget** — Check current token usage. Determine if operating in Normal (<30%), Selective (30–60%), Conservative (60–80%), or Critical (>80%) mode.
   - ✅ Checkpoint: Token budget mode identified before proceeding.

2. **Apply Progressive Disclosure** — Start with the cheapest context sources: docs/ READMEs → interface files → handlers → implementations → tests. Never bulk-read directories.
   - ✅ Checkpoint: Files read in priority order, not randomly.

3. **Optimize Search Patterns** — Use `files_with_matches` for discovery, `count` for scope assessment, then `content` with `-n` on targeted files. Scope searches to relevant directories.
   - ✅ Checkpoint: No global unrestricted grep executed.

4. **Batch Parallel Operations** — Make all independent tool calls in a single response. Never read files one-per-turn when they can be parallelized.
   - ✅ Checkpoint: Independent reads/searches batched in same turn.

5. **Minimize Output Pollution** — Suppress verbose build/test output. Use `--quiet`, `--no-pager`, pipe to `head`. Report summaries, not full logs.
   - ✅ Checkpoint: No full file contents echoed back unnecessarily.

## Constraints

### MUST DO
- Use `view_range` for targeted reads instead of full file reads
- Batch parallel tool calls in a single response
- Use `project_summary` tool for session priming instead of reading multiple files
- Check `docs/` before exploring source code
- Scope grep/glob to relevant directories and file types

### MUST NOT
- Bulk-read entire directories
- Re-read files already seen in the current session (unless modified)
- Echo file contents back to the user after editing
- Run global unrestricted grep across the entire repo
- Read files "just to understand" without a specific question

## Output Template

```
## Context Status
- **Mode:** [Normal | Selective | Conservative | Critical]
- **Files read this session:** [count]
- **Recommendation:** [continue | use view_range | delegate to sub-agent | suggest /compact]
```
