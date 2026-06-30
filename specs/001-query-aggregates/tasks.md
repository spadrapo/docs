# Tasks: Document the full query aggregate-function set (COUNT, MAX, MIN, SUM, AVG)

**Feature**: `001-query-aggregates` | **Branch**: `362-query-aggregate-functions`
**Plan**: [plan.md](./plan.md) | **Spec**: [spec.md](./spec.md) | **Contract**: [contracts/aggregates.md](./contracts/aggregates.md)

**Tests**: No automated test suite for docs content. Validation = `validate_drapo` on every sample +
live page run (see [quickstart.md](./quickstart.md)). No TDD test tasks generated.

**Key files**:

- `src/WebDocs/WebDocs.csproj` — engine package reference
- `src/WebDocs/wwwroot/app/menu/0001 - Guide/0016 - Data Querying.html` — guide page (uses `<pre><code>`)
- `src/WebDocs/wwwroot/app/menu/0002 - Data/0002 - Types/0010 - query.html` — data-type page (uses live `<d-sample>`)

---

> **Implementation note (research R4):** live rendering revealed the engine evaluates **one aggregate
> per query** (only `Projections[0]`) and that positional/nested aggregates (`Sum(Cells.[N].Value)`)
> return empty. The originally planned multi-aggregate example (T007/T009) and the positional sample
> (T010) were corrected/dropped to match the engine. See `research.md` R4 and `contracts/aggregates.md`.

## Phase 1: Setup

- [X] T001 Bump the engine reference `Drapo` from `2025.12.9.6` to `2026.6.30.2` in `src/WebDocs/WebDocs.csproj` (the version whose `@types/drapo` exposes `ResolveQueryAggregationsSum`/`…Avg`).
- [X] T002 Run `dotnet restore` then `dotnet build` in `src/WebDocs` and confirm the build succeeds on `Drapo 2026.6.30.2`. (Build succeeded, 0 errors.)

---

## Phase 2: Foundational (blocking prerequisite — gates all user stories)

**Purpose**: Prove the bumped engine actually *evaluates* SUM/AVG (not just parses them) before any
docs are written — constitution Principle I (Accuracy). If this fails, STOP: the docs cannot be
accurate.

- [X] T003 Run the site (`dotnet run` in `src/WebDocs`) and exercise an all-aggregate query (`SELECT Sum(Amount) AS Total, Avg(Amount) AS Average, Count(Id) AS Items FROM orders`); confirm it renders **non-empty** values (not the pre-#659 silent-empty result).
- [X] T004 Smoke-check that existing query samples on `0010 - query.html` (Count, joins, ORDER BY, DISTINCT) still render correctly after the bump — regression guard for the ~6-month engine jump.

**Checkpoint**: Engine confirmed to dispatch all five aggregates and no regression in existing
samples → user-story phases may begin.

---

## Phase 3: User Story 1 — Discover the complete set of aggregate functions (P1) 🎯 MVP

**Goal**: Both the guide and the data-type page enumerate the same five aggregates instead of
COUNT-only.

**Independent test**: Open both pages; confirm `COUNT`, `MAX`, `MIN`, `SUM`, `AVG` are all listed and
the guide section is no longer titled "Aggregation: COUNT". (Spec SC-001, SC-006.)

- [X] T005 [P] [US1] In `src/WebDocs/wwwroot/app/menu/0001 - Guide/0016 - Data Querying.html`, rename the `<h3>Aggregation: COUNT</h3>` section to `<h3>Aggregation</h3>` and update its intro prose to state the supported set is `COUNT`, `MAX`, `MIN`, `SUM`, `AVG`.
- [X] T006 [P] [US1] In `src/WebDocs/wwwroot/app/menu/0002 - Data/0002 - Types/0010 - query.html`, update the intro sentence ("…and aggregate functions") to name the explicit set `COUNT`, `MAX`, `MIN`, `SUM`, `AVG`.

**Checkpoint**: US1 independently shippable — accurate complete list on both surfaces.

---

## Phase 4: User Story 2 — SUM/AVG semantics & single-object result rule (P2)

**Goal**: Readers learn result shape, SUM/AVG numeric semantics, reactivity, with a worked example.

**Independent test**: An all-aggregate query is documented as a single object accessed `{{key.Alias}}`;
SUM/AVG null/non-numeric/empty behavior is stated. (Spec SC-003, SC-004.)

- [X] T007 [US1→builds on T005] [US2] In `0016 - Data Querying.html` Aggregation section, document the single-object result rule (all-aggregate → single object, `{{key.Alias}}`) and add the canonical worked `<pre><code>` example using `Sum(Amount) AS Total, Avg(Amount) AS Average, Count(Id) AS Items` from `contracts/aggregates.md`.
- [X] T008 [US2] In `0016 - Data Querying.html`, document `SUM(column)` (numeric total; null/non-numeric skipped; null when none) and `AVG(column)` (sum ÷ count of numeric values; null when none); reinforce the Reactivity note for live aggregate totals.
- [X] T009 [P] [US2] In `0010 - query.html`, add a live `<d-sample>` (after the existing "Count" sample) demonstrating `Sum`/`Avg` (and `Max`/`Min`) over an inline array storage, binding `{{stats.Total}}` / `{{stats.Average}}` etc.
- [X] T010 [P] [US2] In `0010 - query.html`, add a short callout/sample for a positional/nested source aggregate (`Sum(Cells.[N].Value)`-style) per FR-011, or note it inline if a full sample is impractical.

**Checkpoint**: US2 shippable — semantics + runnable example present.

---

## Phase 5: User Story 3 — Version gotcha for older builds (P3)

**Goal**: Readers on pre-#659 builds understand the silent-empty behavior and the fixing version.

**Independent test**: A callout names the first engine version supporting SUM/AVG and explains the
prior empty-result behavior. (Spec SC-005.)

- [X] T011 [US3] In `0016 - Data Querying.html` Aggregation section, add a version/gotcha callout: before #659 `Sum(...)`/`Avg(...)` parsed/validated but returned an empty result (fell through to a normal projection); SUM/AVG require `Drapo` ≥ `2026.6.30.2`.
- [X] T012 [P] [US3] In `0010 - query.html`, add a brief version-requirement note near the SUM/AVG sample pointing to the same minimum engine version.

**Checkpoint**: US3 shippable — version caveat documented.

---

## Phase 6: Polish & Cross-Cutting

- [X] T013 Run every new/edited aggregate snippet through `validate_drapo` (MCP); confirm `valid: true`, 0 errors for all (Spec SC-002, FR-010).
- [X] T014 Resolve all `d-dataKey`/component/sector names in the new samples via the `drapo-resolver` skill (constitution Principle III).
- [X] T015 Execute `quickstart.md` end-to-end (run site, view both pages, confirm non-empty SUM/AVG, single-object render, gotcha visible) and verify MCP `get_data_type_details("query")` reflects the five-function set (Docs ↔ Tooling Parity).
- [X] T016 Final `dotnet build` green; commit changes scoped to this feature and open the PR from `362-query-aggregate-functions` referencing issue #362.

---

## Dependencies & Execution Order

- **Setup (T001–T002)** → **Foundational (T003–T004)** must complete first; the engine bump and its
  runtime confirmation gate everything (accuracy).
- **US1 (T005–T006)** = MVP. **US2 (T007–T010)** builds on US1's guide edit (T007 follows T005, same
  file). **US3 (T011–T012)** follows US1/US2 edits in the same section.
- **Polish (T013–T016)** runs after all content tasks.
- Same-file edits in `0016 - Data Querying.html` (T005→T007→T008→T011) are **sequential**.
- Same-file edits in `0010 - query.html` (T006→T009→T010→T012) are **sequential**.
- Cross-file tasks are **[P]** parallel: e.g. T005 (guide) ∥ T006 (query page); T009/T010 (query) ∥
  T007/T008 (guide); T011 (guide) ∥ T012 (query).

## Parallel Example

```text
After T004 checkpoint, run the two-file edits in parallel per story:
  Guide track:  T005 → T007 → T008 → T011
  Query track:  T006 → T009 → T010 → T012
  (the two tracks proceed concurrently; join at Polish T013)
```

## Implementation Strategy

- **MVP = US1 (T001–T006)**: bump the engine + make both pages enumerate the accurate five-function
  set. Independently shippable and already corrects the core inaccuracy.
- **Increment 2 = US2**: add semantics + the runnable SUM/AVG sample.
- **Increment 3 = US3**: add the version gotcha.
- Validate (`validate_drapo` + live run) after each increment, not only at the end.
