# Claude Code Skills

Bridge files that register the project's universal AI skills with Claude Code's `/skills` discovery system. Each subfolder contains a lightweight `SKILL.md` that redirects Claude to the full skill definition in `.github/skills/`.

## At a Glance

| Aspect | Detail |
|--------|--------|
| **Count** | 42 skills across 11 categories |
| **Architecture** | Bridge pattern — `.claude/skills/` → `.github/skills/` (single source of truth) |
| **Invocation** | `/skill-name` in Claude Code (e.g., `/owasp-audit`, `/code-reviewer`) |
| **Relationship** | Same universal skills work in Copilot CLI, Gemini, and any file-reading AI agent |

## How It Works

```
1. User types:        /owasp-audit
2. Claude loads:      .claude/skills/owasp-audit/SKILL.md         (bridge, ~15 lines)
3. Bridge says:       "Read .github/skills/security/owasp-audit/SKILL.md"
4. Claude follows:    Full Core Workflow from the universal skill file
5. On-demand:         Load references/*.md only when the current step needs them
```

## Why Bridges?

- **Single source of truth** — all skill content lives in `.github/skills/`, shared across AI tools.
- **Claude-specific discovery** — bridges register skills with Claude Code's `/skills` list.
- **Minimal overhead** — bridge files are < 20 lines each; no duplicated content.

## Skill Categories (11)

| Category | Skills | Path |
|----------|--------|------|
| `code-quality` | code-reviewer, refactor-planner, code-documenter, debugging-wizard | `.github/skills/code-quality/` |
| `security` | owasp-audit, secret-scanner, threat-modeler, authentication, authorization | `.github/skills/security/` |
| `architecture` | architecture-reviewer, design-pattern-advisor, dependency-analyzer, legacy-modernizer | `.github/skills/architecture/` |
| `testing` | test-generator, tdd-coach, test-coverage-analyzer | `.github/skills/testing/` |
| `database` | schema-reviewer, query-optimizer | `.github/skills/database/` |
| `devops` | ci-cd-builder, deployment-preflight, monitoring-expert, chaos-engineer | `.github/skills/devops/` |
| `documentation` | readme-generator, adr-creator, api-documenter | `.github/skills/documentation/` |
| `research` | codebase-explorer, tech-spike-planner, spec-miner | `.github/skills/research/` |
| `project-management` | spec-writer, issue-creator, feature-forge | `.github/skills/project-management/` |
| `ai` | mcp-developer, prompt-engineer, agent-orchestrator | `.github/skills/ai/` |
| `language` | dotnet-core-expert, csharp-developer | `.github/skills/language/` |

## Creating a New Skill

1. **Create the universal skill** in `.github/skills/{category}/{skill-name}/SKILL.md` with Core Workflow and optional `references/` folder.
2. **Create the bridge** in `.claude/skills/{skill-name}/SKILL.md`:
   ```markdown
   # {Skill Name}
   > Claude Code bridge — read the universal skill for full instructions.
   Read: `.github/skills/{category}/{skill-name}/SKILL.md`
   Follow the Core Workflow steps inside.
   ```
3. The skill appears in Claude Code's `/skills` list automatically.

## Key Rules

- Bridge files must be **minimal** (< 20 lines) — all real content lives in `.github/skills/`.
- **Never duplicate** skill content in the bridge. If the bridge grows, the content belongs upstream.
- Follow **progressive disclosure** — load `references/*.md` only when the current workflow step requires it.
- See `.github/skills/CATALOG.md` for the full skill inventory with descriptions.

## See Also

- `.github/skills/` — Universal skill definitions (source of truth)
- `.github/skills/CATALOG.md` — Full skill catalog with descriptions and categories
- `.claude/rules/` — Always-on behavioral rules for Claude Code
- `.claude/hooks/` — Event-triggered PowerShell scripts for Claude Code
