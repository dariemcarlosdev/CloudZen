---
applyTo: "EscrowApp/Features/**/*.cs, EscrowApp/Components/Pages/**/*.razor, EscrowApp/Components/Pages/**/*.razor.cs, EscrowApp/Models/**/*.cs, EscrowApp/Events/**/*.cs, EscrowApp/Services/**/*.cs, EscrowApp/Data/**/*.cs, EscrowApp/Infrastructure/**/*.cs, EscrowApp.Tests/**/*.cs, EscrowApp/Program.cs"
---

# Planning Documentation Sync

When you complete work that changes project status — implementing a feature, writing tests, replacing stubs, adding new handlers/components/strategies — you **must** update:

1. **`docs/planning/implementation-plan.md`** — Update phase completion %, move items between "What's Built" and "What's Missing", update MVP priorities.
2. **`docs/planning/task-checklist.md`** — Check off completed items (`[x]`), add new tasks, update phase status markers.
3. **Update `Last synced with codebase` date** at the top of each file.

Use the `planning_status` tool to verify these files are current before finishing your task.
