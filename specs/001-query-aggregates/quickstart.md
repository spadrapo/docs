# Quickstart: validate the query aggregate docs

A run/validation guide proving the feature works end-to-end. Details live in
[contracts/aggregates.md](./contracts/aggregates.md) and [data-model.md](./data-model.md).

## Prerequisites

- .NET 8 SDK
- Branch `362-query-aggregate-functions` checked out
- Engine package bumped: `src/WebDocs/WebDocs.csproj` references `Drapo` `2026.6.30.2`

## Step 1 — Confirm the engine bump

```bash
cd src/WebDocs
dotnet restore
dotnet build
```

Expected: build succeeds on `Drapo 2026.6.30.2`. (Sanity-check `@types/drapo` exposes
`ResolveQueryAggregationsSum` / `…Avg`.)

## Step 2 — Validate every changed/added sample (parse-level)

Run each new/edited aggregate snippet through `validate_drapo` (MCP). Expected: `valid: true`,
0 errors for all of them.

## Step 3 — Run the site and confirm SUM/AVG produce real values

```bash
dotnet run    # https://localhost:5001
```

- Navigate to **Data → Types → query**. The aggregate sample(s) must show **non-empty** totals/
  averages (the key fix vs. the pre-#659 silent-empty behavior).
- Navigate to the **Guide → Data Querying** page. The Aggregation section lists `COUNT`, `MAX`,
  `MIN`, `SUM`, `AVG`, shows the single-object access pattern, and the version gotcha callout.

**Expected outcome** (maps to spec Success Criteria):

| Check | Success Criterion |
|-------|-------------------|
| Both pages list the same five aggregates | SC-001, SC-006 |
| All aggregate samples pass validation & resolve | SC-002 |
| All-aggregate query renders a single object (`{{stats.Total}}`) | SC-003 |
| SUM/AVG `null`/non-numeric & empty behavior documented | SC-004 |
| Gotcha names first supporting engine version | SC-005 |

## Step 4 — Confirm MCP parity

Call MCP `get_data_type_details("query")` after the page edit; its aggregate text must reflect the
five-function set (it is derived from the page content — no separate code change).

## Step 5 — Smoke-check existing samples after the bump

Because the bump spans many engine releases, spot-check a few unrelated existing samples on the
`query` page and one other page still render correctly (no regression from the engine upgrade).
