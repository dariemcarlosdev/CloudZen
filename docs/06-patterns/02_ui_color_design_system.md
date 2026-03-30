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
| Heading | `text-gray-800` | Page/section titles |
| Body | `text-gray-700` or `text-teal-cyan-aqua-700` | Paragraphs |
| Secondary | `text-gray-500` | Subtitles, descriptions |
| Muted | `text-gray-400` | Footer, metadata |
| Accent | `text-teal-cyan-aqua-600` | Highlighted keywords, links |
| Dark accent | `text-teal-cyan-aqua-800` | Dark heading variants |

### Backgrounds

| Role | Class |
|------|-------|
| Default surface | `bg-white` |
| Subtle section | `bg-gray-50` or `bg-gray-100` |
| Gradient section | `bg-gradient-to-br from-gray-50 via-white to-teal-50` |
| Dark section | `bg-gray-700` (mid), `bg-gray-900` (footer) |
| Decorative blob | `bg-teal-200 rounded-full opacity-20 blur-2xl` |

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

---

## Component Patterns

### Buttons

| Type | Classes | Use for |
|------|---------|---------|
| **Primary CTA** | `bg-orange-400 text-white rounded-full shadow-lg hover:bg-orange-500 hover:scale-105 transition-all duration-300` | "Get Started", "Book", main actions |
| **Secondary** | `bg-white text-gray-700 border-2 border-gray-200 rounded-full hover:border-teal-cyan-aqua-300 hover:text-teal-cyan-aqua-600 transition-all` | Alternate actions |
| **Inverted** | `bg-white text-orange-500 rounded-full shadow` | On colored backgrounds (CTA sections) |
| **Text link** | `text-teal-cyan-aqua-600 hover:text-teal-cyan-aqua-400 transition` | Inline links |
| **Social icon** | `bg-teal-cyan-aqua-50 text-teal-cyan-aqua-600 rounded-full hover:bg-teal-cyan-aqua-600 hover:text-white transition-all duration-200` | Social media links |

> **Rule:** Primary CTAs are always **orange**. Teal is for accents and links, never primary actions.

### Cards

```
Standard card:
  bg-white rounded-2xl border border-gray-100 shadow-sm
  hover:shadow-xl hover:border-teal-cyan-aqua-200 hover:-translate-y-1
  transition-all duration-300

With gradient header:
  Header: bg-gradient-to-r from-teal-cyan-aqua-600 to-teal-cyan-aqua-400 text-white
  Badge:  bg-white/20 backdrop-blur-sm text-white px-3 py-1 rounded-full text-xs
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

| Element | Effect |
|---------|--------|
| Cards | `hover:shadow-xl hover:-translate-y-1` or `hover:-translate-y-2` |
| Primary buttons | `hover:bg-orange-500 hover:scale-105 hover:shadow-xl` |
| Links | `hover:text-teal-cyan-aqua-400` |
| Nav links | `hover:text-teal-cyan-aqua-600` |
| Icons (group) | `group-hover:bg-teal-cyan-aqua-600 group-hover:text-white` |

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

## Quick Reference: New Component Checklist

When building a new component, follow these conventions:

1. **Surface:** `bg-white rounded-2xl border border-gray-100`
2. **Heading:** `text-gray-800 font-bold` with `font-ibm-plex`
3. **Body text:** `text-gray-500 leading-relaxed` with `font-helvetica`
4. **Accent keywords:** `text-teal-cyan-aqua-600`
5. **CTA button:** `bg-orange-400 text-white rounded-full hover:bg-orange-500`
6. **Icon container:** `bg-teal-cyan-aqua-50 text-teal-cyan-aqua-600 rounded-xl`
7. **Hover lift:** `hover:shadow-xl hover:-translate-y-1 transition-all duration-300`
8. **Focus ring:** `focus:ring-2 focus:ring-teal-cyan-aqua-600`
9. **Decorative line:** `w-16 h-1 bg-teal-cyan-aqua-600 rounded-full`
10. **Badge/label:** `bg-teal-cyan-aqua-50 text-teal-cyan-aqua-600 text-sm rounded-full`
