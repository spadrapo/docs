# Implementation Plan: Documentation Design System & Visual Redesign

**Branch**: `002-docs-design-system` | **Date**: 2026-07-09 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/002-docs-design-system/spec.md`

## Summary

Establish a **design-token-based design system** and apply it across the Drapo documentation site so the site looks modern, consistent, and professional — in both light and dark themes — **without changing any documentation content or the MCP/serving layer**. The approach is CSS-first and dogfooding-preserving: introduce a single tokens layer as CSS custom properties (`tokens.less`), define semantic aliases and reusable component styles on top of it, refactor the existing ad-hoc `styles/*.less` files to consume tokens instead of hard-coded values, restyle the shared Drapo sectors (header, menu, content shell, footer) for a modern layout including responsive behavior, and keep the existing Cake/dotless Less→CSS build and the synchronous `theme.js` toggle. No `d-*` markup semantics, sample content, `parameters.json`, menu structure, controllers, services, or MCP output change.

## Technical Context

**Language/Version**: HTML5 + CSS (authored as Less, compiled by dotless.Core). Serving app is ASP.NET Core on .NET 8 (C#) — **unchanged** by this feature. Frontend is Drapo (`d-*` attributes, sectors, components), engine served at `/drapo.js` from `Sysphera.Middleware.Drapo`.

**Primary Dependencies**: `Sysphera.Middleware.Drapo` (engine, unchanged); `dotless.Core` via `build.cake` (Less→CSS); Bootstrap (`bootstrap.min.css`, currently linked in `index.html`); `theme.js` (light/dark toggle, synchronous head script). No new runtime dependency is planned; fonts are self-hosted or system stack (no external CDN) to honor Simplicity.

**Storage**: N/A. Documentation content is convention-bound static files under `wwwroot/app/**` and `wwwroot/components/**`; this feature does not read, write, or reshape them.

**Testing**: Manual visual validation via the running site (light + dark, multiple sections, narrow/wide viewports); `validate_drapo` for any touched sample (samples are expected to be untouched); `dotnet build` green if any serving-layer file were touched (none expected); content-diff and MCP-response-diff to prove invariance (SC-002/SC-003).

**Target Platform**: Modern evergreen browsers, desktop and mobile widths (responsive).

**Project Type**: Web application — ASP.NET Core serving a Drapo SPA. Styling lives in `src/WebDocs/styles/*.less` → compiled to `src/WebDocs/wwwroot/css/theme.css`; shared UI in `wwwroot/app/shared/*.html` (sectors) and `wwwroot/components/*`.

**Performance Goals**: No added client JS beyond the existing toggle; CSS-only theming; no flash of unstyled/wrong-theme content (preserve synchronous `theme.js`); theme switch applies instantly and page-wide.

**Constraints**: Content invariant (FR-002); MCP/serving-layer invariant (FR-003); dogfooding preserved — Drapo-native rendering, .NET 8 / Less-Cake build kept (FR-008); WCAG AA text/code contrast in both themes (FR-007); every shared style traces to a named token (FR-005).

**Scale/Scope**: 6 doc sections (Guide, Data, Attributes, Functions, Debugging, Applications) + shell (header/menu/content/footer/search); 8 existing `styles/*.less` files; 3 components (`code`, `executor`, `sample`); one new token layer + semantic aliases + component-style layer.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Gate | Status |
|-----------|------|--------|
| I. Accuracy Over the Real Engine (NON-NEGOTIABLE) | Feature must not add/alter any documented attribute, function, parameter, or behavior. | **PASS** — presentation-only; no reference content touched. |
| II. Content Is Structured Data | Must not rename/move/reshape convention-bound content files or break service discovery. | **PASS** — only `styles/*.less`, compiled CSS, and shared sector/component *presentation* markup change; discovery paths and `parameters.json`/menu numbering untouched. |
| III. Every Example Is Valid, Runnable Drapo | Samples must remain valid Drapo. | **PASS** — sample markup/content unchanged; restyling is CSS-class/token level. Any incidental sample touch re-runs `validate_drapo`. |
| IV. Docs ↔ Tooling Parity | A content-shape change must ship its Service/`*VM`/Controller/MCP counterpart. | **PASS (N/A)** — no content-shape change, so no MCP/serving-layer counterpart is required; conversely no serving-layer code is modified. |
| V. Simplicity & Dogfooding | Prefer Drapo-native + CSS over JS; add deps only with stated reason; keep documented build. | **PASS** — no new heavy dependency; Less/Cake build kept; theming stays CSS-variable + existing `theme.js`; Bootstrap retained (not added) and layered over, avoiding a risky rip-out. |

**Result**: No violations. Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/002-docs-design-system/
├── plan.md              # This file
├── research.md          # Phase 0 output — design decisions
├── data-model.md        # Phase 1 output — token/pattern/theme model
├── quickstart.md        # Phase 1 output — how to build, run, and validate the redesign
├── contracts/
│   └── design-system.md # Phase 1 output — the token & component-style contract
└── checklists/
    └── requirements.md  # Spec quality checklist (from /speckit-specify)
```

### Source Code (repository root)

```text
src/WebDocs/
├── styles/                      # Less sources (compiled by build.cake → wwwroot/css/theme.css)
│   ├── theme.less               # entry: @import order — tokens FIRST, then layers
│   ├── tokens.less              # NEW — design tokens: color/type/space/radius/shadow as CSS custom properties (:root + html.dark-theme)
│   ├── base.less                # NEW (or fold into layout) — element defaults, typography scale, resets built on tokens
│   ├── components.less          # NEW — reusable patterns: buttons, tables, cards, callouts, code blocks, nav items, search results
│   ├── layout.less              # REFAC — app shell + responsive doc grid (replaces fixed 15%/80%); tokens replace :root literals
│   ├── header.less              # REFAC — consume tokens; modern header/search styling
│   ├── menu.less                # REFAC — modern nav items; responsive/collapsible menu
│   ├── content.less             # REFAC — content typography rhythm via tokens
│   ├── functions.less           # REFAC — parameter tables via table pattern/tokens
│   ├── samples.less             # REFAC — sample surfaces via tokens
│   └── validator.less           # REFAC — validation states via tokens
├── wwwroot/
│   ├── index.html               # link order: bootstrap → theme.css (design system overrides Bootstrap); theme.js kept in <head>
│   ├── css/theme.css            # BUILD OUTPUT (generated; do not hand-edit)
│   ├── app/shared/
│   │   ├── header.html          # REFAC — presentation markup only (classes/structure), same behavior/d-* wiring
│   │   ├── menu.html            # REFAC — presentation markup only; responsive affordance
│   │   └── footer.html          # REFAC — presentation markup only
│   └── components/
│       ├── code/                # REFAC — code block presentation via tokens
│       └── sample/              # REFAC — sample container presentation via tokens
└── build.cake                   # UNCHANGED mechanism — compiles styles/theme.less (which now @imports tokens first)
```

**Structure Decision**: Keep the existing single-project ASP.NET Core layout and the Less→CSS Cake pipeline. Introduce a **layered stylesheet architecture** inside `styles/`: `tokens.less` (single source of truth) → `base.less`/typography → `components.less` (patterns) → existing feature stylesheets refactored to consume tokens. `theme.less` `@import`s tokens first so every downstream layer and the light/dark `html.dark-theme` overrides resolve against named tokens. Shared sectors and components are restyled at the class/structure level without altering their Drapo (`d-*`) behavior. Bootstrap stays linked but the design system, loaded after it, is the authority on look.

## Complexity Tracking

> Not required — Constitution Check has no violations.
