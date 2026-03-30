---
name: ai-ready-docs
description: Apply AI-Model-Ready formatting to documentation. Use this when creating new docs, reviewing existing docs, or when asked to make documentation AI-ready for ChatGPT, Claude, or Gemini models.
---

## Purpose

This skill enforces the CloudZen AI-Model-Ready documentation standard. All documentation in `docs/` must follow this pattern so that any document can be fed to ChatGPT, Anthropic Claude, or Google Gemini as context and be parsed accurately.

## When to Apply

- **Creating** any new `.md` file in `docs/`
- **Reviewing** or **updating** existing documentation
- When the user asks to make docs "AI-ready", "model-ready", or "LLM-friendly"
- When the user invokes this skill by name

## Process

1. Read the target file(s) to understand current state.
2. Apply all formatting rules below.
3. Verify the result matches the checklist.
4. If creating a new doc, also update the parent directory's `README.md` index.

---

## AI-Model-Ready Formatting Rules

### Rule 1: Metadata Block

Every document MUST start with a metadata block as the very first content. Use markdown blockquotes, NOT YAML frontmatter:

```markdown
> **Document**: [Human-readable title]  
> **Scope**: [One-line description of what this doc covers]  
> **Audience**: AI assistants, developers  
> **Last Updated**: [Month Year]  
```

**Guidelines**:
- `Document` — descriptive title, not the filename
- `Scope` — concise sentence covering the doc's boundaries (use em-dashes for lists)
- `Audience` — always include "AI assistants" first, then human audiences
- `Last Updated` — month and year only (e.g., "March 2026")

### Rule 2: Scope Boundaries

Immediately after the metadata block (or after the H1 title), include a brief note about what this document does NOT cover, with cross-references to the docs that do:

```markdown
> For [related topic], see [`filename.md`](./filename.md).
```

Or as a subsection:

```markdown
### Scope Boundaries

This document does not cover:
- [Topic A] — see [`other-doc.md`](../path/other-doc.md)
- [Topic B] — see [`another-doc.md`](../path/another-doc.md)
```

### Rule 3: Table of Contents

Every document with more than 3 sections MUST include a Table of Contents after the metadata block and H1 heading. Use markdown links:

```markdown
## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Components](#components)
...
```

### Rule 4: Quick Reference Table

Feature documentation MUST include a Quick Reference summary table near the top (after Overview). This gives AI models immediate structured context:

```markdown
## Quick Reference

| Item | Value |
|------|-------|
| **Endpoint** | `POST /api/example` |
| **Frontend Component** | `Features/X/Components/Main.razor` |
| **Backend Function** | `Api/Features/X/ExampleFunction.cs` |
| **Key Integration** | [external service or pattern] |
| **Entry Point** | [how users reach this feature] |
```

### Rule 5: Heading Hierarchy

- Use H1 (`#`) only once — the document title
- Use H2 (`##`) for major sections
- Use H3 (`###`) for subsections
- Never skip levels (no H2 → H4)
- **No emoji** in headings — they cause parsing inconsistencies across models

### Rule 6: Structured Data

Prefer tables over prose for factual/reference information:

- Configuration settings → table
- Component listings → table
- API fields → table with Type, Required, Constraints columns
- Error handling → table with Error, User Message, HTTP Status columns
- Constants → table with Name, Value, Description columns

### Rule 7: Code Blocks

Always specify the language in fenced code blocks for syntax highlighting:

```csharp
// Good
public async Task<Result> DoSomething() { }
```

```json
{ "key": "value" }
```

### Rule 8: Cross-References

Every cross-reference must include a brief description of what the linked document adds:

```markdown
- [`API_ENDPOINTS.md`](../01-architecture/API_ENDPOINTS.md) — Full endpoint specification with request/response schemas
- [`02_ui_color_design_system.md`](../06-patterns/02_ui_color_design_system.md) — Sidebar and component color usage
```

Never use bare links without context.

### Rule 9: Self-Contained Content

Each document must be understandable without reading other documents. This means:
- Define acronyms on first use
- Include enough context to understand the feature independently
- Cross-reference for depth, but don't require it for comprehension

### Rule 10: ASCII-Safe Content

For maximum compatibility across AI model tokenizers:
- Use ASCII arrows (`->`, `-->`) instead of Unicode (`→`, `⟶`)
- Use ASCII dashes (`--`) instead of em-dashes (`—`)
- Use `[x]` and `[ ]` instead of `✅` and `❌` in tables
- Replace `•` bullets with `-`
- Avoid decorative emoji entirely

### Rule 11: File Naming

Files in `docs/` subdirectories follow numbered prefix convention:
```
XX_CATEGORY_NAME.md
```
Examples: `01_FEATURE_CONTACT_FORM.md`, `02_FEATURE_APPOINTMENT_SYSTEM.md`

### Rule 12: Directory Index

Each `docs/` subdirectory MUST have a `README.md` that:
- Has its own metadata block
- Lists all documents in the directory with a summary table
- Includes cross-references to related directories

---

## Verification Checklist

After applying the pattern, verify:

- [ ] Metadata block is the first content in the file
- [ ] Scope boundaries are stated (what's NOT covered)
- [ ] Table of Contents is present (if 3+ sections)
- [ ] Quick Reference table exists (for feature docs)
- [ ] No emoji in headings
- [ ] All code blocks have language specifiers
- [ ] Cross-references include descriptions
- [ ] Structured data uses tables, not prose
- [ ] Heading hierarchy is correct (H1 > H2 > H3, no skips)
- [ ] ASCII-safe characters used throughout
- [ ] Last Updated date is current
- [ ] Parent README.md index is updated (if new doc)

---

## Template for New Feature Documentation

```markdown
> **Document**: [Feature Name]  
> **Scope**: [What this doc covers]  
> **Audience**: AI assistants, developers  
> **Last Updated**: [Month Year]  

# [Feature Name]

## Table of Contents

1. [Overview](#overview)
2. [Quick Reference](#quick-reference)
3. [User Flow](#user-flow)
4. [Components](#components)
5. [API Integration](#api-integration)
6. [Request/Response](#requestresponse)
7. [Configuration](#configuration)
8. [Error Handling](#error-handling)
9. [Related Docs](#related-docs)

---

## Overview

[1-2 paragraph description of the feature]

### Scope Boundaries

This document does not cover:
- [Topic] -- see [`doc.md`](path)

---

## Quick Reference

| Item | Value |
|------|-------|
| **Endpoint** | `METHOD /api/path` |
| **Frontend Component** | `Features/X/Components/Main.razor` |
| **Backend Function** | `Api/Features/X/Function.cs` |
| **Entry Point** | [How users reach this feature] |

---

## User Flow

| Step | Action | Component |
|------|--------|-----------|
| 1 | ... | `Component.razor` |

---

[Continue with remaining sections...]

---

## Related Docs

- [`doc.md`](path) -- Description of what it adds

---

*Last Updated: [Month Year]*
```

## Template for Non-Feature Documentation

```markdown
> **Document**: [Title]  
> **Scope**: [What this doc covers]  
> **Audience**: AI assistants, developers  
> **Last Updated**: [Month Year]  

# [Title]

## Table of Contents

[sections...]

---

## Overview

[description]

### Scope Boundaries

[what's not covered + cross-refs]

---

[Content sections with tables for structured data...]

---

## Related Docs

- [`doc.md`](path) -- Description

---

*Last Updated: [Month Year]*
```
