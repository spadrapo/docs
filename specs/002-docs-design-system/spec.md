# Feature Specification: Documentation Design System & Visual Redesign

**Feature Branch**: `002-docs-design-system`

**Created**: 2026-07-09

**Status**: Draft

**Input**: User description: "in the current folder we have the drapo docs repo. It design is ugly as hell. we should use claude design to have a better design. we dont want to change the info or the MCP that we have over there. Just the design of it. maybe se should create a design system and use claude design to it."

## Overview

The Drapo documentation site presents accurate, valuable content but looks dated and unpolished, which undermines the credibility of the framework it documents. This feature is a **purely visual redesign**: it establishes a cohesive **design system** (color, typography, spacing, component styles, light/dark theming) and applies it across the documentation website so the site looks modern, professional, and consistent.

**Explicitly out of scope**: the documentation *content* (function/attribute/concept text, samples, parameters), the information architecture (menu structure, page organization), and the **MCP server** and all serving-layer behavior that exposes content to AI agents. No documented fact, symbol, sample, or API response changes — only how the human-facing site *looks*.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A cohesive, modern visual identity across the site (Priority: P1)

A developer visiting the Drapo docs experiences a single, consistent, modern visual language on every page — the same typography scale, color palette, spacing rhythm, and component styling — instead of the current inconsistent, dated look. Headers, navigation menu, content pages, code samples, tables, and the search UI all share one coherent design.

**Why this priority**: This is the core of the request — "the design is ugly, make it better." A consistent visual identity is the minimum viable outcome that delivers the perceived quality improvement, and it can be shipped on its own.

**Independent Test**: Open the running site and navigate across the header, menu, a Guide page, a Function reference page (with samples), and the search results. Confirm that typography, colors, spacing, buttons, and surfaces are visually consistent and modern on every view, and that no documentation text or sample has changed.

**Acceptance Scenarios**:

1. **Given** the redesigned site is running, **When** a visitor moves between a Guide page, a Function page, and an Attributes page, **Then** the visual styling (fonts, colors, spacing, component appearance) is consistent across all of them.
2. **Given** any documentation page, **When** it is compared to the same page before the redesign, **Then** the wording, samples, parameters, and menu items are byte-for-byte identical while only presentation differs.
3. **Given** a page containing a code sample, **When** it is viewed after the redesign, **Then** the sample renders with the new, legible code styling and the sample content is unchanged.

---

### User Story 2 - A documented, reusable design system (Priority: P2)

A maintainer needs to add or restyle a page and can rely on a **defined design system** — named design tokens (colors, typography, spacing, radii, shadows) and reusable styled patterns (buttons, tables, cards, code blocks, callouts, navigation items) — rather than inventing one-off styles. New pages inherit the look automatically by using the system.

**Why this priority**: The user explicitly asked to "create a design system." Without it, the redesign decays back into inconsistency as content grows. It builds on P1 (the visual identity is the raw material the system codifies) but is separable — the site can look good first, then be refactored into tokens.

**Independent Test**: Review the design-system definition (tokens and documented reusable patterns). Confirm each visual decision in the site traces to a named token/pattern rather than a scattered hard-coded value, and that a new sample page built only from the system matches the site's look with no bespoke styling.

**Acceptance Scenarios**:

1. **Given** the design system exists, **When** a maintainer inspects the site's colors, type, and spacing, **Then** each is defined once as a named token and referenced everywhere it is used.
2. **Given** the design system exists, **When** a maintainer creates a new content page using only the documented patterns, **Then** it visually matches the rest of the site without additional custom styling.
3. **Given** the design system exists, **When** a single token value (e.g., the primary accent color) is changed, **Then** every place that uses it updates consistently across the site.

---

### User Story 3 - Refined, accessible light and dark themes (Priority: P3)

A visitor can use the site comfortably in both light and dark modes, each polished as part of the design system, with legible contrast and consistent theming across all components — including the ones (tables, code samples, search, forms) that currently look inconsistent between modes.

**Why this priority**: The site already has a light/dark toggle, so this refines existing behavior rather than adding a brand-new capability. It is valuable but the redesign is still worthwhile if only one theme were polished first.

**Independent Test**: Toggle between light and dark modes on the header, menu, content, tables, code samples, and search. Confirm every component is fully and consistently themed with legible contrast in both modes.

**Acceptance Scenarios**:

1. **Given** the redesigned site, **When** a visitor toggles to dark mode, **Then** every component (header, menu, content, tables, code, search, forms) is styled for dark mode with no unstyled or low-contrast areas.
2. **Given** either theme, **When** a visitor reads body text and code, **Then** text/background contrast meets a recognized accessibility standard for legibility.
3. **Given** the theme toggle, **When** a visitor switches themes, **Then** the transition applies to the whole page consistently and the selected preference behaves as it did before the redesign.

---

### Edge Cases

- **Long/overflowing content**: How do very long code samples, wide tables, and long menu labels behave under the new spacing and typography — do they wrap or scroll gracefully without breaking layout?
- **Small viewports**: How does the redesigned layout adapt on narrow/mobile widths (the current layout uses fixed percentage columns for menu and content)?
- **Dogfooding constraint**: The site is itself built with Drapo; the redesign must not require abandoning Drapo-native rendering of components/sectors in favor of hand-written scripting.
- **Mixed content authored before the system**: Existing pages authored with ad-hoc inline styles should still render acceptably (or be reconciled) under the new system.
- **Theme flash**: Does switching or loading a theme avoid a jarring flash of unstyled/wrong-theme content?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The site MUST present a single, consistent modern visual identity (typography, color, spacing, component styling) across all human-facing views: header, navigation menu, content pages, function/attribute/concept reference pages, code samples, tables, forms, and search.
- **FR-002**: The redesign MUST NOT alter any documentation content — the text, samples, parameters, menu labels, ordering, and information architecture remain exactly as they are; only presentation changes.
- **FR-003**: The redesign MUST NOT alter the MCP server or any serving-layer behavior that exposes content to AI agents (functions, attributes, concepts, data types, validation). MCP responses remain identical.
- **FR-004**: A defined **design system** MUST exist consisting of named design tokens (at minimum: color palette, typography scale, spacing scale, corner radii, elevation/shadow) and reusable styled patterns for the site's recurring UI (buttons/controls, tables, code blocks, callouts/notes, cards/panels, navigation items, search results).
- **FR-005**: Every visual style applied on the site MUST derive from the design system's tokens/patterns rather than scattered one-off values, such that changing a token updates all its usages consistently.
- **FR-006**: The site MUST support polished **light and dark themes** defined within the design system, with every component fully themed in both modes; the existing theme-toggle behavior MUST be preserved.
- **FR-007**: Body text and code MUST meet a recognized text-contrast accessibility standard in both light and dark themes.
- **FR-008**: The redesign MUST preserve the existing dogfooding approach — the site continues to be rendered with Drapo (components, sectors, `d-*` attributes) and MUST NOT introduce hand-written JavaScript where a Drapo-native or CSS solution suffices.
- **FR-009**: Code samples MUST render with legible, modern code styling while keeping the sample markup/content unchanged and any existing sample validity intact.
- **FR-010**: The redesigned layout MUST behave gracefully with overflowing content (wide tables, long code, long labels) and across viewport widths, without broken or overlapping layout.
- **FR-011**: The design system MUST be documented for maintainers so that new pages/components can adopt it consistently, and the site build (as currently documented) MUST continue to produce the styled site.

### Key Entities *(include if feature involves data)*

- **Design Token**: A single named, reusable design decision (e.g., a color, a font size, a spacing step, a radius, a shadow). Attributes: name, category, value(s) per theme.
- **Design Pattern / Component Style**: A reusable styling of a recurring UI element (button, table, code block, callout, card, nav item, search result) expressed in terms of tokens.
- **Theme**: A named set of token values (light, dark) applied wholesale to the site.
- **Documentation Surface**: A human-facing view being styled (header, menu, content page, function/attribute page, sample, search) — the target of the design system, whose *content* is invariant.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of human-facing views (header, menu, content, reference pages, samples, tables, forms, search) render with the new design system in both light and dark modes, with zero components left in the old/unstyled look.
- **SC-002**: 100% of documentation content (text, samples, parameters, menu items, ordering) is unchanged after the redesign, verifiable by a content diff showing only presentation-layer changes.
- **SC-003**: 100% of MCP responses and validation behavior are unchanged after the redesign, verifiable by comparing MCP/serving-layer output before and after.
- **SC-004**: Every color, type size, and spacing value used on the site maps to a named design token (no orphan hard-coded values in the shared styling), verifiable by inspecting the token definitions and their usage.
- **SC-005**: Body-text and code contrast pass a recognized accessibility contrast threshold (e.g., WCAG AA) in both themes.
- **SC-006**: A new content page built solely from the documented design system visually matches the rest of the site with no page-specific custom styling.
- **SC-007**: Changing a single core token (e.g., primary accent) visibly and consistently updates every usage across the site with no missed elements.
- **SC-008**: The site builds and runs using the currently documented build process, and the reviewer subjectively confirms the site reads as "modern and professional" versus the prior look.

## Assumptions

- **Visual-only scope**: "We don't want to change the info or the MCP" is taken literally — this feature touches presentation (styling, theming, layout polish, design-system definition) only. Any content or serving-layer change is explicitly out of scope and would be a separate feature.
- **"Use Claude design" / "claude design"** is interpreted as *use strong, opinionated modern design guidance to craft the look and the design system* — not as adopting any specific external product; the spec stays technology-agnostic about how the styling is authored.
- **Existing theming preserved and extended**: The site already has a light/dark toggle and CSS-variable-based theming; the design system formalizes and polishes this rather than replacing the toggle behavior.
- **Dogfooding preserved**: The redesign stays within the constitution's Simplicity & Dogfooding principle — Drapo-native rendering and the documented .NET 8 / Less-based build remain; no new heavy dependencies without justification.
- **All sections in scope**: The redesign applies to the entire site (Guide, Data, Attributes, Functions, Debugging, Applications, and the shell/header/menu/search), not a single section.
- **Responsive is expected**: Modern look includes graceful behavior on narrow viewports; the current fixed-percentage layout is assumed to be improvable as part of "better design."
- **Baseline for "before"**: The current styling (Bootstrap + ad-hoc Less files + hand-rolled CSS variables) is the "ugly" baseline the redesign replaces or subsumes.
