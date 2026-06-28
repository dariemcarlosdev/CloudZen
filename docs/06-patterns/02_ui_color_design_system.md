# UI Color & Design System Pattern

Reference for building consistent components. All styling uses **Tailwind CSS v4** (CDN) with custom theme extensions defined in `wwwroot/index.html`.

---

## Brand Color Palette

### Primary Brand Colors

| Token | Hex | Usage |
|-------|-----|-------|
| `cloudzen-teal` | `#61C2C8` | Brand accent, validation highlights |
| `cloudzen-teal-hover` | `#74b7bb` | Hover state for teal elements |
| `cloudzen-teal-light` | `#76cbd2` | Light teal variant |
| `cloudzen-blue` | `#1b6ec2` | Secondary blue, legacy buttons |
| `cloudzen-blue-dark` | `#1861ac` | Borders, shadows |
| `cloudzen-blue-focus` | `#258cfb` | Focus rings |
| `cloudzen-steel` | `#2c194d` | ⚠️ Legacy purple — **avoid for new components** (use `teal-cyan-aqua-900` instead) |
| `cloudzen-steel-hover` | `#4a3270` | ⚠️ Legacy — reserved for backward compatibility |

### Teal-Cyan-Aqua Scale (Primary UI Scale)

This is the **main working palette** for component styling.

| Shade | Hex | Role |
|-------|-----|------|
| `50` | `#DAF6F9` | Icon/badge backgrounds, light fills |
| `100` | `#B8EFF4` | Light overlays |
| `200` | `#89D6DC` | Hover borders, header scroll accent |
| `300` | `#78BCC2` | Active borders, secondary hover |
| `400` | `#659FA5` | Accent borders, text highlights |
| `500` | `#538488` | Mid-tone UI elements |
| `600` | `#40676B` | **Primary accent** — links, highlights, icons, focus rings |
| `700` | `#2F4E51` | Body/paragraph text |
| `800` | `#1F3638` | Headings, card titles |
| `900` | `#0F1E1F` | Footer text, deep surfaces |
| `950` | `#081314` | Deepest background |

### Fonts

| Token | Stack | Usage |
|-------|-------|-------|
| `font-ibm-plex` | IBM Plex Sans, Arial, Helvetica, sans-serif | Headings, CTAs |
| `font-helvetica` | Helvetica Neue, Helvetica, Arial, sans-serif | Body text, UI |

---

## Color Roles

### Text Hierarchy

| Role | Class | When to use |
|------|-------|-------------|
| Heading (primary) ✅ | `text-gray-900` | Page/card titles — max contrast on white (current convention) |
| Heading (legacy) | `text-gray-800` | Existing components; new code prefers `text-gray-900` |
| Body | `text-gray-600` or `text-gray-700` | Paragraphs |
| Secondary | `text-gray-500` | Subtitles, descriptions |
| Muted | `text-gray-400` | Footer, metadata |
| Accent | `text-teal-cyan-aqua-600` | Highlighted keywords, links |
| Eyebrow / overline | `text-[10px] font-semibold text-gray-500 uppercase tracking-[0.14em]` | Section dividers ("Key results", "Challenges", "Active filters") — see Eyebrow Label pattern below |
| Dark accent | `text-teal-cyan-aqua-800` | Dark heading variants |

### Backgrounds

| Role | Class |
|------|-------|
| Default surface | `bg-white` |
| Subtle section | `bg-gray-50` or `bg-gray-100` |
| Gradient section | `bg-gradient-to-br from-gray-50 via-white to-teal-50` |
| **Dark CTA section** ✅ | `bg-gradient-to-br from-teal-cyan-aqua-900 to-teal-cyan-aqua-800` (see "Dark CTA Sections" below) |
| ⚠️ Dark section (legacy) | `bg-gray-700` — **avoid for new CTAs**, use dark-teal gradient instead |
| Footer | `bg-gray-900` |
| Structured dot pattern | `radial-gradient(circle, rgba(64,103,107,0.14) 1px, transparent 1.2px) 22px 22px` with elliptical mask — replaces decorative blobs (anti-Codevibe) |

### Sidebars (Dark Surfaces)

For dark sidebars in multi-step flows (booking, wizards, dashboards), use the **dark teal gradient** instead of `cloudzen-steel` (purple).

| Option | Classes | Hex Range | Recommendation |
|--------|---------|-----------|----------------|
| **Dark Teal** ✅ | `bg-gradient-to-br from-teal-cyan-aqua-900 to-teal-cyan-aqua-800` | `#0F1E1F` → `#1F3638` | **Recommended** — maintains brand cohesion |
| Slate Blue-Gray | `bg-slate-800` | `#1e293b` | Neutral alternative |
| Deep Ocean Teal | `bg-teal-900` | `#134e4a` | Tailwind default teal |
| Purple (legacy) | `bg-cloudzen-steel` | `#2c194d` | **Avoid** — clashes with teal/orange palette |

#### Why Dark Teal?

- **Brand cohesion:** The sidebar feels part of the same visual family as teal accents
- **Color harmony:** Purple sits opposite orange on the color wheel, creating tension rather than unity
- **Accent visibility:** `teal-cyan-aqua-200/300` accent text has natural kinship with the dark teal background

#### Sidebar Text Hierarchy (on dark teal)

| Role | Class | Example |
|------|-------|---------|
| Heading | `text-white` | Section titles |
| Secondary info | `text-teal-cyan-aqua-100` | Metadata, IDs |
| Badge/label | `text-teal-cyan-aqua-200` | Status indicators |
| Icons | `text-teal-cyan-aqua-300` | Decorative icons |
| Badge background | `bg-white/10` | Semi-transparent pills |

### Dark CTA Sections

Same dark-teal gradient as sidebars, used for page-bottom call-to-action blocks (`Services`, `Mission`, `WhoIAm` all migrated from `bg-gray-700` for brand cohesion).

```html
<section class="bg-gradient-to-br from-teal-cyan-aqua-900 to-teal-cyan-aqua-800 text-white text-center py-12 md:py-16">
    <h2 class="reveal text-2xl md:text-3xl font-bold mb-4">Headline</h2>
    <p class="reveal text-teal-cyan-aqua-100 mb-8">Subtext</p>
    <div class="reveal flex flex-col sm:flex-row gap-4 justify-center">
        <!-- Primary CTA: orange + shimmer + glow. No hover lift; active scale only. -->
        <a class="btn-shimmer px-8 py-3 bg-orange-400 text-teal-cyan-aqua-900 font-semibold rounded-full
                  shadow-lg shadow-orange-500/30
                  hover:bg-orange-500 hover:shadow-xl hover:shadow-orange-500/40
                  transition-all duration-300 active:scale-95 uppercase tracking-wide">
            Get Started →
        </a>
        <!-- Secondary CTA: glass pill — only legit blur-on-dark surface use. -->
        <a class="px-8 py-3 bg-white/10 backdrop-blur-sm text-white font-semibold rounded-full
                  border border-white/30
                  hover:bg-white/20 hover:border-white/50
                  transition-all duration-300 uppercase tracking-wide">
            Secondary Action
        </a>
    </div>
</section>
```

| Element | Class |
|---------|-------|
| Background | `bg-gradient-to-br from-teal-cyan-aqua-900 to-teal-cyan-aqua-800` |
| Headline | `text-white` |
| Subtext | `text-teal-cyan-aqua-100` |
| Primary CTA | `btn-shimmer bg-orange-400 text-teal-cyan-aqua-900` + glow + `active:scale-95` |
| Secondary CTA (on dark) | Glass pill: `bg-white/10 backdrop-blur-sm border-white/30` |

---

## Component Patterns

### Buttons

| Type | Classes | Use for |
|------|---------|---------|
| **Primary CTA** ✅ | `btn-shimmer group inline-flex items-center gap-2 px-8 py-3 bg-orange-400 text-teal-cyan-aqua-900 font-semibold rounded-full shadow-lg shadow-orange-400/30 hover:bg-orange-500 hover:shadow-xl hover:shadow-orange-500/40 transition-all duration-300 active:scale-95` | "Get Started", "Book", main actions |
| **Secondary** | `inline-flex items-center justify-center gap-2 px-8 py-3 bg-white text-gray-700 font-light rounded-full border border-gray-300 hover:border-teal-cyan-aqua-300 hover:text-teal-cyan-aqua-600 hover:bg-teal-cyan-aqua-50/40 transition-all duration-300` | Alternate actions next to primary |
| **Outlined ghost** | `inline-flex items-center gap-1.5 text-sm font-medium text-gray-500 hover:text-teal-cyan-aqua-700 transition-colors` | Tertiary actions, "Clear all" in filters |
| **Text link** | `text-teal-cyan-aqua-600 hover:text-teal-cyan-aqua-400 transition-colors` | Inline links |
| **Social icon** | `w-10 h-10 rounded-full bg-teal-cyan-aqua-50 text-teal-cyan-aqua-600 hover:bg-teal-cyan-aqua-600 hover:text-white flex items-center justify-center transition-all duration-200` | Social media links |
| **GitHub outlined** | `inline-flex items-center gap-1.5 px-3 py-2 text-xs font-medium text-gray-700 bg-gray-50 rounded-lg border border-gray-200 hover:bg-gray-900 hover:text-white hover:border-gray-900 transition-colors duration-200` | "View on GitHub" — secondary, swaps to dark on hover |

> **Rules:**
> - Primary CTAs are always **orange**. Teal is for accents and links, never primary actions.
> - Primary buttons get `btn-shimmer` + orange glow shadow + `active:scale-95`. Never `hover:scale-105` or `hover:-translate-y-N` (Codevibe lift).
> - Touch feedback uses `active:scale-95` only — never on hover.

### Cards — Standard pattern ✅

Restrained B2B treatment: no resting shadow, no hover-lift. Hover swaps border to teal + reveals a top accent bar via `transform: scaleX` (transform-only → no CLS, no layout thrash). Soft directional shadow appears on hover.

```html
<div class="group relative bg-white rounded-2xl border border-gray-200/80 overflow-hidden
            transition-[border-color,box-shadow] duration-300 ease-out
            hover:border-teal-cyan-aqua-300 hover:shadow-[0_10px_30px_-15px_rgba(64,103,107,0.22)]">
    <!-- Accent bar — fixed 16% sliver at rest, scales to 100% on hover -->
    <span aria-hidden="true"
          class="absolute top-0 left-0 right-0 h-[2px] bg-teal-cyan-aqua-500 origin-left
                 scale-x-[0.16] group-hover:scale-x-100
                 transition-transform duration-500 ease-[cubic-bezier(0.16,1,0.3,1)]"></span>
    <!-- Card content -->
</div>
```

| Token | Value | Why |
|-------|-------|-----|
| Border (rest) | `border-gray-200/80` | Quieter than `border-gray-100` — visible without shouting |
| Shadow (rest) | none | Cards rest flat; engaged on intent only |
| Border (hover) | `border-teal-cyan-aqua-300` | Brand teal accent |
| Shadow (hover) | `0 10px 30px -15px rgba(64,103,107,0.22)` | Soft directional (down + spread negative), teal-tinted |
| Accent bar | `h-[2px] bg-teal-cyan-aqua-500` with `scale-x` transform | 16% sliver hints structure; full bar on hover |
| Transition | `transition-[border-color,box-shadow] duration-300 ease-out` | Scoped, not `transition-all` |

> **Rule:** Never use `hover:-translate-y-1` / `hover:-translate-y-2` on cards (Codevibe lift). The accent-bar + border-shift + soft shadow pattern delivers the same "engaged on hover" signal without the templated feel.

### Cards — Anti-patterns (do not use)

```diff
- bg-white rounded-2xl shadow-lg hover:shadow-2xl transition-all duration-300 hover:-translate-y-2  // Codevibe lift
- bg-gradient-to-r from-teal-cyan-aqua-600 to-teal-cyan-aqua-400 text-white  // gradient card header block
- absolute top-4 right-4 bg-white/20 backdrop-blur-sm text-white  // glass overlay badge on gradient header
```

### Icon Containers

```
Round icon (standard card):
  w-16 h-16 bg-teal-cyan-aqua-50 rounded-full
  text-teal-cyan-aqua-600 text-3xl

Square icon (service card):
  w-14 h-14 bg-teal-cyan-aqua-50 rounded-xl
  text-teal-cyan-aqua-600 text-2xl
  group-hover:bg-teal-cyan-aqua-600 group-hover:text-white transition-colors
```

### Form Inputs

```
bg-gray-50 border border-gray-200 rounded-xl px-4 py-3
focus:outline-none focus:ring-2 focus:ring-teal-cyan-aqua-600 focus:border-transparent

Validation message: text-teal-cyan-aqua-600 text-xs mt-1
Invalid state:      outline: 1px solid #61C2C8  (via .invalid CSS class)
```

### Section Badges

```
inline-block px-4 py-1.5 bg-teal-cyan-aqua-50 text-teal-cyan-aqua-600
text-sm font-semibold rounded-full
```

### Accent Lines (Visual Separators)

```
w-16 h-1 bg-teal-cyan-aqua-600 rounded-full
```

Used below section titles and profile headers for visual rhythm.

---

## Interaction Patterns

### Hover Effects

| Element | Effect | Notes |
|---------|--------|-------|
| Cards ✅ | `hover:border-teal-cyan-aqua-300 hover:shadow-[0_10px_30px_-15px_rgba(64,103,107,0.22)]` + accent-bar `scaleX` | See "Cards — Standard pattern". Transform-only, no CLS. |
| ⚠️ Cards (legacy) | `hover:shadow-xl hover:-translate-y-1` / `hover:-translate-y-2` | **Avoid** — Codevibe lift. |
| Primary buttons ✅ | `hover:bg-orange-500 hover:shadow-xl hover:shadow-orange-500/40` (no scale) | Touch feedback via `active:scale-95` only. |
| ⚠️ Primary buttons (legacy) | `hover:scale-105` | **Avoid** — overused, feels templated. |
| Links | `hover:text-teal-cyan-aqua-400` | |
| Nav links | `hover:text-teal-cyan-aqua-600` + underline grow | |
| Icons (group, in card) | `group-hover:bg-teal-cyan-aqua-600 group-hover:text-white transition-colors duration-300` | Color swap, no transform |
| Arrow icons | `transition-transform group-hover:translate-x-1` | Subtle horizontal nudge — keep |

### Transitions

| Scope | Classes |
|-------|---------|
| Color only | `transition` (default) |
| All properties | `transition-all duration-300` |
| Fast | `transition-all duration-200` |
| Custom easing | `cubic-bezier(0.4, 0, 0.2, 1)` (scroll-to-top) |

### Shadow Hierarchy

| Level | Class | Usage |
|-------|-------|-------|
| Rest | `shadow-sm` | Cards at rest |
| Elevated | `shadow-lg` | Modals, dropdowns |
| Hover | `shadow-xl` | Cards on hover |
| Prominent | `shadow-xl shadow-gray-200/50` | Form containers |
| Colored | `shadow-teal-cyan-aqua-500/50` | Scroll-to-top button |

---

## Motion System

Premium entrance + scroll-reveal motion. Defined in `wwwroot/css/motion.css`, driven by `wwwroot/js/scroll-reveal.js` (IntersectionObserver), with keyframe utilities in the Tailwind config (`wwwroot/index.html`). **All motion animates `opacity`/`transform` only** — never `width`/`height`/`top`/`left` — to keep CLS ≈ 0.

> **Rule:** Only add `.reveal*` classes to components whose host page calls `initScrollReveal()` in `OnAfterRenderAsync(firstRender)`. Without that call, IntersectionObserver never fires and the element stays `opacity: 0` (invisible content). Above-the-fold heroes use the CSS-only `.entrance` classes instead (no JS dependency).

### Scroll-Reveal Classes (below the fold)

| Class | Effect | Use for |
|-------|--------|---------|
| `reveal` | Fade + rise (`translateY(24px)` → 0) | Section headers, paragraphs, accent lines |
| `reveal-scale` | Fade + `scale(0.94)` → 1 | Cards, tiles, badges |
| `reveal-left` / `reveal-right` | Fade + slide in from side | Alternating feature rows |

Add `.is-visible` is applied automatically by the observer when the element enters the viewport (threshold `0.12`), then it is unobserved (one-shot).

### Stagger Delays

Append `reveal-delay-1` … `reveal-delay-6` for grid/list cascade (≈80ms steps). Compute per-item in a loop, e.g. `reveal-delay-@((i % 6) + 1)`.

### Above-the-Fold Entrance (CSS-only, fires on load)

| Class | Effect |
|-------|--------|
| `entrance` | Fade-up on load |
| `entrance-1` … `entrance-6` | Choreography delays (50ms → 550ms) |

Hero order convention: badge → headline lines → subtext → CTAs → proof block.

### CTA Sheen

| Class | Effect |
|-------|--------|
| `btn-shimmer` | Diagonal light sweep across the button on hover (≈0.85s) |

Pair with palette glow on primary CTAs: `shadow-lg shadow-orange-400/30 hover:shadow-xl hover:shadow-orange-500/40` and `active:scale-95`.

### Decorative Motion

| Class | Effect |
|-------|--------|
| `animate-float-warm` | Gentle vertical float for hero imagery (defined in hero scoped CSS) — 7s, ±4px |
| `icon-halo` | Slow pulsing teal box-shadow ring (4.5s loop) — for decorative circle icons |
| `icon-halo-hover` | Scale `1.05` + soft teal shadow on hover (pair with `icon-halo`) |
| `logo-hover` | Gentle `scale(1.04)` on hover — header logo |

### Step Number Style

| Class | Effect |
|-------|--------|
| `step-number` | Teal gradient `linear-gradient(135deg, #40676B 0%, #61C2C8 100%)` + brand glow + `scale(1.08)` on hover. Apply to numbered process-step circles. |

### Accordion Panel Enter (use with caution)

| Class | Effect |
|-------|--------|
| `panel-enter` | Fade + `translateY(-6px) → 0` + `max-height: 0 → 800px` over 0.32s (cubic-bezier ease-out expo) + `overflow: hidden` |

> **⚠️ Do NOT stack `.panel-enter` on a panel whose host component already declares an `animation` in scoped CSS.** Two competing `animation` declarations + the `overflow: hidden` + `max-height` clipping can hide content entirely. Example: `Faq.razor.css` already animates `.faq-panel { animation: faq-slide-down }` — adding `.panel-enter` to the same element broke the panel. Use `.panel-enter` only on accordion panels with no scoped enter animation.

### Keyframe Inventory (Tailwind animation utilities)

| Utility | Keyframe |
|---------|----------|
| `animate-fade-up` | opacity 0 + `translateY(20px)` → visible |
| `animate-fade-in` | opacity 0 → 1 |
| `animate-scale-in` | opacity 0 + `scale(0.94)` → visible |

### Timing & Easing Guidance

| Scope | Budget | Easing |
|-------|--------|--------|
| Entrance / reveal | ≤ 600–700ms | `cubic-bezier(0.16, 1, 0.3, 1)` (ease-out expo) |
| Hover / micro-interaction | ≤ 300ms | `transition-all duration-300` |
| Fast feedback | ≤ 200ms | `transition-all duration-200` |

### Reduced Motion (mandatory)

`motion.css` includes a global `@media (prefers-reduced-motion: reduce)` kill-switch that disables all `.reveal*`, `.entrance`, shimmer, and blob motion (content shows immediately). `scroll-reveal.js` also detects reduced motion and marks elements visible without observing. Per-component continuous animations (e.g. hero node pulses) must add their own reduced-motion guard.

### Reveal-Guard Checklist (mandatory before shipping a new page)

If you add any `.reveal` / `.reveal-scale` / `.reveal-left` / `.reveal-right` class to a page or any of its child components:

1. The page's code-behind (or inline `@code`) **must** call `await JS.InvokeVoidAsync("initScrollReveal");` in `OnAfterRenderAsync(firstRender)`.
2. `[Inject] private IJSRuntime JS { get; set; } = default!;` (or `@inject IJSRuntime JS`).
3. Without (1), the IntersectionObserver never fires → `opacity: 0` stays → **invisible content**.
4. Above-the-fold elements that fire on load (no scroll needed) → use `.entrance` + `.entrance-1..6` instead (CSS-only, no JS).

**Pages with reveal classes already wired** (as of 2026-06-13):
`Pages/Index.razor`, `Pages/Contact.razor`, `Pages/ManageAppointment.razor`, `Features/Landing/Components/Services.razor`, `Features/Landing/Components/Mission.razor`, `Features/Legal/Components/Faq.razor`, `Features/Legal/Components/PrivacyPolicy.razor`, `Features/Legal/Components/TermsOfService.razor`, `Features/Profile/Components/WhoIAm.razor`, `Features/Tickets/Components/Tickets.razor`.

### Multi-Animation Conflict (lesson learned)

If a panel or content element has a **scoped** `animation` declared in its `.razor.css`, do **not** also apply a **global** motion utility (e.g. `.panel-enter`) that declares its own `animation` + `overflow: hidden` + `max-height` on the same element. The combination can hide content. Fix: pick one. Real example: `Faq.razor.css` `.faq-panel { animation: faq-slide-down }` collided with `.panel-enter` from `motion.css` and clipped panel content invisible.

---

## Gradient Text Accent

For premium headline accents, apply a teal gradient clip to a single accent line (keeps palette, adds depth):

```html
<span class="bg-gradient-to-r from-teal-cyan-aqua-500 to-cloudzen-teal bg-clip-text text-transparent">
    Accent line
</span>
```

> Use on **one** line per headline only — overuse flattens hierarchy.

---

## Header Scroll Behavior

```css
/* Default */
header { background: transparent; }

/* On scroll (via JS class toggle) */
header.header-scrolled {
  background-color: rgba(255, 255, 255, 0.85);
  backdrop-filter: blur(12px);
  border-bottom-color: #89D6DC; /* teal-cyan-aqua-200 */
}
```

---

## Dark Mode (Scaffolded)

Dark mode classes exist but are not yet fully implemented:

```html
<div class="bg-white dark:bg-teal-cyan-aqua-900 text-gray-900 dark:text-teal-cyan-aqua-50">
```

When implementing, use the teal-cyan-aqua scale for dark surfaces (`900`, `950`) and light text (`50`, `100`).

---

## Status Pill — semantic outlined variants

For project/order/ticket status indicators. Outlined chip, not filled bright pill.

```html
<span class="inline-flex items-center px-2.5 py-1 text-[10px] font-semibold uppercase tracking-wider rounded-md border @variantClasses">
    @status
</span>
```

| Status | Classes |
|--------|---------|
| Completed / Success | `bg-teal-cyan-aqua-50 text-teal-cyan-aqua-700 border-teal-cyan-aqua-200` |
| In Progress / Active | `bg-orange-50 text-orange-700 border-orange-200` |
| Planning / Pending | `bg-gray-50 text-gray-600 border-gray-200` |
| Default / Unknown | `bg-gray-50 text-gray-600 border-gray-200` |

> **Anti-patterns:** filled bright pills (`bg-amber-300 text-yellow-800`, `bg-red-200 text-red-700`), gradient pills with double shadow.

---

## Progress Bar — Teal/Orange Palette Ramp

Thin (`h-1.5`) container with rounded fill. Color reflects completion semantics, not generic rainbow.

```html
<div class="w-full bg-gray-100 rounded-full h-1.5 overflow-hidden">
    <div class="h-full rounded-full transition-[width] duration-500 ease-out @colorClass"
         style="width: @($"{progress}%")"></div>
</div>
```

| Progress | Fill Color |
|----------|-----------|
| ≥ 100% | `bg-teal-cyan-aqua-600` |
| ≥ 70%  | `bg-teal-cyan-aqua-500` |
| ≥ 40%  | `bg-orange-400` |
| < 40%  | `bg-gray-400` |

Pair label with `tabular-nums` so changing percentage doesn't cause layout shift.

> **Anti-pattern:** rainbow ramps (`bg-blue-400 / bg-emerald-600 / bg-yellow-400 / bg-red-500`) — off-brand and visually noisy.

---

## Avatar — Ring + Outline + Tinted Shadow

Replaces "glow halo" pattern (blurred gradient absolute layer behind avatar = Codevibe).

```html
<img src="@avatarUrl" alt="@alt"
     class="w-36 h-36 md:w-40 md:h-40 rounded-full object-cover
            ring-1 ring-gray-200
            outline outline-[6px] outline-white
            shadow-[0_8px_24px_-12px_rgba(64,103,107,0.25)]" />
```

| Layer | Role |
|-------|------|
| `ring-1 ring-gray-200` | Subtle gray frame |
| `outline outline-[6px] outline-white` | White "card" around avatar — grounds it on patterned/colored backgrounds |
| `shadow-[0_8px_24px_-12px_...]` | Soft teal-tinted directional shadow |

> **Anti-pattern:** `absolute -inset-1 bg-gradient-to-br ... rounded-full blur-sm opacity-40` — the "AI hero" avatar glow.

---

## Participant Avatar Stack

Overlapping avatar discs with white ring — reads as "team" without per-avatar colored borders.

```html
<div class="flex -space-x-2">
    @foreach (var p in participants)
    {
        <img src="@p.ImageUrl" alt="@p.Name"
             class="w-8 h-8 rounded-full border-2 border-white ring-1 ring-gray-200 object-cover" />
    }
</div>
<span class="text-xs text-gray-500">@string.Join(", ", participants.Select(p => p.Name))</span>
```

> **Anti-pattern:** `border-2 border-indigo-500 shadow` per avatar — loud, off-brand.

---

## Form Filter Panel

Restrained brand-aligned filter UI. Single white surface, system `<select>` controls with teal focus ring.

```html
<div class="bg-white rounded-2xl border border-gray-200/80 p-6 md:p-7">
    <!-- Header: icon + title + "Clear all" text link -->
    <div class="flex items-center justify-between mb-5">
        <div class="flex items-center gap-2.5">
            <i class="bi bi-funnel text-teal-cyan-aqua-600"></i>
            <h4 class="text-sm font-semibold text-gray-900 tracking-tight">Filter projects</h4>
        </div>
        <button class="inline-flex items-center gap-1.5 text-xs font-medium text-gray-500 hover:text-teal-cyan-aqua-700 transition-colors">
            <i class="bi bi-x-lg text-[10px]"></i> Clear all
        </button>
    </div>

    <!-- Select control with manual chevron -->
    <div class="relative">
        <select class="w-full appearance-none pl-3.5 pr-9 py-2.5 bg-white border border-gray-300 rounded-lg text-sm text-gray-800
                       hover:border-teal-cyan-aqua-300
                       focus:outline-none focus:ring-2 focus:ring-teal-cyan-aqua-600/30 focus:border-teal-cyan-aqua-500
                       transition-colors duration-200 cursor-pointer">...</select>
        <i class="bi bi-chevron-down absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 text-xs pointer-events-none"></i>
    </div>

    <!-- Active filter chip (dismissible) -->
    <span class="inline-flex items-center gap-1.5 pl-2.5 pr-1 py-1 bg-teal-cyan-aqua-50 text-teal-cyan-aqua-700 text-xs font-medium rounded-md border border-teal-cyan-aqua-100">
        <span>Status: Completed</span>
        <button class="inline-flex items-center justify-center w-4 h-4 rounded hover:bg-teal-cyan-aqua-100">
            <i class="bi bi-x"></i>
        </button>
    </span>
</div>
```

> **Anti-patterns:** `from-white via-indigo-50/30 to-white` gradient panel, per-field accent colors (status=indigo, type=amber), gradient pill buttons with double shadow, big "active filter count" badge, hover-rotate icons.

---

## Hero Right-Panel Background — Structured Dot Grid

Replaces decorative blur blobs. Reads as "engineering / systems" not "AI hero blur orb".

```css
.hero-grid-pattern {
    position: absolute;
    inset: 0;
    background-image: radial-gradient(circle, rgba(64, 103, 107, 0.14) 1px, transparent 1.2px);
    background-size: 22px 22px;
    -webkit-mask-image: radial-gradient(ellipse 62% 62% at center, #000 28%, transparent 78%);
    mask-image: radial-gradient(ellipse 62% 62% at center, #000 28%, transparent 78%);
    pointer-events: none;
    z-index: 0;
}
```

Pair with toned-down `.hero-radial-glow` (opacity ≤ 0.12) behind subject.

> **Removed utilities:** `.animate-blob-drift`, `.hero-blob`, `.hero-blob-teal`, `.hero-blob-cyan` — all deleted 2026-06-28. Do not reintroduce.

---

## Counter / Data-Viz Brand Tokens

When passing colors to chart/counter components, use brand-aligned hex (not raw indigo/green/red).

| Role | Hex | Token |
|------|-----|-------|
| Primary metric / total | `#40676B` | `teal-cyan-aqua-600` |
| Attention / needs action | `#fb923c` | `orange-400` |
| Passive / resolved | `#9CA3AF` | `gray-400` |

> **Anti-patterns:** app indigo `#6366f1`, raw success-green `#16a34a`, raw mid-gray `#6b7280`.

---

## Eyebrow Label Pattern

Editorial overline for section dividers ("Key results", "Challenges", "Active filters", "Progress").

```html
<h4 class="text-[10px] font-semibold text-gray-500 uppercase tracking-[0.14em] mb-2">Key results</h4>
```

Reads as section divider, scans quietly, leaves the heading hierarchy for real headings (H1/H2/H3).

---

## Quick Reference: New Component Checklist

When building a new component, follow these conventions:

1. **Surface:** `bg-white rounded-2xl border border-gray-200/80` (no resting shadow)
2. **Heading:** `text-gray-900 font-bold tracking-tight` with `font-ibm-plex`
3. **Body text:** `text-gray-600 leading-relaxed` with `font-helvetica`
4. **Accent keywords:** `text-teal-cyan-aqua-600`
5. **CTA button:** `btn-shimmer bg-orange-400 text-teal-cyan-aqua-900 rounded-full shadow-lg shadow-orange-400/30 hover:bg-orange-500 hover:shadow-xl hover:shadow-orange-500/40 active:scale-95`
6. **Icon container:** `w-12 h-12 bg-teal-cyan-aqua-50 text-teal-cyan-aqua-600 rounded-xl group-hover:bg-teal-cyan-aqua-600 group-hover:text-white transition-colors duration-300`
7. **Card hover** (replaces "hover lift"): `transition-[border-color,box-shadow] duration-300 ease-out hover:border-teal-cyan-aqua-300 hover:shadow-[0_10px_30px_-15px_rgba(64,103,107,0.22)]` + accent-bar `scaleX` (see Cards pattern)
8. **Focus ring:** `focus:outline-none focus:ring-2 focus:ring-teal-cyan-aqua-600/30 focus:border-teal-cyan-aqua-500`
9. **Decorative line:** `w-12 h-[3px] bg-teal-cyan-aqua-600 rounded-full` (was `w-16 h-1` — thinner/shorter for restraint)
10. **Badge/category chip:** `inline-flex items-center px-2.5 py-1 text-[10px] font-semibold tracking-wider uppercase text-teal-cyan-aqua-700 bg-teal-cyan-aqua-50 rounded-md border border-teal-cyan-aqua-100`
11. **Eyebrow label:** `text-[10px] font-semibold text-gray-500 uppercase tracking-[0.14em]`
12. **Status pill (semantic):** see "Status Pill — semantic outlined variants" below
13. **No icon emojis:** SVG (Lucide-style) or `bi-*` (bootstrap-icons) only. Never `🚀 ✨ 📊` in component markup.

> **Anti-Codevibe checklist** (every new component must NOT include):
> - Floating gradient blobs / radial blur orbs as background decoration
> - `hover:-translate-y-N` card lift
> - `hover:scale-105` on primary buttons
> - `backdrop-blur-*` on light surfaces (legit only on dark/photo surfaces)
> - Per-feature accent colors (indigo for one filter, amber for another) — single teal accent only
> - Gradient on small interactive controls (pill buttons, badges, chips)
> - Double shadow `shadow-lg shadow-X-500/30 border-1 border-X-400` stack
