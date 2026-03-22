---
name: commit-changes
description: Commit pending changes to git. Use this when asked to commit, save changes, or make a git commit.
---

## Process

1. Run `git --no-pager status` to see what's changed (staged, unstaged, untracked).
2. If there are no changes, inform the user and stop.
3. Run `git --no-pager diff --stat` to get a summary of modifications.
4. Review the changes to understand what was done — read diffs or file contents as needed to write an accurate commit message.
5. Stage all changes with `git add -A` (unless the user specifies particular files).
6. Commit with a clear, conventional commit message:
   - First line: concise summary (imperative mood, ~50 chars)
   - Blank line, then bullet points describing key changes if the commit touches multiple areas
   - Always include the trailer: `Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>`
7. Confirm the commit hash and summary to the user.

## Commit message style

```
Add user authentication middleware

- Implement JWT validation in auth middleware
- Add login/logout endpoints to AuthController
- Register auth services in Program.cs

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

## Rules

- Never use `--no-verify` unless explicitly asked.
- Never force-push unless explicitly asked.
- Do not push after committing unless the user asks.
- Always use `git --no-pager` to avoid interactive pager issues.
