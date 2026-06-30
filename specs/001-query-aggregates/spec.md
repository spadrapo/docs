# Feature Specification: Document the full query aggregate-function set (COUNT, MAX, MIN, SUM, AVG)

**Feature Branch**: `362-query-aggregate-functions`

**Created**: 2026-06-30

**Status**: Draft

**Input**: User description: "lets work on issue #362 — Docs: document SUM and AVG aggregate functions in query data type"

## Overview

The Drapo `query` data type supports aggregate functions, but the documentation only shows
`COUNT`. The engine also supports `MAX` and `MIN`, and `SUM`/`AVG` are added by engine PR
spadrapo/drapo#659. This feature brings the documentation in line with the engine by documenting the
**full, accurate aggregate set** (`COUNT`, `MAX`, `MIN`, `SUM`, `AVG`), the single-object result
rule when every selected column is an aggregate, `SUM`/`AVG` numeric semantics, and a version
caveat so readers on older builds are not surprised by silently-empty results.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Discover the complete set of aggregate functions (Priority: P1)

A developer building a reactive total/summary (e.g. an order total, an average price) reads the
"Data Querying" guide and the `query` data-type page to learn which aggregate functions Drapo
supports. Today they only see `COUNT`, so they wrongly assume `SUM`/`AVG`/`MIN`/`MAX` are
unavailable, or they try them and get a silently-empty result with no explanation.

**Why this priority**: This is the core of the issue. Without an accurate, complete list, readers
cannot use aggregates they actually have, and the docs misrepresent the engine — a direct violation
of the project's accuracy principle.

**Independent Test**: Open the "Data Querying" guide and the `query` type page; confirm all five
aggregates (`COUNT`, `MAX`, `MIN`, `SUM`, `AVG`) are listed and described. Delivers value on its own
even without new runnable samples.

**Acceptance Scenarios**:

1. **Given** the "Data Querying" guide, **When** a reader reaches the Aggregation section, **Then**
   it lists `COUNT`, `MAX`, `MIN`, `SUM`, and `AVG` (not just `COUNT`) and is no longer titled
   "Aggregation: COUNT".
2. **Given** the `query` data-type page, **When** a reader looks for aggregate functions, **Then**
   the supported set is stated explicitly as `COUNT`, `MAX`, `MIN`, `SUM`, `AVG`.
3. **Given** any MCP/reference text that enumerates supported aggregates, **When** it is read,
   **Then** it matches the same five-function set (docs ↔ tooling parity).

### User Story 2 - Understand SUM/AVG semantics and the single-object result rule (Priority: P2)

A developer wants to compute a numeric total and average over a column and bind the result. They
need to know how the result is shaped (single object vs. array) and how non-numeric/null values and
empty inputs are handled.

**Why this priority**: Correct semantics prevent subtle bugs (e.g. expecting an array, or
mis-handling `null`). Builds on the list from Story 1.

**Independent Test**: Read the documented semantics and confirm a sample that selects only aggregate
columns is described as producing a single object accessed via `{{key.Alias}}`.

**Acceptance Scenarios**:

1. **Given** an aggregate query (one aggregate function over one column), **When** the docs
   describe its result, **Then** they state the result is a **single object** (not an array),
   accessed as `{{key.Alias}}` — applying to all aggregates, not only `COUNT` — and that each query
   evaluates exactly one aggregate (use a separate query item per total). *(Corrected per research
   R4: the engine resolves only the first aggregate projection.)*
2. **Given** the `SUM(column)` description, **When** a reader reviews it, **Then** it states: numeric
   total of the column's values; `null`/non-numeric values are skipped; returns `null` if there are
   no numeric values.
3. **Given** the `AVG(column)` description, **When** a reader reviews it, **Then** it states: sum ÷
   count of the numeric values; returns `null` if there are none.
4. **Given** the Reactivity behavior, **When** the source storage changes, **Then** the docs note
   aggregate queries re-evaluate (good for live totals).

### User Story 3 - Avoid the silent-empty-result trap on older engine builds (Priority: P3)

A developer on an engine build older than the one that ships #659 tries `SUM`/`AVG`, gets an empty
result, and needs the docs to explain why.

**Why this priority**: Prevents support churn and confusion, but only affects the subset of readers
on older builds; lower impact than the core documentation.

**Independent Test**: Read the gotcha/version callout and confirm it names the first engine version
that supports `SUM`/`AVG` and explains the prior silent-empty behavior.

**Acceptance Scenarios**:

1. **Given** the aggregate documentation, **When** a reader is on a pre-#659 build, **Then** a
   callout explains that `SUM(...)`/`AVG(...)` previously parsed/validated fine but returned an empty
   result (fell through to a normal projection), and names the first supporting engine version.

### Edge Cases

- A query with more than one aggregate in the `SELECT` — only the first resolves (research R4);
  examples must use one aggregate per query.
- `SUM`/`AVG` over a column containing `null` or non-numeric values (skipped).
- `SUM`/`AVG` over zero numeric values (returns `null`).
- Positional/nested-column aggregates (`Cells.[N].Value`) return empty on this engine (research R4)
  — must not be documented as working.
- Every documented example must be valid Drapo, resolve its symbols, **and render non-empty live**
  (constitution requirement; `validate_drapo` alone is insufficient).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The "Data Querying" guide Aggregation section MUST list all five supported aggregates
  (`COUNT`, `MAX`, `MIN`, `SUM`, `AVG`) and MUST no longer present aggregation as COUNT-only.
- **FR-002**: The `query` data-type page MUST state the supported aggregate set explicitly as
  `COUNT`, `MAX`, `MIN`, `SUM`, `AVG` wherever aggregate functions are mentioned.
- **FR-003**: The documentation MUST state the single-object result rule: an aggregate query computes
  one aggregate and the result is a single object accessed as `{{key.Alias}}`, applying to all
  aggregates. It MUST also state that each query evaluates exactly **one** aggregate (use one query
  storage item per total). *(Corrected per research R4.)*
- **FR-004**: The documentation MUST describe `SUM(column)` semantics: numeric total; `null`/
  non-numeric skipped; `null` when no numeric values.
- **FR-005**: The documentation MUST describe `AVG(column)` semantics: sum ÷ count of numeric values;
  `null` when none.
- **FR-006**: The documentation MUST include worked examples using `SUM`, `AVG`, `MIN`, `MAX`, and
  `COUNT` over a shared source, each as its own query storage item, binding `{{key.Alias}}` values.
  *(Corrected per research R4: a single combined multi-aggregate SELECT is NOT supported — only the
  first aggregate resolves — so the example uses one query per aggregate.)*
- **FR-007**: The documentation MUST note (or reinforce) that aggregate queries re-evaluate when the
  source storage changes, supporting live totals.
- **FR-008**: The documentation MUST include a version/gotcha callout explaining the pre-#659
  silent-empty behavior for `SUM`/`AVG` and naming the first engine version that supports them.
- **FR-009**: Any MCP/reference text that enumerates supported aggregates MUST be updated to the same
  five-function set, preserving docs ↔ tooling parity.
- **FR-010**: Every example added or changed MUST be valid Drapo (passes `validate_drapo`) and every
  `d-dataKey`/component/sector it names MUST resolve.
- **FR-011**: ~~Documentation MUST cover aggregating a positional/nested source (e.g.
  `Sum(Cells.[N].Value)`).~~ **Dropped (research R4):** the engine's parameter-name resolver replaces
  only the first dot, so positional/nested aggregates return empty; documenting them would violate
  the accuracy principle. Documentation MUST NOT claim positional/nested aggregates work.

### Key Entities *(include if feature involves data)*

- **Aggregate function**: A query function that collapses a column of values into a single value.
  Set: `COUNT`, `MAX`, `MIN`, `SUM`, `AVG`. `SUM`/`AVG` are numeric and skip non-numeric/null inputs.
- **Aggregate query result**: When all selected columns are aggregates, a single object keyed by the
  column aliases; otherwise an array of row objects.
- **Documentation surfaces**: the "Data Querying" guide page, the `query` data-type page, and any
  MCP/reference enumeration of aggregates — all kept consistent.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A reader can name all five supported aggregate functions after reading either the guide
  or the data-type page (both list the same five).
- **SC-002**: 100% of aggregate-related examples in the changed pages pass Drapo validation and have
  resolvable symbols.
- **SC-003**: A reader can correctly predict the result shape (single object vs. array) of an
  all-aggregate query from the documentation alone.
- **SC-004**: A reader can state how `SUM`/`AVG` treat `null`/non-numeric values and an empty input
  (returns `null`) from the documentation alone.
- **SC-005**: A reader on an older engine build can explain, from the docs, why `SUM`/`AVG` returned
  an empty result and which version fixes it.
- **SC-006**: The set of aggregates listed is identical across the guide, the data-type page, and any
  MCP/reference enumeration (no drift).

## Assumptions

- **Engine precondition (dependency)**: This work proceeds only once engine PR spadrapo/drapo#659 has
  merged and shipped to the bundled engine (`Sysphera.Middleware.Drapo` / served `drapo.js`) so the
  documented `SUM`/`AVG` behavior matches a released build. The plan phase MUST confirm the bundled
  engine actually evaluates `SUM`/`AVG` (not just parses them) before publishing the new examples;
  `validate_drapo` only checks parse-level legality, so it cannot by itself confirm #659 shipped.
- The first-supporting engine version string used in the gotcha callout will be taken from the
  shipped build/NuGet bump that includes #659; it is not yet pinned in this spec.
- `MAX`/`MIN` are already engine-supported and documenting them is in scope (the issue lists the full
  set) even though #659 only adds `SUM`/`AVG`.
- The existing COUNT sample and other query samples remain; this feature extends rather than rewrites
  the pages.
- Numeric formatting in examples (e.g. `d-format='N2'`) is illustrative and not a behavior of the
  aggregate functions themselves.
