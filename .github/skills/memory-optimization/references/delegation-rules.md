# Sub-Agent Delegation Rules — Deep Dive

## When to Delegate vs. Do It Yourself

| Task | Approach | Why |
|------|----------|-----|
| Read 1-3 known files | Do it yourself | Faster, stays in context |
| Search for a symbol | Do it yourself (grep) | Single tool call |
| Analyze 5+ independent areas | Delegate to explore agents | Parallel, keeps main context clean |
| Complex multi-file refactor | Delegate to general-purpose | Separate context window |
| Run build/tests | Delegate to task agent | Summary only comes back |

## Delegation Context Rules

- **Give complete context** to sub-agents — they don't share your memory
- **Don't duplicate** sub-agent findings by re-reading the same files afterward
- **Trust sub-agent results** for status (pass/fail), verify only if suspicious

## Token Budget per Agent Type

| Agent Type | Model | Context Cost | Use For |
|-----------|-------|-------------|---------|
| explore | Haiku | Low | Research, file discovery |
| task | Haiku | Low | Build/test execution |
| general-purpose | Sonnet | Medium | Multi-step implementation |
| rubber-duck | Sonnet | Medium | Plan/implementation critique |
| code-review | Sonnet | Medium | Change review |

## Parallelization Rules

- Launch independent explore agents in parallel (max 5 per wave)
- Never launch more than one task/general-purpose agent at a time (side effects)
- Use DAG-based dependencies for multi-wave orchestration
