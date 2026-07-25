# Git Hooks

Automated scripts that run on git events (pre-commit, post-commit, etc.).

## At a Glance

| Aspect | Detail |
|--------|--------|
| **Type** | Standard git hooks — not Copilot-specific |
| **Trigger** | Git events (commit, push, merge, etc.) |
| **Location** | Each hook is a folder with its script(s) |
| **Registration** | Via `.git/hooks/` symlinks or a hook manager (husky, lefthook) |

## Current Hooks

| Hook | Trigger | Purpose |
|------|---------|---------|
| `secrets-scanner` | pre-commit | Scans staged files for hardcoded secrets, API keys, and credentials before allowing the commit |

## How to Create a New Hook

1. Create a folder under `.github/hooks/` with a descriptive name.
2. Add the hook script (shell, PowerShell, or any executable).
3. Register the script in `.git/hooks/` — either:
   - Symlink manually: `ln -s ../../.github/hooks/my-hook/run.sh .git/hooks/pre-commit`
   - Use a hook manager like **husky** or **lefthook** for automatic setup.

## Hook Types Reference

| Git Hook | When It Runs |
|----------|-------------|
| `pre-commit` | Before a commit is created — use for linting, secret scanning |
| `commit-msg` | After commit message is entered — use for message format validation |
| `pre-push` | Before pushing to remote — use for build/test verification |
| `post-commit` | After a commit is created — use for notifications |

## Hooks vs. Extensions

| Concern | Git Hooks | Copilot Extensions |
|---------|-----------|-------------------|
| **Trigger** | Git operations (commit, push) | AI assistant session events |
| **Runtime** | Shell / any executable | Node.js (ES modules) |
| **Purpose** | Code quality gates at git time | AI workflow enhancement |
| **Audience** | All developers | AI-assisted development |

These are complementary: hooks enforce rules at commit time, extensions enforce rules during AI-assisted coding.

## See Also

- [`.github/extensions/`](../extensions/) — Copilot CLI extensions (AI-time hooks and tools)
- [`.github/extensions/build-guardian/`](../extensions/build-guardian/) — Build verification (extension-based complement to git hooks)
- [`.github/extensions/security-scanner/`](../extensions/security-scanner/) — OWASP scanning tools (extension-based complement to secrets-scanner)
