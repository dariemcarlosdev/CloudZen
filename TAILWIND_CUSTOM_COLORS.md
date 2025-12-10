# CloudZen Custom Tailwind Colors Reference

This document provides a comprehensive guide for using CloudZen's custom brand colors and design system with Tailwind CSS utility classes.

## 🎨 Available Custom Colors

### CloudZen Brand Colors (Single Shades)
| Color Name | Hex Value | Preview | Usage |
|------------|-----------|---------|-------|
| `cloudzen-teal` | `#61C2C8` | ![#61C2C8](https://via.placeholder.com/20/61C2C8/61C2C8) | Primary brand teal (links, accents) |
| `cloudzen-teal-hover` | `#74b7bb` | ![#74b7bb](https://via.placeholder.com/20/74b7bb/74b7bb) | Hover state for teal elements |
| `cloudzen-teal-light` | `#76cbd2` | ![#76cbd2](https://via.placeholder.com/20/76cbd2/76cbd2) | Light teal variant |
| `cloudzen-blue` | `#1b6ec2` | ![#1b6ec2](https://via.placeholder.com/20/1b6ec2/1b6ec2) | Primary blue (buttons, highlights) |
| `cloudzen-blue-dark` | `#1861ac` | ![#1861ac](https://via.placeholder.com/20/1861ac/1861ac) | Darker blue for borders/shadows |
| `cloudzen-blue-focus` | `#258cfb` | ![#258cfb](https://via.placeholder.com/20/258cfb/258cfb) | Focus ring color |

### Teal-Cyan-Aqua Palette (Full Gradient Scale)
A complete 11-shade gradient from very light cyan to nearly black teal, perfect for backgrounds, overlays, and subtle UI elements.

| Shade | Hex Value | Preview | Description |
|-------|-----------|---------|-------------|
| `50` | `#DAF6F9` | ![#DAF6F9](https://via.placeholder.com/20/DAF6F9/DAF6F9) | Very light cyan - Subtle backgrounds |
| `100` | `#B8EFF4` | ![#B8EFF4](https://via.placeholder.com/20/B8EFF4/B8EFF4) | Light cyan - Light overlays |
| `200` | `#89D6DC` | ![#89D6DC](https://via.placeholder.com/20/89D6DC/89D6DC) | Medium cyan - Hover states |
| `300` | `#78BCC2` | ![#78BCC2](https://via.placeholder.com/20/78BCC2/78BCC2) | Teal - Active states |
| `400` | `#659FA5` | ![#659FA5](https://via.placeholder.com/20/659FA5/659FA5) | Darker teal - Borders |
| `500` | `#538488` | ![#538488](https://via.placeholder.com/20/538488/538488) | Teal-gray - Primary buttons |
| `600` | `#40676B` | ![#40676B](https://via.placeholder.com/20/40676B/40676B) | Dark teal-gray - Hover buttons |
| `700` | `#2F4E51` | ![#2F4E51](https://via.placeholder.com/20/2F4E51/2F4E51) | Very dark teal - Text |
| `800` | `#1F3638` | ![#1F3638](https://via.placeholder.com/20/1F3638/1F3638) | Almost black teal - Headings |
| `900` | `#0F1E1F` | ![#0F1E1F](https://via.placeholder.com/20/0F1E1F/0F1E1F) | Nearly black - Footers |
| `950` | `#081314` | ![#081314](https://via.placeholder.com/20/081314/081314) | Almost pure black - Deep backgrounds |

### Custom Font Families
| Font Name | CSS Stack | Usage |
|-----------|-----------|-------|
| `font-ibm-plex` | IBM Plex Sans, Arial, Helvetica, sans-serif | Brand font (headings, CTAs) |
| `font-helvetica` | Helvetica Neue, Helvetica, Arial, sans-serif | Body text (paragraphs, UI)

---

## 🛠️ Usage Examples

### Background Colors
```html
<div class="bg-cloudzen-teal">Brand teal</div>
<div class="bg-teal-cyan-aqua-500">Teal-gray</div>
<section class="bg-teal-cyan-aqua-50">Light cyan background</section>
```

### Text Colors
```html
<h1 class="text-cloudzen-blue">Blue heading</h1>
<p class="text-teal-cyan-aqua-700">Dark teal paragraph</p>
<a href="#" class="text-cloudzen-teal hover:text-cloudzen-teal-hover transition">Link</a>
```

### Hover States
```html
<button class="bg-cloudzen-blue hover:bg-cloudzen-blue-dark transition">Button</button>
<a class="text-cloudzen-teal hover:text-cloudzen-teal-hover transition-colors">Link</a>
```

### Border Colors
```html
<div class="border-2 border-cloudzen-teal">Teal border</div>
<input class="border border-teal-cyan-aqua-300 focus:border-cloudzen-blue" />
```

### Gradients
```html
<!-- Background gradient -->
<div class="bg-gradient-to-r from-cloudzen-teal to-cloudzen-blue">Gradient</div>

<!-- Text gradient -->
<h1 class="bg-gradient-to-r from-cloudzen-teal to-cloudzen-blue bg-clip-text text-transparent">
  Gradient Text
</h1>

<!-- Subtle gradient -->
<section class="bg-gradient-to-br from-teal-cyan-aqua-50 to-teal-cyan-aqua-200">
  Light gradient
</section>
```

### Shadows
```html
<div class="shadow-lg shadow-cloudzen-teal/50">Card with teal shadow</div>
<button class="shadow-md shadow-cloudzen-blue/30 hover:shadow-lg">Button</button>
```

### Focus Rings
```html
<input class="focus:ring-2 focus:ring-cloudzen-blue-focus focus:border-cloudzen-blue" />
<button class="focus:ring-2 focus:ring-cloudzen-teal focus:ring-offset-2">Button</button>
```

### Opacity Modifiers
```html
<div class="bg-cloudzen-teal/10">10% teal</div>
<div class="bg-cloudzen-blue/50">50% blue</div>
<div class="text-teal-cyan-aqua-700/80">80% opacity text</div>
```

### Custom Fonts
```html
<h1 class="font-ibm-plex font-bold text-cloudzen-blue">Branded Heading</h1>
<p class="font-helvetica text-gray-700">Body text</p>
<button class="font-ibm-plex font-semibold">CTA Button</button>
```

---

## 🎨 Real-World Components

### Example 1: Branded Button
```html
<button class="bg-cloudzen-teal hover:bg-cloudzen-teal-hover text-white font-ibm-plex font-semibold px-6 py-3 rounded-lg shadow-md transition-all">
  Get Started
</button>
```

### Example 2: Card with Custom Palette
```html
<div class="bg-white border-2 border-teal-cyan-aqua-300 rounded-xl p-6 shadow-xl shadow-teal-cyan-aqua-500/20 hover:shadow-2xl transition-shadow">
  <h3 class="text-teal-cyan-aqua-800 font-bold text-xl mb-3">Card Title</h3>
  <p class="text-gray-600 font-helvetica">Card content with custom colors</p>
</div>
```

### Example 3: Navigation Link
```html
<a href="#" class="text-cloudzen-teal hover:text-cloudzen-teal-hover font-ibm-plex font-semibold transition-colors border-b-2 border-transparent hover:border-cloudzen-teal-hover">
  Navigation Link
</a>
```

### Example 4: Hero Section with Gradient
```html
<section class="bg-gradient-to-br from-teal-cyan-aqua-50 to-teal-cyan-aqua-200 py-24">
  <h1 class="text-5xl font-bold font-ibm-plex bg-gradient-to-r from-cloudzen-teal to-cloudzen-blue bg-clip-text text-transparent">
    Welcome to CloudZen
  </h1>
  <p class="text-xl text-teal-cyan-aqua-700 font-helvetica">Building scalable solutions</p>
</section>
```

### Example 5: Form Input
```html
<input type="email" class="w-full px-4 py-2 border-2 border-teal-cyan-aqua-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-cloudzen-blue-focus focus:border-cloudzen-blue transition-all font-helvetica" placeholder="Enter email" />
```

### Responsive Design
```html
<!-- Responsive backgrounds -->
<div class="bg-teal-cyan-aqua-100 md:bg-teal-cyan-aqua-300 lg:bg-teal-cyan-aqua-500">
  Light (mobile) → Medium (tablet) → Dark (desktop)
</div>

<!-- Responsive text -->
<h2 class="text-cloudzen-teal md:text-cloudzen-blue lg:text-teal-cyan-aqua-700">
  Responsive color heading
</h2>
```

### Dark Mode (Future)
```html
<!-- Light/dark mode ready -->
<div class="bg-white dark:bg-teal-cyan-aqua-900 text-gray-900 dark:text-teal-cyan-aqua-50">
  Content
</div>

<button class="bg-cloudzen-teal dark:bg-teal-cyan-aqua-700 hover:bg-cloudzen-teal-hover dark:hover:bg-teal-cyan-aqua-600">
  Dark mode button
</button>
```

---

## 🔄 Migration from Custom CSS

**Before:**
```css
.cloudzen-hover {
    color: #fff;
    font-weight: 600;
}
.cloudzen-hover:hover { color: #76cbd2; }
```

**After:**
```html
<a class="text-white font-semibold hover:text-cloudzen-teal-light transition">Link</a>
```

---

## 🎯 Best Practices

1. **Always add transitions**: `hover:bg-cloudzen-teal-hover transition`
2. **Use opacity for subtle effects**: `bg-cloudzen-teal/10`, `shadow-cloudzen-blue/30`
3. **Combine with Tailwind defaults**: `bg-gray-50 border-cloudzen-teal`
4. **Follow color hierarchy**: Light (50-200) → Medium (300-500) → Dark (600-950)
5. **Ensure contrast**: Test readability with [WebAIM Contrast Checker](https://webaim.org/resources/contrastchecker/)
6. **Responsive colors**: `bg-teal-cyan-aqua-100 md:bg-teal-cyan-aqua-300 lg:bg-teal-cyan-aqua-500`

---

## 🔧 Extending Colors

Edit `wwwroot/index.html` to add more colors:

```javascript
tailwind.config = {
    theme: {
        extend: {
            colors: {
                // Add semantic colors:
                'cloudzen-success': '#26b050',
                'cloudzen-error': '#ef4444',
                'cloudzen-warning': '#f59e0b',
            }
        }
    }
}
```

---

## 📚 Resources

- ** Palette Generator and API for Tailwind CSS**: [www.tints.dev](https://www.tints.dev/palette/v1:ZW1lcmFsZHw3OEJDQzJ8MzAwfHB8MHwwfDB8MTAwfG0)
- **Tailwind Docs**: [tailwindcss.com/docs](https://tailwindcss.com/docs)
- **Color Generator**: [uicolors.app](https://uicolors.app)
- **Contrast Checker**: [webaim.org/contrastchecker](https://webaim.org/resources/contrastchecker/)
- **Component Docs**: [COMPONENT_ARCHITECTURE.md](COMPONENT_ARCHITECTURE.md)

---

**Last Updated**: December 2025  
**Maintained By**: Dariem C. Macias - [LinkedIn](https://www.linkedin.com/in/dariemcmacias) | [GitHub](https://github.com/dariemcarlosdev)
