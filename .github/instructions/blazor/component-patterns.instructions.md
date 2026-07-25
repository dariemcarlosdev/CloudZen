---
applyTo: "**/*.razor, **/*.razor.cs, **/*.razor.css"
---

# Blazor Component Patterns — Project Conventions

## Mandatory Code-Behind Pattern

Every Blazor component consists of **three files**:

```
ComponentName.razor        ← Markup only (HTML + Razor directives)
ComponentName.razor.cs     ← Logic (partial class, lifecycle, event handlers)
ComponentName.razor.css    ← Scoped styles (Bootstrap 5 overrides only)
```

### .razor — Markup
Contains HTML, Razor directives, and component references. **No `@code {}` blocks.** Use `@inject IStringLocalizer<SharedResource> L` for localized strings. Render loading indicators while data loads: `@if (_orders is null) { <spinner /> } else { <table /> }`.

### .razor.cs — Code-Behind
Sealed partial class with all logic. Inject services via `[Inject]` properties. Override `OnInitializedAsync` for data loading (not constructor). Implement `IDisposable` if component owns `CancellationTokenSource`. Use `protected` access on fields to allow markup binding. Call `IMediator.Send()` for all data operations.

---

## Component Lifecycle

| Method | Use When |
|---|---|
| `OnInitializedAsync` | Loading data on first render — primary data-fetch location |
| `OnParametersSetAsync` | Reacting to parameter changes from parent (e.g., selected order ID) |
| `OnAfterRenderAsync(firstRender)` | JS interop setup, DOM measurements — guard with `if (firstRender)` |
| `ShouldRender()` | Skipping re-renders on high-frequency updates (e.g., real-time feeds) |
| `Dispose` / `DisposeAsync` | Cleaning up `CancellationTokenSource`, timers, event subscriptions |

**Never** use the constructor for async work. Always use `OnInitializedAsync`.

---

## Bootstrap 5 Class Conventions

Use these standard Bootstrap 5 classes consistently:

| Element | Classes |
|---|---|
| Primary actions | `btn btn-primary` |
| Danger/cancel | `btn btn-outline-danger` |
| Data tables | `table table-striped table-hover` |
| Table headers | `table-dark` on `<thead>` |
| Status badges | `badge bg-success`, `badge bg-warning text-dark`, `badge bg-danger` |
| Cards | `card`, `card-header`, `card-body` |
| Forms | `form-control`, `form-label`, `form-select`, `form-check` |
| Layout | `container-fluid`, `row`, `col-md-*` |
| Spacing | `mt-3`, `mb-4`, `p-3` — use Bootstrap spacing utilities |
| Alerts | `alert alert-info`, `alert alert-danger` |

**Do NOT** use inline `style` attributes. Apply Bootstrap utility classes or scoped CSS instead.

---

## Localization

Inject `IStringLocalizer<SharedResource>` in every component that renders user-facing text. Resource keys: dot-separated, context-prefixed (e.g., `Dashboard.Title`, `Button.CreateOrder`). Never hardcode user-visible strings — always use localizer keys. In markup: `@Localizer["Key"]`. In code-behind: `L["Key"]`.

---

## Parent-Child Communication

### EventCallback&lt;T&gt; — Child notifies parent

Child component declares `[Parameter] public EventCallback<Guid> OnComplete { get; set; }` and calls `await OnComplete.InvokeAsync(_orderId)`. Parent invokes: `<ChildComponent OnComplete="HandleCompleteAsync" />`.

### CascadingParameter — Reserved for auth state only

Only use `[CascadingParameter] private Task<AuthenticationState> AuthState` for authentication. Do **not** cascade custom state objects. Use `IMediator` or scoped DI services instead.

---

## StreamRendering for Progressive Loading

Apply `[StreamRendering]` on pages that fetch data in `OnInitializedAsync`. Renders page shell immediately, streams content as data becomes available. Pair with a loading indicator that displays when `_data is null`.

---

## IDisposable Cleanup

Implement `IDisposable` when component owns: `CancellationTokenSource`, `Timer`, `PeriodicTimer`, event handler subscriptions, or `IJSObjectReference`. Call `.Cancel()` on `CancellationTokenSource` in `Dispose()`. This prevents memory leaks and circuit issues from dangling async operations.

---

## Hard Rules

| Rule | Rationale |
|---|---|
| ❌ No `@code { }` blocks in `.razor` files | Separation of concerns — logic lives in `.razor.cs` |
| ❌ No inline `style="..."` attributes | Use Bootstrap utilities or scoped `.razor.css` |
| ❌ No direct repository or DbContext injection | Go through `IMediator.Send()` only |
| ❌ No `IHttpContextAccessor` in components | Use `[CascadingParameter] Task<AuthenticationState>` |
| ✅ Always `partial class` in `.razor.cs` | Required for code-behind to work |
| ✅ Always scoped `.razor.css` per component | Prevents style leakage across components |
| ✅ Always localize user-facing strings | Required for multi-locale support |
| ✅ Always cancel async work on Dispose | Prevents memory leaks and circuit issues |
