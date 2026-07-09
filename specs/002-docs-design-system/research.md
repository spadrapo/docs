# Phase 0 Research: Documentation Design System & Visual Redesign

All decisions below are constrained by the spec's hard boundaries — **no content change, no MCP/serving-layer change** — and the constitution's Simplicity & Dogfooding principle. There were no `NEEDS CLARIFICATION` markers in the spec; the items here resolve the open *implementation* choices the plan implied.

## D1 — Where and how design tokens live

**Decision**: Define tokens as **CSS custom properties** in a new `styles/tokens.less`, split into two tiers: (a) primitive tokens (raw palette, type sizes, spacing steps, radii, shadows) and (b) semantic tokens (`--color-surface`, `--color-text`, `--color-accent`, `--space-md`, etc.) that reference primitives. Light values live in `:root`; dark overrides live in `html.dark-theme`. `theme.less` `@import`s `tokens.less` **first**.

**Rationale**: The site already themes via CSS custom properties on `:root` / `html.dark-theme` and toggles `html.dark-theme` through the synchronous `theme.js`. Custom properties are the least-risk, dogfooding-friendly mechanism — runtime theme switch with zero added JS, and changing one token cascades everywhere (satisfies FR-005/SC-007). Less compiles them fine via dotless; using CSS variables (not Less `@variables`) keeps values overridable per-theme at runtime.

**Alternatives considered**: (1) Less `@variables` only — rejected: resolved at compile time, cannot switch theme at runtime without recompiling / duplicate stylesheets. (2) A JS design-token runtime / CSS-in-JS — rejected: violates Simplicity & Dogfooding, adds a dependency for no gain. (3) A third-party CSS framework (Tailwind, etc.) — rejected: heavy new dependency and build change, conflicts with the "keep the documented build" constraint.

## D2 — Keep or remove Bootstrap

**Decision**: **Keep** `bootstrap.min.css` linked in `index.html`, loaded **before** `theme.css`; the design system overrides the handful of Bootstrap components actually used and owns the visual language. Do not attempt a full Bootstrap removal in this feature.

**Rationale**: An audit shows only light Bootstrap reliance (`.btn`, `.form-group`, `.container`, `.badge`, `.alert`, a few table utilities), much of it inside Applications demo pages. Removing Bootstrap risks silent visual breakage across many content pages for little benefit, and content pages are invariant here. Layering the design system after Bootstrap gives full control of the look while keeping risk near zero. A later, separate feature can retire Bootstrap once every usage is mapped to a design-system pattern.

**Alternatives considered**: (1) Remove Bootstrap now — rejected: broad regression risk on invariant content pages, out of proportion to a visual-only feature. (2) Replace Bootstrap with another utility framework — rejected: new dependency, larger blast radius.

## D3 — Typography / fonts

**Decision**: Use a **self-hosted or system font stack** — a modern UI stack (`system-ui`/`-apple-system`/`Segoe UI`/`Roboto`…) for body/UI and a self-hosted monospace for code. No external font CDN. Expose sizes as type-scale tokens (`--font-size-*`, `--line-height-*`, `--font-family-ui`, `--font-family-mono`).

**Rationale**: The current `font: 14px 'Open Sans'` has no bundled/self-hosted Open Sans, so it silently falls back. A system-UI stack is fast, dependency-free, looks modern, and honors Simplicity (no external request, no CSP/privacy concern). Self-hosting mono (only if a system mono is insufficient) keeps code legible without a CDN.

**Alternatives considered**: (1) Google Fonts CDN — rejected: external dependency + privacy/latency, against Simplicity. (2) Keep 14px Open Sans reference — rejected: it doesn't load and is part of the "ugly" baseline.

## D4 — Responsive layout for the doc shell

**Decision**: Replace the fixed `.docLeft { width:15% }` / `.docRight { width:80% }` split with a **token-driven responsive layout** (fl/grid): a sidebar menu + fluid content on wide viewports, collapsing to a stacked/toggleable menu on narrow viewports, using spacing tokens and a small set of breakpoints. Preserve the Drapo sector structure (`dSectorMenu`, `dSectorContent`) and all `d-*` wiring.

**Rationale**: "Better design" includes not overflowing or wasting space; the current percentages leave a ~5% dead gutter and don't adapt to mobile. The header already has one `@media (max-width:768px)` rule, so a breakpoint approach is consistent. Changing layout CSS/classes does not change Drapo behavior or content.

**Alternatives considered**: (1) Keep fixed percentages — rejected: not modern, poor on mobile (spec Edge Cases + Assumptions call for responsive). (2) Introduce a JS-driven layout — rejected: unnecessary; CSS grid/flex + a CSS-only menu toggle suffice, preserving dogfooding.

## D5 — Theme toggle & flash avoidance

**Decision**: **Keep `theme.js` exactly as-is** (synchronous `<head>` script that applies the stored theme before paint and exposes `toggleTheme`). The design system only supplies the token values for each theme; the toggle mechanism and persisted preference (`localStorage 'drapo-theme'`) are unchanged.

**Rationale**: The existing script already prevents a flash of wrong theme (it runs before body render) and satisfies FR-006's "preserve existing toggle behavior." Re-implementing it would add risk with no benefit.

**Alternatives considered**: (1) `prefers-color-scheme` auto-detection — could be added as an enhancement to the default when no stored preference exists, but is optional and must not override an explicit user choice; deferred to keep scope tight. (2) Rewrite the toggle in Drapo — deferred; current script is minimal and works.

## D6 — Build & delivery mechanism

**Decision**: Keep `build.cake`'s `less` task compiling `styles/theme.less` → `wwwroot/css/theme.css` unchanged. New files are pulled in purely via `@import` order inside `theme.less` (tokens → base → components → feature stylesheets). `wwwroot/css/theme.css` remains a generated artifact and is not hand-edited.

**Rationale**: Honors "keep the documented build." Adding files through `@import` needs no Cake change since only `theme.less` is compiled and it aggregates the rest.

**Alternatives considered**: (1) Add a CSS bundler/PostCSS pipeline — rejected: new tooling, against Simplicity. (2) Author raw CSS instead of Less — rejected: breaks the documented Less workflow and existing `@import` structure.

## D7 — Scope of markup edits (sectors & components)

**Decision**: Limit HTML edits to **presentation** in the shared sectors (`app/shared/header.html`, `menu.html`, `footer.html`) and the `code`/`sample` components — class names, wrapper structure, and design-system hooks — while keeping every `d-*` attribute, `d-sector`, `d-dataKey`, and event binding semantically identical. No edits to `wwwroot/app/menu/**` doc pages, `functions/**`, `parameters.json`, or component *behavior*.

**Rationale**: These sectors/components are the shell that every page renders inside; restyling them delivers most of the visual win with no content-file changes. Keeping `d-*` wiring intact preserves Principle II/III and MCP invariance. Any incidental change touching a sample re-runs `validate_drapo` and the drapo-resolver checks.

**Alternatives considered**: (1) Touch individual content pages for styling — rejected: content is invariant; per-page styling would also decay the design system. (2) Leave sectors as-is and only change CSS — viable for pure restyle, but small structural/class additions in sectors are needed for modern header/menu/responsive affordances, so limited presentation edits are allowed.

## Open questions / deferred (non-blocking)

- **`prefers-color-scheme` default** (D5 alt 1) — optional enhancement, deferred.
- **Bootstrap retirement** (D2) — explicitly a future, separate feature.
- **Which exact accent/brand palette** — chosen during implementation from Drapo brand cues (logo/existing accent `#007bff`); not a scope-affecting decision, captured as tokens.
