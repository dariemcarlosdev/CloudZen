# Session Continuity — Deep Dive

## Session Store Usage

Before starting major work, check session history:

```sql
-- What was done recently in this project?
SELECT s.id, s.summary, s.updated_at
FROM sessions s
WHERE s.cwd LIKE '%CloudZen%'
ORDER BY s.updated_at DESC LIMIT 5;

-- Was this problem solved before?
SELECT content FROM search_index
WHERE search_index MATCH 'keyword1 OR keyword2'
ORDER BY rank LIMIT 10;
```

## Continuity Patterns

- **Check plan.md** at session start — it may contain unfinished work
- **Check todos** — `SELECT * FROM todos WHERE status != 'done'` for pending items
- **Reference previous sessions** when the user says "continue" or "pick up where we left off"

## Context Checkpoint Pattern

For long-running tasks:

1. After completing a logical unit of work, summarize what was done and what's next
2. If context is growing large, proactively use `/compact` to summarize and free space
3. Before `/compact`, ensure all important decisions are captured in the plan or todos

## Token Budget Awareness Thresholds

| Context Usage | Action |
|---------------|--------|
| < 30% | Normal operation — read freely |
| 30-60% | Be selective — use view_range, prefer summaries |
| 60-80% | Conservative — delegate to sub-agents, summarize |
| > 80% | Critical — suggest /compact, stop reading new files |
