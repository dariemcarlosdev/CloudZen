# Copilot CLI Project Configuration — Setup Guide

> How to replicate this AI development infrastructure in any .NET project.
> This guide walks through every component, explains its purpose, and provides
> templates you can adapt.

## Overview

This configuration system gives AI assistants (Copilot, Claude, Gemini) deep
project knowledge through layered instruction files, custom extensions with
hooks and tools, MCP server integrations, and LSP configuration.

### What You Get

| Layer | Purpose | Files |
|-------|---------|-------|
| **Model Instructions** | Project identity, per-model optimization | `AGENTS.md`, `CLAUDE.md`, `GEMINI.md` |
| **Master Rules** | Technology stack, architecture, conventions | `.github/copilot-instructions.md` |
| **Scoped Instructions** | Domain-specific rules activated by file glob | `.github/instructions/**/*.instructions.md` |
| **Skills Catalog** | Reusable AI skills with lazy-loaded references | `.github/skills/{category}/{skill}/SKILL.md` |
| **Extensions** | Custom tools, hooks, and real-time behaviors | `.github/extensions/*/extension.mjs` |
| **MCP Servers** | External tool integrations (DB, APIs) | `.github/copilot-mcp.json` |
| **LSP Config** | Language server for code intelligence | `.github/lsp.json` |
| **Cloud Agent** | CI environment for Copilot coding agent | `.github/copilot-setup-steps.yml` |

---

## Step 1: Create the Directory Structure

```bash
# From your solution/repo root:
mkdir -p .github/instructions/{architecture,security,testing,resilience,memory,development}
mkdir -p .github/instructions/{blazor,cqrs,database,domain}
mkdir -p .github/extensions/{security-scanner,build-guardian,context-optimizer}
mkdir -p .github/extensions/{research-first,doc-sync,dotnet-conventions}
mkdir -p .github/skills/{code-quality,security,architecture,testing}
mkdir -p .github/skills/{database,devops,documentation,research}
mkdir -p .github/skills/{project-management,ai,language}
# Claude Code bridge (for /skills discovery)
mkdir -p .claude/skills
```

On Windows (PowerShell):

```powershell
$dirs = @(
    # Instructions (scoped rules)
    ".github\instructions\architecture",
    ".github\instructions\security",
    ".github\instructions\testing",
    ".github\instructions\resilience",
    ".github\instructions\memory",
    ".github\instructions\development",
    ".github\instructions\blazor",
    ".github\instructions\cqrs",
    ".github\instructions\database",
    ".github\instructions\domain",
    # Extensions (hooks + tools)
    ".github\extensions\security-scanner",
    ".github\extensions\build-guardian",
    ".github\extensions\context-optimizer",
    ".github\extensions\research-first",
    ".github\extensions\doc-sync",
    ".github\extensions\dotnet-conventions",
    # Skills catalog (11 categories)
    ".github\skills\code-quality",
    ".github\skills\security",
    ".github\skills\architecture",
    ".github\skills\testing",
    ".github\skills\database",
    ".github\skills\devops",
    ".github\skills\documentation",
    ".github\skills\research",
    ".github\skills\project-management",
    ".github\skills\ai",
    ".github\skills\language"
)
# Add Claude Code bridge directory
$dirs += ".claude\skills"
foreach ($d in $dirs) {
    New-Item -ItemType Directory -Path $d -Force | Out-Null
}
```

Add domain-specific instruction folders as needed (e.g., `blazor/`, `cqrs/`,
`database/`, `domain/`).

### Final Structure

```
your-project/
├── AGENTS.md
├── CLAUDE.md
├── GEMINI.md
├── .github/
│   ├── copilot-instructions.md
│   ├── copilot-mcp.json
│   ├── copilot-setup-steps.yml
│   ├── lsp.json
│   ├── SETUP-GUIDE.md                          ← This file
│   ├── instructions/
│   │   ├── architecture/
│   │   │   └── clean-architecture.instructions.md
│   │   ├── security/
│   │   │   └── owasp-top10.instructions.md
│   │   ├── testing/
│   │   │   └── testing-standards.instructions.md
│   │   ├── resilience/
│   │   │   └── polly-patterns.instructions.md
│   │   ├── memory/
│   │   │   └── memory-optimization.instructions.md
│   │   ├── development/
│   │   │   └── mvp-first.instructions.md       ← MVP anti-over-engineering rules
│   │   ├── blazor/
│   │   │   └── component-patterns.instructions.md
│   │   ├── cqrs/
│   │   │   └── mediatr-patterns.instructions.md
│   │   ├── database/
│   │   │   └── ef-core-patterns.instructions.md
│   │   ├── domain/
│   │   │   └── ddd-guidelines.instructions.md
│   │   └── {your-domain}/
│   │       └── {your-rules}.instructions.md
│   ├── skills/                                  ← Reusable AI skills catalog
│   │   ├── CATALOG.md                           ← Master index of all skills
│   │   ├── code-quality/
│   │   │   ├── code-reviewer/
│   │   │   │   ├── SKILL.md                     ← Lean core (4-6 KB)
│   │   │   │   └── references/                  ← Deep-dive files (2-4 KB each)
│   │   │   │       ├── review-checklist.md
│   │   │   │       ├── common-issues.md
│   │   │   │       └── ...
│   │   │   ├── refactor-planner/
│   │   │   ├── code-documenter/
│   │   │   └── debugging-wizard/
│   │   ├── security/
│   │   │   ├── owasp-audit/
│   │   │   ├── secret-scanner/
│   │   │   ├── threat-modeler/
│   │   │   ├── authentication/                  ← Auth patterns (Entra ID, JWT, OIDC)
│   │   │   └── authorization/                   ← AuthZ patterns (policies, claims, Blazor)
│   │   ├── architecture/
│   │   ├── testing/
│   │   ├── database/
│   │   ├── devops/
│   │   ├── documentation/
│   │   ├── research/
│   │   ├── project-management/
│   │   ├── ai/                                  ← Agent orchestration, MCP, prompts
│   │   └── language/                            ← .NET Core, C# deep expertise
│   └── extensions/
│       ├── security-scanner/
│       │   └── extension.mjs
│       ├── build-guardian/
│       │   └── extension.mjs
│       ├── context-optimizer/
│       │   └── extension.mjs
│       ├── research-first/
│       │   └── extension.mjs
│       ├── doc-sync/
│       │   └── extension.mjs
│       └── dotnet-conventions/
│           └── extension.mjs
├── .claude/                                    ← Claude Code specific
│   ├── settings.json
│   ├── skills/                                 ← Bridge files for /skills discovery
│   │   ├── code-reviewer/SKILL.md              ← Bridges to .github/skills/code-reviewer/
│   │   ├── owasp-audit/SKILL.md                ← Bridges to .github/skills/owasp-audit/
│   │   ├── agent-orchestrator/SKILL.md         ← Bridges to .github/skills/agent-orchestrator/
│   │   └── ... (37 total — one per universal skill)
│   └── rules/                                  ← Scoped rules (auto-loaded by file path)
│       ├── clean-architecture.md               ← paths: ["**/*.cs"]
│       ├── blazor-components.md                ← paths: ["**/*.razor", "**/*.razor.cs"]
│       ├── cqrs-mediatr.md                     ← paths: ["**/Commands/**", "**/Queries/**"]
│       ├── ef-core.md                          ← paths: ["**/Infrastructure/**"]
│       ├── owasp-security.md                   ← paths: ["**/*.cs", "**/*.razor"]
│       ├── memory-optimization.md              ← paths: ["**/*"] (always active)
│       ├── mvp-first.md                        ← paths: ["**/*"] (always active)
│       ├── ddd-domain.md                       ← paths: ["**/Domain/**"]
│       ├── polly-resilience.md                 ← paths: ["**/Services/**"]
│       └── testing-standards.md                ← paths: ["**/*Test*"]
└── YourProject.sln
```

---

## Step 2: Model Instruction Files (Root)

These files sit at the repo root and are auto-discovered by Copilot CLI.

### `AGENTS.md` — Universal Agent Instructions

This is the **primary identity file** all AI models read. Include:

```markdown
# Project Name — Agent Instructions

## Project Identity
- What the project does (1-2 sentences)
- Target users / domain

## Architecture
- Pattern (Clean Architecture, Vertical Slice, etc.)
- Layer map with directory names
- Dependency flow diagram (ASCII)

## Mandatory Rules
- [ ] List non-negotiable rules (e.g., "always use code-behind")
- [ ] Security requirements
- [ ] Documentation sync requirements

## Key Design Patterns
- List patterns in use with where they're applied

## Anti-Patterns
- What NOT to do (with brief justification)
```

### `CLAUDE.md` / `GEMINI.md` — Model-Specific Optimization

Tailor prompting patterns per model's strengths:

| Model | Emphasize |
|-------|-----------|
| **Claude** | Structured reasoning, chain-of-thought, systematic OWASP enumeration |
| **Gemini** | Code search, dependency graph mapping, cross-reference analysis |

---

## Step 3: Master Copilot Instructions

### `.github/copilot-instructions.md`

This is the **single most important file** — Copilot reads it for every session.

Template structure:

```markdown
# {Project Name} — Copilot Instructions

## Project Overview
[1-paragraph description]

## Technology Stack
| Technology | Version | Purpose |
|------------|---------|---------|
| .NET       | 10      | Runtime |
| ...        | ...     | ...     |

## Architecture
[Layer diagram, dependency rules]

## File Organization
| Directory | Layer | Contents |
|-----------|-------|----------|
| ...       | ...   | ...      |

## Conventions
- Naming, formatting, patterns

## Domain Rules
- Business-specific rules the AI must follow
```

**Keep it under 10 KB** — this loads into every conversation.

---

## Step 4: Scoped Instruction Files

These activate **only when the AI touches matching files**, keeping context lean.

### Format

Each file needs a glob at the top:

```markdown
---
applyTo: "**/*.cs"
---

# Rule Title

## Rules
...
```

### Recommended Scopes for .NET Projects

| File | Glob | Purpose |
|------|------|---------|
| `architecture/*.instructions.md` | `**/*.cs` | Layer dependency rules |
| `blazor/*.instructions.md` | `**/*.razor*` | Component patterns |
| `security/*.instructions.md` | `**/*.cs, **/*.razor` | OWASP rules |
| `testing/*.instructions.md` | `**/*Test*/**` | Test conventions |
| `database/*.instructions.md` | `**/Data/**, **/Migrations/**` | EF Core patterns |
| `resilience/*.instructions.md` | `**/Services/**, **/Infrastructure/**` | Polly patterns |
| `memory/*.instructions.md` | `**/*` | Context window optimization |

### Adapting for Non-.NET Projects

| Stack | Suggested Scopes |
|-------|-----------------|
| **React/TypeScript** | `components/`, `hooks/`, `api/`, `store/`, `**/*.test.ts` |
| **Python/Django** | `models/`, `views/`, `serializers/`, `tests/`, `migrations/` |
| **Go** | `cmd/`, `internal/`, `pkg/`, `**/*_test.go` |
| **Java/Spring** | `controller/`, `service/`, `repository/`, `**/test/**` |

---

## Step 5: Extensions (Skills, Hooks, Agents)

Extensions are Node.js ES modules (`.mjs`) that run as child processes.

### Anatomy of an Extension

```javascript
import { joinSession } from "@github/copilot-sdk/extension";

const session = await joinSession({
    hooks: {
        // Intercept and modify behavior at lifecycle points
        onUserPromptSubmitted: async (input) => { /* ... */ },
        onPreToolUse: async (input) => { /* ... */ },
        onPostToolUse: async (input) => { /* ... */ },
        onSessionStart: async (input) => { /* ... */ },
    },
    tools: [
        // Custom tools the AI can invoke
        {
            name: "my_tool",
            description: "What it does",
            parameters: { type: "object", properties: { /* ... */ } },
            handler: async (args) => "result string",
        },
    ],
});
```

### Extension Catalog — What to Include

| Extension | Transferable? | Adapt For |
|-----------|--------------|-----------|
| **security-scanner** | ✅ Universal | Adjust secret patterns per stack |
| **build-guardian** | ✅ Change build command | `npm run build`, `go build`, `mvn package` |
| **context-optimizer** | ✅ Update project summary | Change the hardcoded summary text |
| **research-first** | ✅ Universal | Adjust docs/ path if different |
| **doc-sync** | ⚠️ Project-specific | Rewrite feature→docs mapping |
| **dotnet-conventions** | ❌ .NET only | Replace with eslint/pylint/golint hooks |

### Adapting `build-guardian` for Other Stacks

```javascript
// Node.js/TypeScript
const buildCmd = isWindows ? "npm.cmd" : "npm";
const buildArgs = ["run", "build"];
const testCmd = isWindows ? "npm.cmd" : "npm";
const testArgs = ["run", "test"];

// Go
const buildCmd = "go";
const buildArgs = ["build", "./..."];
const testCmd = "go";
const testArgs = ["test", "./..."];

// Python
const buildCmd = "python";
const buildArgs = ["-m", "pytest"];
```

### Critical Rules for Extensions

1. **Tool names must be globally unique** across all extensions
2. **Never use `console.log()`** — stdout is JSON-RPC. Use `session.log()`
3. **Only `.mjs` files** — TypeScript not supported
4. **`@github/copilot-sdk` auto-resolves** — don't `npm install` it
5. **Reload after changes**: use `/clear` or the `extensions_reload` command

---

## Step 6: Skills Catalog (Reusable AI Skills)

The skills catalog provides **reusable, cross-platform AI skills** that work with Copilot CLI,
Claude, and Gemini. Each skill follows the **Jeffallan `references/` pattern** for memory optimization.

### What is a Skill?

A skill is a structured markdown file (`SKILL.md`) that tells AI assistants *how* to perform
a specific task — code review, security audit, test generation, etc. Skills include:

- **YAML frontmatter** — metadata, triggers, platform targeting
- **Core Workflow** — numbered steps with validation checkpoints
- **Reference Guide** — lazy-loaded deep-dive files (memory optimization)
- **Constraints** — MUST DO / MUST NOT DO rules
- **Output Template** — expected deliverable format

### The `references/` Pattern (Memory Optimization)

This is the key innovation for token savings. Instead of one large SKILL.md file:

```
# WITHOUT references/ (old pattern — 15-18 KB loaded every time)
skills/owasp-audit/SKILL.md    ← 17 KB monolithic file

# WITH references/ (new pattern — 5 KB base + surgical deep-dives)
skills/owasp-audit/
  SKILL.md                      ← 5 KB core (always loaded)
  references/
    injection-prevention.md     ← 3 KB (loaded ONLY when doing SQL injection work)
    broken-auth.md              ← 3 KB (loaded ONLY when doing auth review)
    access-control.md           ← 3 KB (loaded ONLY when doing access control)
    crypto-failures.md          ← 3 KB (loaded ONLY when doing crypto review)
```

**Result: ~60-70% token savings per skill invocation.**

The SKILL.md includes a **Reference Guide table** that tells the AI *when* to load each file:

```markdown
## Reference Guide

| Topic | Reference | Load When |
|-------|-----------|-----------|
| Injection Prevention | `references/injection-prevention.md` | SQL injection, XSS, command injection |
| Authentication | `references/broken-auth.md` | Auth failures, session management |
| Access Control | `references/access-control.md` | Broken access control (A01) |
| Crypto Failures | `references/crypto-failures.md` | Data exposure, weak encryption |
```

### SKILL.md Format

```yaml
---
name: skill-name
description: "What this skill does and when to invoke it"
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: YourOrg
  version: "2.0.0"
  domain: code-quality              # category
  triggers: review code, PR review   # activation phrases
  role: specialist                   # specialist | reviewer | expert
  scope: review                      # review | implementation | analysis | design
  platforms: copilot-cli, claude, gemini
  output-format: report              # report | code | document | analysis
  related-skills: refactor-planner, test-generator
---

# Skill Name

One-sentence role definition.

## When to Use This Skill
- Trigger scenario 1
- Trigger scenario 2

## Core Workflow
1. **Step** — Description. _Checkpoint: verify X before proceeding._
2. **Step** — Description.

## Reference Guide
| Topic | Reference | Load When |
|-------|-----------|-----------|
| Topic 1 | `references/topic-1.md` | When doing X |

## Quick Reference
(1-2 inline code examples)

## Constraints
### MUST DO
### MUST NOT DO

## Output Template
(Expected deliverable structure)
```

### Skill Categories (11)

| # | Category | Skills | Description |
|---|----------|--------|-------------|
| 1 | `code-quality` | code-reviewer, refactor-planner, code-documenter, debugging-wizard | Code review, refactoring, documentation, debugging |
| 2 | `security` | owasp-audit, secret-scanner, threat-modeler, authentication, authorization | Security audit, secrets, threats, auth |
| 3 | `architecture` | architecture-reviewer, design-pattern-advisor, dependency-analyzer, legacy-modernizer | Architecture review, patterns, dependencies, migration |
| 4 | `testing` | test-generator, tdd-coach, test-coverage-analyzer | Test generation, TDD, coverage |
| 5 | `database` | schema-reviewer, query-optimizer | Schema review, query optimization |
| 6 | `devops` | ci-cd-builder, deployment-preflight, monitoring-expert, chaos-engineer | CI/CD, deployment, monitoring, resilience |
| 7 | `documentation` | readme-generator, adr-creator, api-documenter | README, ADR, API docs |
| 8 | `research` | codebase-explorer, tech-spike-planner, spec-miner | Exploration, spikes, reverse engineering |
| 9 | `project-management` | spec-writer, issue-creator, feature-forge | Specs, issues, requirements |
| 10 | `ai` | mcp-developer, prompt-engineer, agent-orchestrator | MCP, prompts, agent coordination |
| 11 | `language` | dotnet-core-expert, csharp-developer | .NET Core, C# deep expertise |

### Creating a New Skill

```bash
# 1. Create skill directory with references
mkdir -p .github/skills/{category}/{skill-name}/references

# 2. Create SKILL.md with frontmatter + sections
# 3. Create 3-5 reference files for deep-dive content
# 4. Update CATALOG.md master index
```

### Adapting Skills for Your Stack

Skills are stack-agnostic by design. To adapt for a different stack:

1. **Keep the workflow and constraints** — they're universal
2. **Replace code examples** — swap C# for Python/Go/Java in Quick Reference
3. **Update reference files** — replace `.NET` patterns with your framework's equivalents
4. **Update triggers** — match your team's vocabulary

| Original (.NET) | React/TypeScript | Python/Django | Go |
|-----------------|------------------|---------------|----|
| `xUnit + Moq` | `Jest + React Testing Library` | `pytest + unittest.mock` | `testing + testify` |
| `EF Core` | `Prisma / Drizzle` | `Django ORM` | `GORM / sqlc` |
| `FluentValidation` | `Zod / Yup` | `Pydantic / marshmallow` | `go-playground/validator` |
| `MediatR` | `tRPC` | `Django signals` | `Go channels` |
| `Blazor AuthorizeView` | `Next-Auth + middleware` | `Django permissions` | `casbin` |

---

## Step 6B: Claude Code Bridge Skills (`.claude/skills/`)

Claude Code discovers skills from `.claude/skills/`, not `.github/skills/`. To make all
universal skills appear in Claude's `/skills` menu, create **bridge files** that redirect
to the universal definitions.

### Why a Bridge?

```
.github/skills/   ← Universal source of truth (all models)
.claude/skills/   ← Claude Code discovery layer (bridge files only)
```

- **Single source of truth** stays in `.github/skills/`
- Bridge files are thin (~20 lines) — just YAML frontmatter + read instruction
- `/skills` in Claude Code shows all 36 skills
- Users invoke via `/skill-name` (e.g., `/owasp-audit`, `/code-reviewer`)

### Bridge File Template

Create `.claude/skills/{skill-name}/SKILL.md` for each skill:

```markdown
---
name: {skill-name}
description: {Brief description — shown in /skills listing}
---

# {Skill Display Name}

> **Bridge to universal skill catalog.** The full skill definition lives in
> `.github/skills/{category}/{skill-name}/SKILL.md`.

## Instructions

1. **Read the full skill:** Open `.github/skills/{category}/{skill-name}/SKILL.md`
2. **Follow the Core Workflow** steps defined in that file
3. **Load references on demand** from `.github/skills/{category}/{skill-name}/references/`
4. **Never load all references at once** — progressive disclosure saves tokens
```

### Automation Script (PowerShell)

Generate all bridge files from your skills catalog:

```powershell
# Define skills: @{ name="skill-name"; cat="category"; desc="description" }
$skills = @(
    @{ name="code-reviewer"; cat="code-quality"; desc="Review code for correctness and style" },
    @{ name="owasp-audit"; cat="security"; desc="Audit code against OWASP Top 10" },
    # ... add all your skills
)

foreach ($s in $skills) {
    $dir = ".claude\skills\$($s.name)"
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    @"
---
name: $($s.name)
description: $($s.desc)
---
# $($s.name)
> Bridge to universal skill catalog.
> Full definition: ``.github/skills/$($s.cat)/$($s.name)/SKILL.md``
## Instructions
1. Read ``.github/skills/$($s.cat)/$($s.name)/SKILL.md``
2. Follow the Core Workflow steps
3. Load references on demand from ``references/``
"@ | Set-Content "$dir\SKILL.md" -Encoding UTF8
}
```

### Bash equivalent:

```bash
# For each skill, create the bridge:
for skill_name in code-reviewer owasp-audit agent-orchestrator; do
    mkdir -p ".claude/skills/${skill_name}"
    cat > ".claude/skills/${skill_name}/SKILL.md" << 'EOF'
---
name: SKILL_NAME
description: SKILL_DESC
---
# Bridge — read .github/skills/{category}/{skill}/SKILL.md
EOF
done
```

### Verification

After creating bridge files, run `/skills` in Claude Code — all 37 skills should appear.

---

## Step 6C: Claude Code Scoped Rules (`.claude/rules/`)

Claude Code's equivalent of Copilot's `.github/instructions/` scoped instructions.
Rules in `.claude/rules/` are **auto-loaded** when Claude touches files matching the `paths:` globs.

### How It Maps

| Copilot (`.github/instructions/`) | Claude Code (`.claude/rules/`) | Targeting |
|---|---|---|
| `applyTo: "**/*.cs"` | `paths: ["**/*.cs"]` | Same glob syntax |
| Auto-loaded per file | Auto-loaded per file | Same behavior |
| Full detail (120-340 lines) | Condensed (40-80 lines) + reference link | Claude = lean |

### Rule File Template

```markdown
---
paths:
  - "**/*.cs"
  - "**/*.razor"
description: One-line description of what this rule covers
---

# Rule Name

> Auto-loaded by Claude Code when working with matching files.
> Full reference: `.github/instructions/{category}/{filename}`

## Key Rules

- Rule 1...
- Rule 2...

---

*Deep-dive: Read `.github/instructions/{category}/{filename}` for complete patterns.*
```

### Rules to Create

| Rule File | Scoped To | Source |
|-----------|-----------|--------|
| `clean-architecture.md` | `**/*.cs` | `architecture/clean-architecture.instructions.md` |
| `blazor-components.md` | `**/*.razor`, `**/*.razor.cs`, `**/*.razor.css` | `blazor/component-patterns.instructions.md` |
| `cqrs-mediatr.md` | `**/Commands/**`, `**/Queries/**`, `**/Handlers/**` | `cqrs/mediatr-patterns.instructions.md` |
| `ef-core.md` | `**/Infrastructure/**`, `**/*DbContext*`, `**/*Repository*` | `database/ef-core-patterns.instructions.md` |
| `mvp-first.md` | `**/*` (always) | `development/mvp-first.instructions.md` |
| `ddd-domain.md` | `**/Domain/**`, `**/Entities/**`, `**/ValueObjects/**` | `domain/ddd-guidelines.instructions.md` |
| `memory-optimization.md` | `**/*` (always) | `memory/memory-optimization.instructions.md` |
| `polly-resilience.md` | `**/Infrastructure/**`, `**/Services/**` | `resilience/polly-patterns.instructions.md` |
| `owasp-security.md` | `**/*.cs`, `**/*.razor` | `security/owasp-top10.instructions.md` |
| `testing-standards.md` | `**/*Test*`, `**/*.Tests/**` | `testing/testing-standards.instructions.md` |

### Maintenance

When you update a `.github/instructions/` file, also update the matching `.claude/rules/` file.
The `.claude/rules/` files are condensed summaries — keep them under 80 lines. Point to the
full instruction file for deep-dive reference.

---

## Step 7: MCP Server Configuration

### `.github/copilot-mcp.json`

```json
{
  "mcpServers": {
    "your-db": {
      "command": "npx",
      "args": ["-y", "@anthropic/mcp-sqlserver"],
      "env": {
        "CONNECTION_STRING": "${env:YOUR_DB_CONNECTION_STRING}"
      }
    }
  }
}
```

**Never hardcode connection strings** — always use `${env:VAR_NAME}`.

### Common MCP Servers

| Server | Package | Use Case |
|--------|---------|----------|
| SQL Server | `@anthropic/mcp-sqlserver` | Database exploration |
| PostgreSQL | `@anthropic/mcp-postgres` | Database exploration |
| Filesystem | `@anthropic/mcp-filesystem` | Sandboxed file access |
| GitHub | Built-in | Repo, issues, PRs |

---

## Step 8: LSP Configuration

### `.github/lsp.json`

```json
{
  "lspServers": {
    "csharp": {
      "command": "dotnet",
      "args": ["tool", "run", "csharp-ls", "--solution", "YourProject.sln"],
      "fileExtensions": {
        ".cs": "csharp"
      }
    }
  }
}
```

### Common Language Servers

| Language | Command | Install |
|----------|---------|---------|
| C# | `csharp-ls` | `dotnet tool install csharp-ls` |
| TypeScript | `typescript-language-server` | `npm i -g typescript-language-server` |
| Python | `pylsp` | `pip install python-lsp-server` |
| Go | `gopls` | `go install golang.org/x/tools/gopls@latest` |
| Rust | `rust-analyzer` | Via rustup |

---

## Step 9: Cloud Agent Setup

### `.github/copilot-setup-steps.yml`

This configures the GitHub Copilot coding agent's CI environment:

```yaml
steps:
  - name: Setup runtime
    uses: actions/setup-dotnet@v4       # or setup-node, setup-go, etc.
    with:
      dotnet-version: '10.0.x'

  - name: Install dependencies
    run: dotnet restore YourProject.sln  # or npm ci, go mod download, etc.

  - name: Build
    run: dotnet build YourProject.sln --no-restore

  - name: Setup database
    uses: ikalnytskyi/action-setup-postgres@v7
    with:
      username: app_user
      password: ${{ secrets.DB_PASSWORD }}
      database: app_db
```

**All secrets via `${{ secrets.* }}`** — never inline credentials.

---

## Checklist for New Projects

### Phase 1: Foundation
- [ ] Create directory structure (Step 1)
- [ ] Write `AGENTS.md` with project identity and rules
- [ ] Write `CLAUDE.md` and `GEMINI.md` with model-specific guidance
- [ ] Write `.github/copilot-instructions.md` (most important file)

### Phase 2: Scoped Instructions
- [ ] Add scoped instructions for your stack's key concerns
- [ ] Add `memory-optimization.instructions.md` (copy as-is — it's universal)
- [ ] Add `mvp-first.instructions.md` (copy as-is — it's universal)
- [ ] Add domain-specific instructions (architecture, testing, security, etc.)

### Phase 3: Skills Catalog
- [ ] Copy `.github/skills/` directory (all categories)
- [ ] Adapt code examples in SKILL.md files for your stack
- [ ] Adapt reference files for your framework/language
- [ ] Update `CATALOG.md` with any added/removed skills
- [ ] Add authentication/authorization skills matching your auth provider
- [ ] Generate `.claude/skills/` bridge files (see Step 6B automation script)
- [ ] Verify `/skills` shows all skills in Claude Code

### Phase 3B: Claude Code Rules (`.claude/rules/`)
- [ ] Create `.claude/rules/` directory
- [ ] Create condensed rule files for each `.github/instructions/` file (see Step 6C)
- [ ] Verify `paths:` globs match your project structure
- [ ] Test: Edit a `.cs` file in Claude Code → rules should auto-load

### Phase 4: Extensions & Hooks
- [ ] Copy and adapt Copilot CLI extensions:
  - [ ] `security-scanner` — update secret patterns
  - [ ] `build-guardian` — update build/test commands
  - [ ] `context-optimizer` — update project summary
  - [ ] `research-first` — update docs path
  - [ ] `doc-sync` — rewrite feature→docs mapping
  - [ ] Stack-specific conventions extension
- [ ] Validate extensions: `node --check .github/extensions/*/extension.mjs`
- [ ] Create Claude Code hooks (`.claude/hooks/*.ps1` or `.sh`):
  - [ ] `security-scanner` — PreToolUse blocker for secrets
  - [ ] `dotnet-conventions` — PostToolUse convention checker
  - [ ] `doc-sync-reminder` — PostToolUse docs reminder
  - [ ] `build-reminder` — PostToolUse build reminder
  - [ ] `research-first` — UserPromptSubmit guidance
  - [ ] `context-optimizer` — SessionStart project context
- [ ] Add `hooks` section to `.claude/settings.json`
- [ ] Review hooks reference: `.github/docs/hooks-reference.md`

### Phase 5: Infrastructure
- [ ] Configure MCP servers if using databases
- [ ] Configure LSP for your language
- [ ] Configure cloud agent setup steps
- [ ] Test: start Copilot CLI in the repo and verify extensions load

---

## Maintenance

- **Update instructions** when architecture or conventions change
- **Update extension hooks** when adding new directories or features
- **Update `context-optimizer` summary** when the tech stack evolves
- **Update `doc-sync` mappings** when adding new feature documentation
- **Update skills** when adding new frameworks or patterns
- **Update `CATALOG.md`** when adding or removing skills
- **Update this SETUP-GUIDE.md** when any structural changes are made
- **Run `/instructions`** in Copilot CLI to verify which files are loaded
- **Run `extensions_manage({ operation: "list" })`** to check extension health
