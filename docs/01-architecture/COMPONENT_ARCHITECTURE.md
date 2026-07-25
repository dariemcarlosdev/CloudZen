# Component Architecture

## Overview

CloudZen is a Blazor WebAssembly app using a component-based architecture. Parent components orchestrate state and layout; children receive data via `[Parameter]` and communicate upward via `EventCallback<T>`. No centralized state management library is used.

---

## Directory Structure

> For the full folder layout, see [Vertical Slice Architecture](VERTICAL_SLICE_ARCHITECTURE.md).

| Directory | Purpose |
|-----------|---------|
| `Features/Booking/Components/` | Calendar, form, confirmation, cancel/reschedule flows |
| `Features/Contact/Components/` | ContactForm |
| `Features/Chat/Components/` | CloudZenChatbot (FAB + panel) |
| `Features/Landing/Components/` | Hero, CTA, Services, Mission, CaseStudies, etc. |
| `Features/Profile/Components/` | ProfileHeader, ProfileApproach, ProfileHighlights |
| `Features/Projects/Components/` | ProjectCard, ProjectFilter |
| `Features/Tickets/Components/` | Tickets overview |
| `Common/Components/` | AnimatedCounterCircle, ScrollToTopButton |
| `Layout/` | MainLayout, Header, Footer |
| `Pages/` | Thin orchestrators: Index.razor, Contact.razor |

---

## Component Communication

| Pattern | Direction | Usage |
|---------|-----------|-------|
| `[Parameter]` | Parent → Child | Pass data down (immutable props) |
| `EventCallback<T>` | Child → Parent | Notify parent of events (child doesn't know parent implementation) |
| Shared service | Sibling ↔ Sibling | Use injected service when siblings need to communicate |

**Key Principles:** Type safety via generics, loose coupling, parent owns state.

---

## Service Layer

| Type | Examples | Pattern |
|------|----------|---------|
| **Backend-calling** (async) | `ApiEmailService`, `ChatbotService`, `AppointmentService` | `HttpClient` + `IOptions<T>` → returns result type with `Ok()`/`Fail()` |
| **Data-only** (sync) | `ProjectService`, `PersonalService`, `ToolService` | In-memory data, synchronous methods, no HTTP |

**DI Registration:** Backend-calling services use `AddScoped<>`, data-only services use `AddScoped<>` or `AddSingleton<>`.

---

## Data Models

| Pattern | When to Use |
|---------|-------------|
| `record` | Immutable data (e.g., `ServiceInfo`, `ToolInfo`) |
| `class` with `[Required]` | Form models with validation (e.g., `ContactFormModel`) |
| Factory methods | Message types with role-based creation (e.g., `ChatMessage.User()`) |

---

## Naming Conventions

| Category | Pattern | Examples |
|----------|---------|----------|
| Components | `<Feature><Role>.razor` | `ProfileHeader`, `BookingCalendar` |
| Code-behind | `<Component>.razor.cs` | `BookingCalendar.razor.cs` |
| Scoped CSS | `<Component>.razor.css` | `BookingCalendar.razor.css` |
| Services | `<Domain>Service.cs` | `ApiEmailService`, `ProjectService` |
| Interfaces | `I<Domain>Service.cs` | `IEmailService`, `IChatbotService` |
| Options | `<Service>Options.cs` | `EmailServiceOptions`, `ChatbotOptions` |
| Parameters | PascalCase | `AvatarUrl`, `OnFilterChange` |

---

## Styling

| Approach | Description |
|----------|-------------|
| **Tailwind CSS v4** | Primary styling via CDN (utility-first, no build pipeline) |
| **Bootstrap Icons** | Iconography via CDN |
| **Component-scoped CSS** | Use `.razor.css` for component-specific overrides (see below) |

> For full color system and patterns, see [UI Color & Design System](../06-patterns/02_ui_color_design_system.md).

### Component-Scoped CSS (`.razor.css`)

| File Pattern | Scope | When to Use |
|--------------|-------|-------------|
| `ComponentName.razor.css` | Isolated to that component only | Complex animations, pseudo-elements, Tailwind can't express |

**Rules:**
- Blazor auto-generates unique `b-{hash}` attributes for CSS isolation
- Prefer Tailwind utilities in markup; use `.razor.css` only when necessary
- Use `::deep` combinator to style child component elements

**Current components with scoped CSS:**

| Component | CSS File | Purpose |
|-----------|----------|---------|
| `CloudZenChatbot` | `CloudZenChatbot.razor.css` | Chat panel animations, scrollbar styling |
| `Header` | `Header.razor.css` | Scroll transition effects |

---

## Component Guidelines

1. **Single responsibility** — one component, one purpose
2. **Parameters for data** — accept via `[Parameter]`, don't fetch internally
3. **EventCallback for events** — child notifies parent, parent owns state
4. **Keep pages thin** — pages are orchestrators, not implementors
5. **Services for data** — inject services for data access
6. **Responsive first** — mobile-first Tailwind classes

---

## Razor Component Best Practices (SOLID Alignment)

### Code-Behind Pattern (Required)

**Always** separate C# logic from markup:

| File | Contains |
|------|----------|
| `ComponentName.razor` | Markup only (HTML + Razor syntax) |
| `ComponentName.razor.cs` | Logic (state, handlers, DI, lifecycle) |

**Benefits:** Separation of concerns, testability, better IntelliSense, SOLID compliance.

### Code-Behind Structure (Section Order)

1. **Dependencies** — `[Inject]` properties (interfaces only)
2. **Parameters** — `[Parameter]` properties with `[EditorRequired]` for mandatory
3. **State** — Private fields grouped by purpose (Form Data, UI Feedback)
4. **Lifecycle** — `OnInitialized`, `OnParametersSet`, etc.
5. **Event Handlers** — Methods invoked from markup
6. **Helper Methods** — Private utilities, CSS builders

### SOLID Principles Summary

| Principle | Blazor Application |
|-----------|-------------------|
| **S** — Single Responsibility | One component = one purpose. Split "god components" into parent/child composition. Max ~200 lines. |
| **O** — Open/Closed | Extend via `[Parameter]`, `RenderFragment`, `EventCallback`. Use enums for behavior variants. |
| **L** — Liskov Substitution | Consistent callback signatures across similar components (e.g., `OnSelected`, `OnDateSelected`). |
| **I** — Interface Segregation | Focused parameters. Use `[EditorRequired]` for mandatory, nullable for optional. No "kitchen sink" option objects. |
| **D** — Dependency Inversion | Inject `IService` interfaces, never concrete types. |

### Documentation Standards

All code-behind files must include:
- `<summary>` describing component purpose
- `<remarks>` noting which SOLID principles are applied
- XML docs on injected services explaining their role

### State Management Patterns

| Pattern | Implementation |
|---------|---------------|
| **Wizard flows** | `enum Step { ... }` + `currentStep` variable |
| **Form state** | Group fields: Form Data section, UI Feedback section |
| **Reset** | Provide `Reset()` method for reusable components |

---

## Component File Checklist

When creating a new component:

- [ ] `ComponentName.razor` — markup only, no `@code` block
- [ ] `ComponentName.razor.cs` — all C# logic with XML docs
- [ ] `ComponentName.razor.css` — only if Tailwind insufficient (optional)
- [ ] Sections ordered: Dependencies → Parameters → State → Lifecycle → Handlers → Helpers
- [ ] `[EditorRequired]` on mandatory parameters
- [ ] Inject interfaces only (Dependency Inversion)
- [ ] Keep under 200 lines; split if larger

---

## Related Docs

- [Vertical Slice Architecture](VERTICAL_SLICE_ARCHITECTURE.md) — Feature folder structure
- [Configuration](CONFIGURATION.md) — IOptions pattern, secrets strategy
- [API Endpoints](API_ENDPOINTS.md) — Backend endpoints that services call
- [Azure Functions](AZURE_FUNCTIONS.md) — API backend architecture
- [UI Color & Design System](../06-patterns/02_ui_color_design_system.md) — Colors, buttons, styling patterns

---

*Last Updated: January 2025*
