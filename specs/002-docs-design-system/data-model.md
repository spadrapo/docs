# Phase 1 Data Model: Design System Entities

This feature's "data model" is the **design system** itself — the named, structured styling entities from the spec (Design Token, Design Pattern / Component Style, Theme, Documentation Surface). They are realized as CSS custom properties and CSS class contracts, not as persisted/DB data. Concrete names are the contract in [contracts/design-system.md](./contracts/design-system.md).

## Entity: Design Token

A single named, reusable design decision.

| Field | Description | Example |
|-------|-------------|---------|
| `name` | CSS custom-property name (kebab-case, `--` prefixed) | `--color-accent` |
| `tier` | `primitive` (raw value) or `semantic` (references a primitive) | `semantic` |
| `category` | `color` \| `typography` \| `space` \| `radius` \| `shadow` \| `motion` | `color` |
| `valueLight` | Value in the light theme (`:root`) | `#2563eb` |
| `valueDark` | Value in the dark theme (`html.dark-theme`); may equal light | `#60a5fa` |

**Rules**:
- Every semantic token MUST resolve to a primitive token or another semantic token, never a bare literal in feature stylesheets (FR-005, SC-004).
- Color tokens used for body text and code MUST yield ≥ WCAG AA contrast against their paired surface token in **both** themes (FR-007, SC-005).
- Primitive tokens define the palette/scale once; feature stylesheets reference **semantic** tokens only.
- Dark values live solely in the `html.dark-theme` override block; toggling the class re-resolves all of them (D5, D1).

**Categories & expected members** (minimum set):
- **color**: surface/background layers, text (primary/muted/inverse), border, accent (+hover/active), success/warn/error/info, code background/foreground.
- **typography**: `--font-family-ui`, `--font-family-mono`, a type scale (`--font-size-xs…3xl`), line-heights, font-weights.
- **space**: a spacing step scale (`--space-1…8` or `xs…3xl`).
- **radius**: `--radius-sm/md/lg/pill`.
- **shadow**: `--shadow-sm/md/lg` (elevation).
- **motion**: standard transition duration/easing (matches existing 0.3s theme transition).

## Entity: Design Pattern / Component Style

A reusable styling of a recurring UI element, expressed only in terms of tokens.

| Field | Description | Example |
|-------|-------------|---------|
| `name` | Stable CSS class (or existing selector it restyles) | `.btn`, `.ds-callout` |
| `surface` | Which documentation surfaces use it | header, content, samples |
| `tokensUsed` | Semantic tokens it consumes | `--color-accent`, `--radius-md`, `--space-md` |
| `states` | Interactive/visual states styled | default, hover, active, focus, disabled |
| `themes` | Must be correct in both | light + dark |

**Required patterns** (the recurring UI from FR-004): buttons/controls (incl. existing `.btn`, `.theme-toggle`), tables (incl. `.dFunctionParametersTable`), code blocks (`code` component), callouts/notes, cards/panels, navigation items (`.dMenu*`), search results (`.search-*`), form inputs (`.search-input`, `.form-group`), validation states (`.ppValidation*`).

**Rules**:
- A pattern MUST NOT hard-code color/spacing/radius/shadow literals; it references semantic tokens (FR-005).
- Each interactive pattern MUST define a visible focus state (accessibility).
- Restyling an existing selector MUST preserve the class name/behavior it is bound to in Drapo markup (D7).

## Entity: Theme

A named, wholesale set of token values applied to the site.

| Field | Description | Value |
|-------|-------------|-------|
| `name` | Theme identifier | `light`, `dark` |
| `activation` | How it is applied | `light` = `:root` default; `dark` = `html.dark-theme` class |
| `persistence` | Where user choice is stored | `localStorage['drapo-theme']` (unchanged) |
| `toggle` | Mechanism | existing synchronous `theme.js` (`toggleTheme`), unchanged (D5) |

**Rules**:
- Both themes MUST fully style every pattern — no unthemed or low-contrast component (FR-006, SC-001).
- Theme application MUST NOT flash wrong/unstyled content on load (synchronous head script preserved).
- The set of token *names* is identical across themes; only values differ.

## Entity: Documentation Surface

A human-facing view targeted by the design system; its **content is invariant**.

| Surface | Rendered by | Redesign touch | Content touch |
|---------|-------------|----------------|---------------|
| Header + search | `app/shared/header.html` sector | presentation markup + `header.less` | none |
| Navigation menu | `app/shared/menu.html` sector | presentation markup + `menu.less` | none (menu items from `MenuController`) |
| Content shell | `index.html` `.doc` + `layout.less` | responsive layout, tokens | none |
| Footer | `app/shared/footer.html` sector | presentation markup | none |
| Guide/Data/Attributes/Functions/Debugging/Applications pages | `wwwroot/app/menu/**` via services | **CSS/token only** (no page-file edits) | none |
| Function reference + parameters | `functions/**`, `functions.less` | table pattern/tokens | none (`description.html`/`parameters.json` unchanged) |
| Code samples | `components/code`, `components/sample`, `samples.less` | container/code presentation via tokens | sample markup unchanged (revalidated) |
| Validator states | `validator.less` | tokens | none |

**Rules**:
- No file under `wwwroot/app/menu/**`, `functions/*/description.html`, `functions/*/parameters.json`, or sample `content.html` is edited for content (FR-002, SC-002).
- No `Controllers/`, `Services/`, `Models/`, or MCP exposure is edited (FR-003, SC-003).

## Relationships

```text
Theme (light|dark)
  └── sets values of ──> Design Token (semantic)
                              └── references ──> Design Token (primitive)
Design Pattern ── consumes ──> Design Token (semantic)
Documentation Surface ── styled by ──> Design Pattern
                       ── content is ──> INVARIANT (not part of this model)
```
