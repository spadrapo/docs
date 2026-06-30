<!--
Sync Impact Report
==================
Version change: (unversioned template) → 1.0.0
Bump rationale: First ratification of the project constitution (MAJOR baseline).

Principles defined:
  I.   Accuracy Over the Real Engine (NON-NEGOTIABLE)
  II.  Content Is Structured Data
  III. Every Example Is Valid, Runnable Drapo
  IV.  Docs ↔ Tooling Parity
  V.   Simplicity & Dogfooding

Sections defined:
  - Technology & Structure Constraints
  - Development Workflow & Quality Gates
  - Governance

Templates reviewed for alignment:
  ✅ .specify/templates/plan-template.md   (Constitution Check gate — principles are checkable as-is)
  ✅ .specify/templates/spec-template.md   (no mandatory-section changes required)
  ✅ .specify/templates/tasks-template.md  (task categories cover content + tooling + validation)

Runtime guidance synchronized:
  ✅ CLAUDE.md                       (codebase guide references these principles)
  ✅ .github/copilot-instructions.md (codebase guide references these principles)

Deferred / TODO: none.
-->

# Drapo Documentation Constitution

This repository (`spadrapo/docs`) is the official documentation for **Drapo** — a declarative
.NET frontend framework. It is two things at once: (1) a body of structured documentation content,
and (2) an ASP.NET Core application that serves that content to humans **and** to AI agents through
an MCP server. These principles govern both faces.

## Core Principles

### I. Accuracy Over the Real Engine (NON-NEGOTIABLE)

Every documented attribute, function, parameter, default, and behavior MUST reflect the **actual
bundled Drapo engine** — the `drapo.js` embedded in the `Sysphera.Middleware.Drapo` assembly and
surfaced by `DrapoEngineCatalog`.

- Do NOT document an attribute or function the engine does not dispatch.
- Before adding or changing a reference page, verify it against the engine: `DrapoEngineCatalog`,
  the `DrapoValidatorService` / `validate_drapo` tool, and the Drapo MCP (`get_attributes`,
  `get_functions`, `get_*_details`).
- When the engine and a doc disagree, the engine wins; the doc is corrected (see commit history:
  "Fix inaccurate attribute docs found auditing against the engine").

Rationale: these docs are a source of truth consumed by both people and AI agents. An inaccurate
page is worse than a missing one because it is trusted and propagated.

### II. Content Is Structured Data

Documentation content is not free-form; it is convention-bound files that the services and MCP
discover by path, name, and number. New content MUST follow the existing conventions exactly:

- Menu sections: `src/WebDocs/wwwroot/app/menu/NNNN - <Section>/NNNN - <title>.html`, where the
  `NNNN` numeric prefixes drive ordering. Keep numbering consistent within a section.
- Functions: `src/WebDocs/wwwroot/app/functions/<Name>/` containing `description.html`,
  `parameters.json`, and `samples/NNN/{description.html, content.html}` (samples numbered upward).
- Components: `src/WebDocs/wwwroot/components/<name>/` (rendered as tag `d-<name>`).

Rationale: `FunctionService`, `AttributeService`, `MenuController`, and the MCP parse these by
convention. Off-convention files are invisible to the serving layer.

### III. Every Example Is Valid, Runnable Drapo

Code samples teach by example, so they MUST be correct:

- Wrap example markup in the documented sample elements (e.g. `<d-code>`, the `code`/`sample`
  components).
- Every sample MUST pass `validate_drapo` (no unknown `d-*` attributes/functions, correct arity,
  well-formed `d-for`/mustaches).
- Symbols referenced in an example — `d-dataKey`s, components, sectors — MUST resolve in this app
  (use the `drapo-resolver` skill before trusting a name).

Rationale: readers copy examples verbatim; a broken example teaches broken Drapo.

### IV. Docs ↔ Tooling Parity

Content and the serving layer evolve together. The MCP output is derived from content, so drift is
silent and dangerous.

- Changing a content shape (e.g. the `parameters.json` schema, a new sample layout) REQUIRES
  updating the matching Service, Model (`*VM`), Controller, and MCP exposure in the same change.
- Adding a new content category REQUIRES the service/discovery code that makes it queryable.
- A PR that changes only one side without the other is incomplete.

Rationale: AI agents rely on the MCP being a faithful projection of the content; a mismatch breaks
tooling without any visible error.

### V. Simplicity & Dogfooding

The documentation site is itself a reference Drapo application and a minimal ASP.NET Core (.NET 8)
stack. It MUST stay that way.

- Prefer Drapo-native solutions (attributes, components, sectors) over hand-written JavaScript.
- Add dependencies (NuGet or npm) only with a clear, stated justification; default to none.
- Keep the build the documented one: `dotnet` for the app, Less/Cake for CSS, Docker optional.

Rationale: the site is the first thing a Drapo learner inspects. It should model good Drapo, not
contradict it.

## Technology & Structure Constraints

- **Runtime/stack**: ASP.NET Core on .NET 8 (`src/WebDocs`), Drapo frontend served from
  `wwwroot/`, Less → CSS via Cake, optional Docker. The Drapo engine is consumed from the
  `Sysphera.Middleware.Drapo` package and served at `/drapo.js`.
- **Serving layer**: `Controllers/` (Attribute, Function, Menu, Sample, Search, Chat, NuGet, Todo),
  `Services/` (FunctionService, AttributeService, ConceptService, DataTypeService,
  DrapoEngineCatalog, DrapoValidatorService, NuGetService) and their interfaces, `Models/` ViewModels
  (`*VM`).
- **AI surface**: an MCP server exposing functions, attributes, concepts, data types, and Drapo
  validation; plus the `drapo-resolver` plugin/skill for app-specific symbol resolution.
- **Authoritative reference**: the engine catalog and `validate_drapo`, never memory or assumption.

## Development Workflow & Quality Gates

- **Scope changes narrowly**: documentation PRs are organized by a coherent group (a family of
  attributes, one function, one section), mirroring the existing commit history.
- **Validate before commit**: examples pass `validate_drapo`; documented symbols exist in the
  engine catalog / MCP; new content follows the naming and folder conventions (Principle II).
- **Build before claiming done**: `dotnet build` (and the app runs) for any change touching the
  serving layer; rendered pages checked for content changes.
- **Keep parity**: any content-shape change ships with its serving-layer/MCP counterpart
  (Principle IV).

## Governance

This constitution supersedes ad-hoc practice for this repository. All changes — to content, to the
serving layer, and to AI tooling — are expected to comply.

- **Compliance**: reviewers verify that PRs uphold the principles above, especially Accuracy
  (I) and Docs ↔ Tooling Parity (IV). Complexity or new dependencies must be justified against
  Principle V.
- **Amendments**: changes to this document require a PR describing the rationale and the impact on
  dependent templates (`.specify/templates/*`) and runtime guidance (`CLAUDE.md`,
  `.github/copilot-instructions.md`), which MUST be updated in the same change.
- **Versioning**: semantic versioning of this constitution —
  MAJOR for backward-incompatible principle removals/redefinitions, MINOR for a new principle or
  materially expanded section, PATCH for clarifications and wording.
- **Runtime guidance**: day-to-day agent guidance lives in `CLAUDE.md` and
  `.github/copilot-instructions.md`; for active features, follow the current plan under `specs/`.

**Version**: 1.0.0 | **Ratified**: 2026-06-30 | **Last Amended**: 2026-06-30
