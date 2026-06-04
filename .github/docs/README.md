# AI Context Documentation Index

This folder contains AI-assistant support docs. Use this file as the quick map for where to load context from.

## Start Here

1. Project overview: `README.md`
2. Architecture and APIs: `docs/01-architecture/`
3. Feature behavior: `docs/03-features/`
4. Security guidance: `docs/04-security/`
5. Troubleshooting and fixes: `docs/05-troubleshooting/`
6. Reusable implementation patterns: `docs/06-patterns/`
7. Deployment runbooks: `docs/02-deployment/`

## AI/Agent-Specific Docs

- Hook lifecycle reference: `.github/docs/hooks-reference.md`
- Project-wide AI instructions: `AGENTS.md`, `CLAUDE.md`, `GEMINI.md`
- Skill catalog and workflows: `.github/skills/CATALOG.md` and `.github/skills/*/SKILL.md`

## Context-Saving Guidance

- For architecture changes, read `docs/01-architecture/*` first.
- For feature work, read the matching file in `docs/03-features/*`.
- For security/compliance-sensitive work, check `docs/04-security/*` before edits.
- For known incidents, load `docs/05-troubleshooting/*`.
- When adding new behavior, update the relevant file under `docs/` and keep this index accurate.
