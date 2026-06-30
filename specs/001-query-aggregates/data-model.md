# Phase 1 Data Model: query aggregate functions

This is a documentation feature; the "data model" is the conceptual model the docs must convey
accurately, not a new persisted schema. No `*VM` / Service / Controller schema changes are required
(the `query` data type is already modeled and exposed via `DataTypeService` / MCP).

## Entity: Aggregate function

A query function that collapses a column of values into a single value.

| Field | Value |
|-------|-------|
| Members | `COUNT`, `MAX`, `MIN`, `SUM`, `AVG` |
| Engine dispatch (post-bump `2026.6.30.2`) | `ResolveQueryAggregations` (COUNT), `…Max`, `…Min`, `…Sum`, `…Avg` |
| Syntax | `Func(column) AS Alias` inside `SELECT` (case-insensitive: `Sum`/`SUM`) |
| Numeric members | `SUM`, `AVG` — skip `null`/non-numeric inputs |

**Validation rules**:
- `SUM(column)`: numeric total; `null`/non-numeric skipped; `null` when no numeric values.
- `AVG(column)`: sum ÷ count of numeric values; `null` when none.
- `COUNT`/`MAX`/`MIN`: pre-existing engine behavior (already supported in `2025.12.9.6`).

## Entity: Aggregate query result

The shape produced by a `query` storage item that uses aggregates.

| Condition | Result shape | Access |
|-----------|--------------|--------|
| Aggregate query (one aggregate function — engine resolves only `Projections[0]`, research R4) | single object with one alias | `{{key.Alias}}` |
| Non-aggregate projection | array of row objects | `d-for` over `{{key}}` |

> One aggregate per query: to show several totals from one source, use one query storage item per
> aggregate. Positional/nested aggregates (`Sum(Cells.[N].Value)`) are unsupported (research R4).

**State transitions / reactivity**: When a source storage item referenced in `FROM` changes, the
aggregate query re-evaluates and re-renders bound elements (live totals).

## Entity: Documentation surfaces (kept consistent)

| Surface | File | Change |
|---------|------|--------|
| "Data Querying" guide | `wwwroot/app/menu/0001 - Guide/0016 - Data Querying.html` | Aggregation section: COUNT-only → full set + SUM/AVG semantics + single-object rule + gotcha callout |
| `query` data-type page | `wwwroot/app/menu/0002 - Data/0002 - Types/0010 - query.html` | aggregate-functions mention → explicit five-function set; add live SUM/AVG (and MAX/MIN) `<d-sample>`(s) |
| MCP enumeration | derived from `query` page via `DataTypeService` | updates automatically with the page text |
| Engine reference | `src/WebDocs/WebDocs.csproj` | `Drapo` `2025.12.9.6` → `2026.6.30.2` |
