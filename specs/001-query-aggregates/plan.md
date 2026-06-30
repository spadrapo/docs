# Implementation Plan: Document the full query aggregate-function set (COUNT, MAX, MIN, SUM, AVG)

**Branch**: `362-query-aggregate-functions` | **Date**: 2026-06-30 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/001-query-aggregates/spec.md`

## Summary

Bring the `query` data-type documentation in line with the engine by documenting the full aggregate
set (`COUNT`, `MAX`, `MIN`, `SUM`, `AVG`), the single-object result rule, `SUM`/`AVG` numeric
semantics, reactivity, and a version caveat. Because the engine bundled with this app
(`Drapo 2025.12.9.6`) lacks `SUM`/`AVG`, the feature **first bumps the Drapo package reference to
`2026.6.30.2`** (which contains engine PR spadrapo/drapo#659), then updates the two affected doc
pages so every documented function is actually dispatched by the served engine.

## Technical Context

**Language/Version**: C# / .NET 8 (ASP.NET Core) serving layer; Drapo `d-*` HTML for content.

**Primary Dependencies**: `Drapo` NuGet package (`Sysphera.Middleware.Drapo`) — **bumped
`2025.12.9.6` → `2026.6.30.2`** as part of this feature; served at `/drapo.js`.

**Storage**: Static content files under `src/WebDocs/wwwroot/app/` (HTML). No database.

**Testing**: `validate_drapo` (parse-level legality of samples) + manual/visual run of the affected
pages (`dotnet run`) to confirm SUM/AVG evaluate to real values, not silent-empty. `dotnet build`
for the serving layer after the package bump.

**Target Platform**: Web (the docs site) + the MCP server surface.

**Project Type**: Documentation content within an ASP.NET Core web app (dogfooding Drapo).

**Performance Goals**: N/A (documentation change).

**Constraints**: Every example MUST pass `validate_drapo` and resolve its symbols; documented
functions MUST be dispatched by the bundled engine (constitution Principle I).

**Scale/Scope**: Two content pages + one package reference + verification. No service/Model/Controller
schema change (the `query` type and aggregates are already modeled; only content changes).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Accuracy Over the Real Engine (NON-NEGOTIABLE) | ✅ via bump | Current `2025.12.9.6` lacks `Sum`/`Avg`; **resolved by bumping to `2026.6.30.2`** whose `@types/drapo` confirms `ResolveQueryAggregationsSum`/`…Avg`/`…Max`/`…Min`. After the bump, all five documented functions are dispatched. Without the bump this gate FAILS. |
| II. Content Is Structured Data | ✅ | Edits stay within the existing convention-bound files (`menu/0001 - Guide/0016 - Data Querying.html`, `menu/0002 - Data/0002 - Types/0010 - query.html`). No new content category, no numbering changes. |
| III. Every Example Is Valid, Runnable Drapo | ✅ planned | New samples wrapped in `<d-sample>`/`<pre><code>` per page convention; each passes `validate_drapo` and is run live to confirm real (non-empty) SUM/AVG output. |
| IV. Docs ↔ Tooling Parity | ✅ | The `query` data type and aggregate behavior are already exposed via `DataTypeService` / MCP `get_data_type_details`; that output is derived from the page content, so updating the page updates the MCP text. No schema/Service/VM change required. The MCP enumeration of aggregates updates automatically with the page text. |
| V. Simplicity & Dogfooding | ✅ | No new JS, no new dependency added (only a version bump of the existing engine dependency, justified by Principle I). Build stays `dotnet` + existing toolchain. |

**Gate result**: PASS, conditional on the package bump landing in this feature (decided: bump to
`2026.6.30.2`).

## Project Structure

### Documentation (this feature)

```text
specs/001-query-aggregates/
├── plan.md              # This file
├── spec.md              # Feature spec
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output (validation/run guide)
├── contracts/
│   └── aggregates.md    # Phase 1 output: the documented aggregate "contract" (set + semantics)
├── checklists/
│   └── requirements.md  # Spec quality checklist (from /speckit-specify)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

Files this feature touches:

```text
src/WebDocs/
├── WebDocs.csproj                                          # PackageReference Drapo 2025.12.9.6 → 2026.6.30.2
└── wwwroot/app/menu/
    ├── 0001 - Guide/0016 - Data Querying.html             # "Aggregation: COUNT" → full set + semantics + gotcha
    └── 0002 - Data/0002 - Types/0010 - query.html         # aggregate-functions mention + new SUM/AVG/MAX/MIN samples
```

**Structure Decision**: Single ASP.NET Core project (`src/WebDocs`). This feature is content-plus-
dependency-bump; it touches two `wwwroot` content pages and the project file, with no new directories
and no serving-layer schema change.

## Complexity Tracking

No constitution violations to justify. The single notable action — bumping the engine dependency —
is **required** by Principle I (Accuracy), not an added complexity; it is the simplest path to making
the SUM/AVG documentation accurate against the served engine.
