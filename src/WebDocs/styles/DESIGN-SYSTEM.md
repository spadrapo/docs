# Drapo Docs — Design System

This folder is authored in **Less** and compiled to `../wwwroot/css/theme.css` by
`build.cake` (`dotnet cake build.cake --target=less`). `theme.css` is generated —
never hand-edit it.

The design is **token-driven**: one source of truth (`tokens.less`) defines every
color, type size, space, radius, and shadow as a CSS custom property, with light
values on `:root` and dark values on `html.dark-theme`. The theme toggle
(`/js/theme.js`) just adds/removes the `dark-theme` class, so switching is instant
and needs no recompile.

## Layer order (defined in `theme.less`)

1. `tokens.less` — design tokens (primitives + semantic). **Source of truth.**
2. `base.less` — element defaults & typographic rhythm (body, headings, links, code).
3. `components.less` — reusable patterns (`.btn`, `.ds-card`, `.ds-callout`, `.ds-table`, forms, badges).
4. Feature stylesheets — `layout`, `menu`, `header`, `samples`, `content`, `functions`, `validator`.

Later layers may only **consume** tokens from step 1. Never write a raw color/spacing
literal in steps 2–4 — reference a semantic token instead.

## Using the system

- **Color / type / space / radius / shadow**: use the semantic tokens documented in
  `../../../specs/002-docs-design-system/contracts/design-system.md` §1
  (e.g. `var(--color-accent)`, `var(--space-4)`, `var(--radius-md)`,
  `var(--font-size-lg)`, `var(--elevation-sm)`). Do **not** reference primitive
  tokens (`--indigo-600`, `--gray-100`) or literals directly.
- **Components**: reuse the classes in `components.less` (`.btn` + variants,
  `.ds-card`/`.card`, `.ds-callout--info/success/warn/error`, `.ds-table`, `.badge`,
  form inputs). A new page built only from these matches the site with no extra CSS.
- **New feature stylesheet**: add the file, `@import` it after `components.less` in
  `theme.less`, and reference semantic tokens only. Rebuild with the Cake `less` task.

## Adding or changing a token

1. Add/adjust the value in `tokens.less` — primitive first, then a semantic alias.
2. Add the matching `html.dark-theme` value (names are identical across themes; only
   values differ).
3. Verify body-text and code contrast stay **WCAG AA** (≥ 4.5:1) in both themes.
4. Rebuild `theme.css`.

## Hard rules (from the project constitution + this feature's spec)

- **Presentation only.** Do not change documentation content, `parameters.json`, menu
  structure, or any `d-*` behavior. Shared-sector/component edits are class/structure
  only, with all `d-*` wiring preserved.
- **No serving-layer / MCP changes** ship from this folder.
- **Dogfooding.** Prefer CSS/token solutions; the theme toggle stays the existing
  synchronous `theme.js`. No new heavy client dependencies.

See `specs/002-docs-design-system/` (spec, plan, contracts, quickstart) for the full
rationale and validation scenarios.
