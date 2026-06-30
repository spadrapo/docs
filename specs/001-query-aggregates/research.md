# Phase 0 Research: query aggregate functions

## R1 — Does the bundled engine actually evaluate SUM/AVG? (was the spec's key unknown)

**Decision**: The bundled engine must be upgraded; document against `Drapo 2026.6.30.2`.

**Findings**:
- The docs app pins `Drapo` `2025.12.9.6` (`src/WebDocs/WebDocs.csproj`).
- The engine's TypeScript surface for that version
  (`node_modules/@types/drapo/index.d.ts`) exposes:
  `ResolveQueryAggregations`, `ResolveQueryAggregationsMax`, `ResolveQueryAggregationsMin` —
  **no `…Sum`, no `…Avg`.** So engine PR spadrapo/drapo#659 is **not** in the served engine.
- `validate_drapo` reports a `SELECT Sum(...) AS Total …` snippet as **valid** (engine
  `1.0.0+e0181c97…`). This is parse-level only — it confirms the *grammar* accepts `Sum(...)`, not
  that the engine evaluates it. This matches the issue's gotcha: pre-#659, `Sum`/`Avg` parsed fine
  but returned an empty result.
- The latest package `2026.6.30.2` (verified by downloading the nupkg and inspecting its
  `@types/drapo/index.d.ts`) **does** expose `ResolveQueryAggregationsSum` and
  `ResolveQueryAggregationsAvg` alongside `…Max`/`…Min`. So #659 has shipped there.

**Rationale**: Constitution Principle I (Accuracy Over the Real Engine, NON-NEGOTIABLE) forbids
documenting a function the bundled engine does not dispatch. Documenting SUM/AVG therefore requires
bumping the engine to a version that contains #659.

**Decision (user-confirmed)**: Bump `Drapo` `2025.12.9.6` → `2026.6.30.2` (latest) in this feature,
then document the full set.

**Alternatives considered**:
- *Bump to the first version that added Sum/Avg*: less unrelated drift, but requires probing many
  intermediate releases to pin the exact version; user chose latest for simplicity/currency.
- *Defer SUM/AVG; document MAX/MIN only now*: accurate today with no bump, but leaves the issue
  half-done.
- *Docs only, assume bump happens elsewhere*: risks merging docs that don't match the served engine
  — violates Principle I. Rejected.

## R2 — Current documentation surfaces and gaps

**Decision**: Two pages to edit; MCP text follows automatically.

**Findings**:
- `wwwroot/app/menu/0001 - Guide/0016 - Data Querying.html` — has an `<h3>Aggregation: COUNT</h3>`
  section showing only `Count(...)` and the single-object access pattern. Uses `<pre><code>` for
  examples (escaped HTML), not `<d-sample>`.
- `wwwroot/app/menu/0002 - Data/0002 - Types/0010 - query.html` — intro says "…and aggregate
  functions"; has one live `<d-sample>` titled "Count" using `Count(O1.Key) AS Count`. Uses live
  `<d-sample>` blocks throughout.
- The MCP `get_data_type_details("query")` text is derived from the `query` page content, so editing
  the page updates the MCP enumeration automatically — no separate Service/VM/Controller edit needed
  (Docs ↔ Tooling Parity satisfied without a code change).

**Rationale**: Keeps the change within the existing convention-bound content files (Principle II) and
respects each page's existing example style.

## R3 — SUM/AVG semantics to document

**Decision**: Document the semantics stated in the issue, then confirm by running the upgraded engine.

**Findings (from issue spadrapo/drapo#362 / PR #659)**:
- `SUM(column)` → numeric total of the column's values; `null`/non-numeric values are skipped;
  returns `null` if there are no numeric values.
- `AVG(column)` → sum ÷ count of the numeric values; returns `null` if none.
- Single-object result rule (already true for COUNT): when **all** selected columns are aggregates,
  the result is a single object accessed as `{{key.Alias}}` (not an array).
- Aggregate queries re-evaluate when the source storage changes (reactive live totals).
- Positional/nested sources work: `Sum(Cells.[N].Value)` uses the same column reference as
  `d-for`/`d-model`.

**Rationale**: These are the behaviors the issue asks us to document. The quickstart records the
manual run that confirms real (non-empty) output on the bumped engine, closing the gap that
`validate_drapo` alone cannot (R1).

**Open verification (handled in quickstart, not blocking the plan)**: exact `null`-vs-empty rendering
and `N2` formatting are confirmed by running the page after the bump.

## R4 — Runtime behavior corrections (found during implementation by live render)

**Decision**: Document **one aggregate per query**; do **not** document multi-aggregate SELECTs or
positional/nested aggregates. The issue's suggested examples for both are inaccurate to the engine.

**Findings (engine `2026.6.30.2`, confirmed by reading `drapo.js` and rendering live)**:
- `ResolveQueryAggregations` inspects **only `query.Projections[0]`** (the first projection). A
  `SELECT Sum(...) AS Total, Avg(...) AS Average, ... FROM orders` resolves only `Sum` → `{Total:60}`;
  the other aliases are absent (live render showed `Total: 60` and empty Average/Lowest/Highest/Items
  with console `Invalid DataKey` lookups). → The engine supports exactly **one aggregate per query**.
- `ResolveQueryFunctionParameterName` does `value.replace(".","_")` — JS `String.replace` with a
  string replaces only the **first** dot. So `Cells.[2].Value` → `Cells_[2].Value`, which never
  matches the flattened column key; the positional/nested aggregate returns empty (live render
  confirmed empty `Total`). → Positional/nested aggregates are **not** usable here.
- Single-aggregate queries render correctly: separate `Sum`/`Avg`/`Min`/`Max`/`Count` query items
  over the same source produced Total 60, Average 20, Lowest 10, Highest 30, Items 3.

**Impact**: FR-006 (combined SUM+AVG+COUNT example) and FR-011 (positional source) as originally
written contradict the engine; corrected in spec.md. The single-object rule is reworded from "when
all selected columns are aggregates" to "an aggregate query computes one aggregate → single object".

**Why `validate_drapo` didn't catch it**: it validates parse-level legality only; both inaccurate
forms parse cleanly. Live rendering on the bumped engine is the authoritative check (constitution
Principle I/III).
