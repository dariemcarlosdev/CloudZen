---
name: commit-changes
description: Commit pending changes to git. Use this when asked to commit, save changes, or make a git commit.
---

## Process

1. Run `git --no-pager status` to see what's changed (staged, unstaged, untracked).
2. If there are no changes, inform the user and stop.
3. Run `git --no-pager diff --stat` and `git --no-pager diff` to understand the changes.
4. If changes span unrelated concerns, split them into **separate atomic commits** — each commit should represent one logical change.
5. Stage files intentionally:
   - Use `git add -A` only when all changes belong to the same logical unit.
   - Use `git add <file>...` to stage specific files for atomic commits.
   - If the user specifies particular files, stage only those.
6. Write a commit message following **Conventional Commits** format (see below).
7. Always include the trailer: `Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>`
8. Confirm the commit hash, branch, and summary to the user.

## Conventional Commits format

```
<type>(<optional scope>): <subject>

<optional body>

<optional footer(s)>
```

### Types

| Type       | When to use                                          |
|------------|------------------------------------------------------|
| `feat`     | New feature or capability                            |
| `fix`      | Bug fix                                              |
| `refactor` | Code change that neither fixes a bug nor adds a feature |
| `style`    | Formatting, whitespace, missing semicolons (no logic change) |
| `docs`     | Documentation only                                   |
| `test`     | Adding or updating tests                             |
| `chore`    | Build process, dependencies, tooling, CI config      |
| `perf`     | Performance improvement                              |
| `ci`       | CI/CD pipeline changes                               |
| `build`    | Build system or external dependency changes          |
| `revert`   | Reverting a previous commit                          |

### Subject line rules

- Use **imperative mood** ("add", not "added" or "adds")
- **Do not** end with a period
- Keep to **50 characters** or less
- Capitalize the first word after the colon
- Must accurately describe what the commit does, not what you were working on

### Body rules

- Separate from subject with a **blank line**
- Wrap lines at **72 characters**
- Explain **what** changed and **why**, not how
- Use bullet points (`-`) for multiple changes

### Footer rules

- `BREAKING CHANGE: <description>` for breaking API/behavior changes (also add `!` after type: `feat!: ...`)
- Reference issues: `Closes #123`, `Fixes #456`, `Refs #789`
- Always end with: `Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>`

## Examples

**Simple feature:**
```
feat(auth): add JWT token refresh endpoint

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

**Multi-change with body:**
```
refactor(services): extract email validation into shared utility

- Move validation logic from ApiEmailService to InputValidator
- Add unit-testable static methods for email format checks
- Remove duplicated regex patterns across services

Closes #42

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

**Breaking change:**
```
feat!(api): change rate limit response from 429 to structured error

BREAKING CHANGE: Rate-limited requests now return a JSON body with
error details instead of a plain 429 status.

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

## Rules

- **Atomic commits**: One logical change per commit. Never bundle unrelated changes.
- **Never commit** secrets, API keys, connection strings, or `local.settings.json`.
- **Never** use `--no-verify` unless explicitly asked.
- **Never** force-push unless explicitly asked.
- **Do not** push after committing unless the user asks.
- **Do not** commit generated files (`bin/`, `obj/`, `node_modules/`) — verify `.gitignore` covers them.
- Always use `git --no-pager` to avoid interactive pager issues.
- If the working tree has both staged and unstaged changes, ask the user what to include before committing.
