# Contract: Design System Token & Component API

This is the interface the redesign exposes **to maintainers** (not to end users or AI agents). It defines the stable names a maintainer relies on so a new page inherits the site's look with zero bespoke styling (FR-011, SC-006). Names below are the committed surface; values are illustrative and finalized during implementation. All tokens are CSS custom properties defined in `styles/tokens.less` (`:root` for light, `html.dark-theme` for dark).

## 1. Token contract

### 1.1 Color — semantic (feature stylesheets use ONLY these)

| Token | Meaning | Light (illustrative) | Dark (illustrative) |
|-------|---------|----------------------|---------------------|
| `--color-bg` | Page background | `#ffffff` | `#0f1524` |
| `--color-surface` | Card/panel/menu surface | `#f8f9fa` | `#16213e` |
| `--color-surface-raised` | Elevated surface (dropdowns, search) | `#ffffff` | `#1c2740` |
| `--color-border` | Default border/divider | `#e2e6ea` | `#2d3748` |
| `--color-text` | Primary text | `#1a1a1a` | `#e6e6e6` |
| `--color-text-muted` | Secondary/label text | `#5a6472` | `#aab2c5` |
| `--color-text-inverse` | Text on accent | `#ffffff` | `#0f1524` |
| `--color-accent` | Primary accent/links/CTAs | `#2563eb` | `#60a5fa` |
| `--color-accent-hover` | Accent hover/active | `#1d4ed8` | `#93c5fd` |
| `--color-focus-ring` | Focus outline | `rgba(37,99,235,.4)` | `rgba(96,165,250,.5)` |
| `--color-code-bg` | Code block background | `#f4f6f8` | `#111a2e` |
| `--color-code-text` | Code foreground | `#1a1a1a` | `#e6e6e6` |
| `--color-success` / `--color-warning` / `--color-error` / `--color-info` | Status | — | — |

> Contrast rule: `--color-text` on `--color-bg`/`--color-surface`, and `--color-code-text` on `--color-code-bg`, MUST meet WCAG AA (≥4.5:1 body, ≥3:1 large) in **both** themes (SC-005).

### 1.2 Typography

| Token | Meaning |
|-------|---------|
| `--font-family-ui` | Body/UI font stack (system-ui based, no CDN) |
| `--font-family-mono` | Code font stack (self-hosted/system mono) |
| `--font-size-xs … --font-size-3xl` | Type scale steps |
| `--line-height-tight` / `--line-height-normal` / `--line-height-loose` | Line heights |
| `--font-weight-normal` / `--font-weight-medium` / `--font-weight-bold` | Weights |

### 1.3 Spacing / radius / shadow / motion

| Token group | Members |
|-------------|---------|
| Spacing | `--space-1 … --space-8` (or `--space-xs … --space-3xl`) |
| Radius | `--radius-sm`, `--radius-md`, `--radius-lg`, `--radius-pill` |
| Shadow | `--shadow-sm`, `--shadow-md`, `--shadow-lg` |
| Motion | `--transition-base` (e.g. `0.3s ease`, matching current theme transition) |

### 1.4 Primitive tokens (referenced only by semantic tokens)

Raw palette ramps and raw scales (e.g. `--blue-500`, `--gray-100`, `--size-2` …). Feature stylesheets MUST NOT reference primitives directly — only semantic tokens (SC-004).

## 2. Component-style contract

Stable class hooks a maintainer may use; each is fully styled in both themes and uses only semantic tokens.

| Class / selector | Purpose | Required states |
|------------------|---------|-----------------|
| `.btn` (+ `.btn-ghost`, `.btn-secondary`, `.btn-small`, `.btn-block`) | Buttons/controls (existing usages restyled) | default, hover, active, focus, disabled |
| `.theme-toggle` | Light/dark toggle button (existing) | default, hover, focus |
| `.dFunctionParametersTable`, `.data-table` | Data/parameter tables | header, row, zebra, hover |
| `.ds-callout` (+ `--info`/`--warn`/`--success`/`--error`) | Note/callout blocks | per status variant |
| `.ds-card` / `.card` | Panel/card surface | default, raised |
| `.dMenu`, `.dMenuTab`, `.dMenuGroupExpanded/Collapsed/None` | Navigation items (existing) | default, hover, active, expanded/collapsed |
| `.search-container`, `.search-input`, `.search-results`, `.search-item*` | Search UI (existing) | default, focus, hover, active item |
| `code` component / `.ds-code` | Code blocks | light + dark, overflow scroll |
| `.form-group`, inputs | Form controls | default, focus, invalid |
| `.ppValidation*` | Validation states (existing) | valid/invalid/unchecked |

**Guarantees**:
1. Using a class/token above yields the site's standard look with no extra CSS (SC-006).
2. Restyled existing selectors keep their exact names so Drapo `d-*` markup binding is unaffected (D7).
3. Every interactive component exposes a visible `:focus`/`:focus-visible` ring via `--color-focus-ring`.

## 3. Invariance contract (what this feature MUST NOT change)

| Guaranteed unchanged | Verification |
|----------------------|--------------|
| All documentation text, samples, `parameters.json`, menu labels & ordering | Content diff shows only presentation-layer changes (SC-002) |
| MCP responses (`get_*`, `*_details`, `validate_drapo`) & serving-layer behavior | MCP/response diff before vs after is empty (SC-003) |
| Drapo `d-*` semantics, sectors, dataKeys, event wiring | Pages render/behave identically; `validate_drapo` + drapo-resolver pass |
| Documented build (dotnet + Less/Cake) & `theme.js` toggle behavior | `dotnet run` + Cake `less` produce the styled site; toggle persists `drapo-theme` |

## 4. Consumption pattern (for `/speckit-tasks` and maintainers)

1. Author `styles/tokens.less` with §1 tokens (light in `:root`, dark in `html.dark-theme`).
2. `@import` order in `theme.less`: `tokens` → `base` → `components` → existing feature stylesheets.
3. Refactor each existing `styles/*.less` to replace literals with §1 semantic tokens (no visual literal left — SC-004).
4. Add §2 patterns in `components.less`; restyle existing selectors in place.
5. Apply responsive shell in `layout.less` (D4); restyle shared sectors' presentation markup only (D7).
6. Validate against [quickstart.md](../quickstart.md).
