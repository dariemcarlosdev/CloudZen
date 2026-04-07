---
paths:
  - "**/*.razor"
  - "**/*.razor.cs"
  - "**/*.razor.css"
description: Blazor component patterns — code-behind, CSS isolation, lifecycle, forms
---

# Blazor Component Patterns

> Auto-loaded by Claude Code when working with matching files.
> Full reference: `.github/instructions/blazor/component-patterns.instructions.md`

## Mandatory Three-File Structure

Every component = three files, no exceptions:

```
ComponentName.razor      ← Markup only (HTML + Razor directives, NO @code blocks)
ComponentName.razor.cs   ← Logic (partial class, lifecycle, event handlers)
ComponentName.razor.css  ← Scoped styles (Bootstrap 5 overrides only)
```

## Code-Behind Rules

- Class must be `partial` and `sealed`, matching the `.razor` filename
- Inject services via `[Inject]` properties, not constructor
- Use `[CascadingParameter] Task<AuthenticationState>` for auth — never `IHttpContextAccessor`
- All data access through `IMediator.Send()` — never inject repositories or `DbContext`

## Lifecycle

- `OnInitializedAsync` — primary data-fetch location (not constructor)
- `OnParametersSetAsync` — react to parameter changes from parent
- `OnAfterRenderAsync(firstRender)` — JS interop setup, guard with `if (firstRender)`
- `ShouldRender()` — skip unnecessary re-renders on high-frequency updates
- Always implement `IDisposable` when owning `CancellationTokenSource`, timers, event subscriptions, or JS interop refs

## Communication

- Parent→Child: `[Parameter]` properties
- Child→Parent: `EventCallback<T>` — invoke with `await OnRelease.InvokeAsync(value)`
- `CascadingParameter` reserved for auth state only — use `IMediator` or scoped DI for custom state

## StreamRendering

- Apply `@attribute [StreamRendering]` on pages fetching data in `OnInitializedAsync`
- Pair with null-check loading indicator (`@if (_data is null) { spinner }`)

## Bootstrap 5 Classes

- Primary actions: `btn btn-primary` | Danger: `btn btn-outline-danger`
- Tables: `table table-striped table-hover` with `table-dark` on `<thead>`
- Forms: `form-control`, `form-label`, `form-select`
- Layout: `container-fluid`, `row`, `col-md-*`
- No inline `style` attributes — use Bootstrap utilities or scoped CSS

## Localization

- Inject `IStringLocalizer<SharedResource>` in every component with user-facing text
- Reference as `@Localizer["Key"]` — never hardcode visible strings
- Keys: dot-separated, context-prefixed (e.g., `Dashboard.Title`)

## Hard Rules

- ❌ No `@code { }` blocks in `.razor` files
- ❌ No inline `style="..."` attributes
- ❌ No direct repository or `DbContext` injection in components
- ✅ Always `partial class` in `.razor.cs`
- ✅ Always scoped `.razor.css` per component
- ✅ Always cancel async work on `Dispose`

---

*Deep-dive: Read `.github/instructions/blazor/component-patterns.instructions.md` for complete patterns and examples.*
