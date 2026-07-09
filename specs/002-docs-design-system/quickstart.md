# Quickstart: Validate the Documentation Redesign

Runnable validation guide proving the design system is applied and the hard boundaries (content + MCP invariant) hold. Implementation details live in `tasks.md` (from `/speckit-tasks`); this file is how you build, run, and check.

## Prerequisites

- .NET 8 SDK
- (Optional) Cake for the Less build: `dotnet tool restore` or the global Cake tool
- A modern browser
- Drapo MCP + `drapo-resolver` skill available (for sample/symbol checks)

## Build & run

```bash
cd src/WebDocs
dotnet restore

# Compile the design system (Less -> wwwroot/css/theme.css). tokens.less is pulled in via theme.less @imports.
dotnet cake build.cake --target=less      # or the documented Cake invocation

dotnet run                                # https://localhost:5001 / http://localhost:5000
```

> Note: content/doc edits require a restart of `dotnet run` to appear (content is cached at startup). CSS changes need the Cake `less` step re-run, then a browser refresh.

## Validation scenarios

### V1 — Cohesive visual identity (FR-001, SC-001) [User Story 1]

1. Open the site and visit, in turn: a **Guide** page, a **Function** page (with a code sample + parameters table), an **Attributes** page, an **Applications** page, and trigger the **search** dropdown.
2. **Expect**: consistent typography, colors, spacing, buttons, tables, and surfaces on every view; modern appearance; no page left in the old look.

### V2 — Light & dark themes fully styled (FR-006/FR-007, SC-001/SC-005) [User Story 3]

1. Toggle the theme (header toggle). Persisted key: `localStorage['drapo-theme']`.
2. **Expect**: header, menu, content, parameter tables, code samples, search, and forms are all themed in **both** modes — no unstyled/low-contrast area.
3. Reload the page in dark mode. **Expect**: no flash of light/unstyled content before paint (synchronous `theme.js`).
4. Check body text and code contrast in both themes against a WCAG AA checker (≥4.5:1 body). **Expect**: pass.

### V3 — Tokens are the single source of truth (FR-004/FR-005, SC-004/SC-007) [User Story 2]

1. Open `styles/tokens.less`. **Expect**: semantic color/type/space/radius/shadow tokens, with `:root` (light) and `html.dark-theme` (dark) values.
2. Grep the refactored `styles/*.less` for raw literals in shared styling (hex colors, px spacing on shared components). **Expect**: shared styles reference `var(--…)` tokens, not literals.
3. Temporarily change `--color-accent`, re-run the `less` build, refresh. **Expect**: every accent usage (links, buttons, focus, active nav) updates consistently; revert afterward.

### V4 — New page inherits the system (SC-006) [User Story 2]

1. Create a throwaway content page using only documented patterns/tokens (a `.ds-card`, a `.btn`, a `.ds-callout`, a table) — no page-specific CSS.
2. **Expect**: it visually matches the rest of the site with zero bespoke styling. Delete it afterward.

### V5 — Content is invariant (FR-002, SC-002)

```bash
# From repo root, compare content-bearing files against the pre-redesign baseline (e.g. master).
git diff --stat master -- src/WebDocs/wwwroot/app src/WebDocs/wwwroot/components
```

1. **Expect**: no diffs under `wwwroot/app/menu/**`, `functions/*/description.html`, `functions/*/parameters.json`, or sample `content.html`. Any diff in `app/shared/*.html` or component templates is **presentation-only** (classes/structure), never wording.
2. For any sample that was touched incidentally: run `validate_drapo` on it and resolve its dataKeys/components/sectors via the `drapo-resolver` skill. **Expect**: pass.

### V6 — MCP / serving layer is invariant (FR-003, SC-003)

```bash
git diff --stat master -- src/WebDocs/Controllers src/WebDocs/Services src/WebDocs/Models
# Expect: empty (no serving-layer/MCP code changed).
dotnet build   # green (only relevant if any serving-layer file were touched)
```

1. Compare a sampling of MCP outputs (`get_functions`, `get_attributes`, a `*_details`, `validate_drapo` on a known snippet) before vs after. **Expect**: identical.

### V7 — Responsive & overflow behavior (FR-010) [Edge Cases]

1. Resize the browser from wide to narrow (mobile width).
2. **Expect**: the menu/content shell adapts (sidebar → stacked/toggleable menu); wide tables and long code samples scroll or wrap gracefully; long menu labels don't break layout; no horizontal page overflow.

### V8 — Dogfooding & build preserved (FR-008, FR-011)

1. Confirm no new heavy client dependency was added to `index.html` beyond existing links; theming is CSS-variable based; `theme.js` unchanged.
2. Confirm the site is produced by the documented `dotnet` + Cake `less` build.

## Done when

- V1–V8 all pass, and the reviewer subjectively confirms the site reads as modern/professional (SC-008).
