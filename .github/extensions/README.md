# Copilot CLI Extensions

Custom JavaScript modules that extend GitHub Copilot CLI with hooks and tools.

## At a Glance

| Aspect | Detail |
|--------|--------|
| **SDK** | `@github/copilot-sdk/extension` — `joinSession()` API |
| **Runtime** | Node.js, ES modules (`.mjs`) |
| **Entry point** | `extension.mjs` per folder |
| **Capabilities** | `hooks` (session/tool lifecycle) and `tools` (custom tool definitions) |
| **Auto-load** | Extensions load on session start; run `extensions_reload` to pick up changes |

## Current Extensions

| Extension | Purpose |
|-----------|---------|
| `build-guardian` | Enforces build verification before commits |
| `context-optimizer` | Provides `project_summary` and `check_docs` tools for efficient context loading |
| `doc-sync` | Config-driven reminders to keep `docs/` and planning docs in sync with code changes |
| `dotnet-conventions` | `check_conventions` tool for .NET coding standards enforcement |
| `research-first` | Encourages codebase exploration before making changes |
| `security-scanner` | OWASP security scanning tools (`owasp_security_scan`, `check_secrets`) |
| `superpowers` | Workflow skill catalog and loader (`superpowers_catalog`, `superpowers_skill`) |

## File Structure

```
extensions/
└── my-extension/
    ├── extension.mjs    ← Entry point (required)
    └── config.json      ← Optional configuration
```

## How to Create a New Extension

1. **Scaffold:** Use `extensions_manage scaffold` with a name and description, or create the folder manually.
2. **Implement:** Export a default function that calls `joinSession()`, registering `hooks` and/or `tools`.
3. **Reload:** Run `extensions_reload` to activate without restarting the CLI.

```js
// extension.mjs — minimal example
import { joinSession } from "@github/copilot-sdk/extension";

joinSession({
  hooks: {
    onSessionStart: async (ctx) => { /* ... */ },
  },
  tools: {
    my_tool: {
      description: "Does something useful",
      parameters: { /* JSON Schema */ },
      execute: async (params) => { /* ... */ },
    },
  },
});
```

## Key Rules

- Extensions run in a **Node.js context** — they have access to `process.cwd()` and the filesystem.
- Extensions do **not** have access to the agent's conversation or chat context directly.
- Use ES module syntax (`import`/`export`) — CommonJS (`require`) is not supported.
- Keep extensions focused — one concern per extension.

## See Also

- [`.github/skills/`](../skills/) — Reusable AI workflow methodologies (markdown, not code)
- [`.github/instructions/`](../instructions/) — Pattern-matched context injection for AI agents
- [`.github/hooks/`](../hooks/) — Git hooks (trigger on git events, not AI events)
