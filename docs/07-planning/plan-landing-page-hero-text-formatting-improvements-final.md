# 🎯 Landing Page Hero Text & Formatting Improvements - Final

## Context & Objectives
The goal is to update the text and formatting of the Hero section in `Features\Landing\Components\HeroWarmTealWash.razor`. The content must be engaging and targeted toward a broader spectrum of small businesses (including clinics, agencies, hospitality, retail, and local small businesses) without being overly specific to just one or two. It should emphasize "automation" and "workflow" while hitting the "less busywork, more time" theme. We will not alter any existing CSS, only HTML text and inline structural tweaks for spacing or typography hierarchy if necessary.

## Proposed Changes

### 1. Badge Update
Change from: "Build & Grow Model"
Update to: "AUTOMATION WORKFLOWS FOR SMALL BUSINESSES"
Reasoning: Broadens the appeal beyond just clinics and agencies while directly using the requested terms, making it clear what the service provides and who it's for.

### 2. Headline Refinement
Change from:
```html
<span class="font-bold text-teal-cyan-aqua-700">Every piece</span>
<span class="block text-teal-cyan-aqua-500 font-light italic">fits together.</span>
<span class="block font-bold text-gray-800">Finally, yours.</span>
```
Update to:
```html
<span class="font-bold text-teal-cyan-aqua-700">Smart systems.</span>
<span class="block text-teal-cyan-aqua-500 font-light italic">Less busywork.</span>
<span class="block font-bold text-gray-800">More time for growing.</span>
```
Reasoning: Retains the strong, direct messaging that resonates with small business owners looking to reduce administrative tasks, drawing inspiration from `HeroAlternate`. Replaced "clients" with "growing" to better encompass retail and hospitality as well (where they might use "customers" or "guests").

### 3. Subtext Enhancement
Change from:
"Custom systems built around your exact workflow — no generic templates, no one-size-fits-all boxes. Technology shaped for your business."
Update to:
"We build custom automation tools designed around your daily routine—whether you run a clinic, agency, hospitality venue, retail store, or any local small business. One partner, clear communication, and outcomes you can actually measure."
Reasoning: Incorporates a broader list of examples (clinic, agency, hospitality, retail, local small business) so it doesn't feel exclusive to just one vertical, while still pulling strong value propositions from the other hero components.

### 4. Stats Row Formatting
Update the labels to highlight tangible metrics and zero-stress implementation, aligning with themes seen in the other hero components.
- Stat 1: Update "Time Saved" to "Avg. Time Saved"
- Stat 2: Update "Flat Build" to "Flat Build Price"
- Stat 3: Change "0 Jargon" to "0 Stress" and change the sub-label to "Day-One Launch" (matching `HeroAlternate`).

**Progress**: 100% [██████████]

**Last Updated**: 2026-06-01 19:56:25

## 📝 Plan Steps
- ✅ **Modify `HeroWarmTealWash.razor`'s Badge text to "AUTOMATION WORKFLOWS FOR SMALL BUSINESSES".**
- ✅ **Update the Headline HTML text items to "Smart systems.", "Less busywork.", and "More time for growing."**
- ✅ **Update the Subtext paragraph to explicitly mention clinics, agencies, hospitality, retail, and local small businesses.**
- ✅ **Tweak the Stats row text and labels to emphasize "Avg. Time Saved", "Flat Build Price", and "0 Stress" / "Day-One Launch".**

## 🔧 Additional Improvements (post-plan)
- ✅ **Unified Stats Row visual pattern** — all stats now use teal value + orange unit + orange label (was inconsistent).
- ✅ **Added Bootstrap Icons** — `bi-hourglass-split`, `bi-tag`, `bi-rocket-takeoff` above each stat.
- ✅ **Separator height** increased from `h-12` → `h-16` to accommodate icons.

