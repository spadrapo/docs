# Contract: Documented query aggregate set & semantics

This is the "contract" the documentation must present — the user-facing surface of the `query`
aggregate functions. It MUST match the bundled engine after the package bump to `Drapo 2026.6.30.2`.

## Supported aggregate functions (the enumerated set)

The docs MUST enumerate exactly these five, identically across the guide page, the `query`
data-type page, and the MCP `get_data_type_details("query")` text:

- `COUNT(column)`
- `MAX(column)`
- `MIN(column)`
- `SUM(column)`  *(added by engine #659; requires engine ≥ the bumped version)*
- `AVG(column)`  *(added by engine #659; requires engine ≥ the bumped version)*

## Result-shape contract

- A query computes **exactly one** aggregate function (the engine resolves only the first
  projection — see research R4). The result is a **single object** with that one alias, accessed
  `{{key.Alias}}`.
- To show several totals from one source, use **one query storage item per aggregate**.
- A non-aggregate projection → result is an **array** of row objects, iterated with `d-for`.

## Numeric-semantics contract (SUM / AVG)

- `SUM(column)`: numeric total of the column's values; `null`/non-numeric values skipped; returns
  `null` if there are no numeric values.
- `AVG(column)`: sum ÷ count of the numeric values; returns `null` if there are none.

## Reactivity contract

- An aggregate query re-evaluates whenever a source storage item in its `FROM` changes (live totals).

## Version contract (gotcha callout)

- Before #659, `Sum(...)`/`Avg(...)` parsed/validated but returned an empty result (fell through to a
  normal projection). The docs MUST name the first engine version that supports SUM/AVG (the bumped
  `Drapo` version, `2026.6.30.2`) so readers on older builds understand the silent-empty behavior.

## Example contract (one aggregate per query)

A worked example MUST produce single-object results and bind aliases, using a separate query item
per aggregate (the engine evaluates only one aggregate per query — research R4):

```html
<div d-dataKey="orders" d-dataType="array"
     d-dataValue='[{"Id":1,"Amount":10},{"Id":2,"Amount":20},{"Id":3,"Amount":30}]'></div>

<div d-dataKey="orderTotal"   d-dataType="query" d-dataValue="SELECT Sum(Amount) AS Total   FROM orders"></div>
<div d-dataKey="orderAverage" d-dataType="query" d-dataValue="SELECT Avg(Amount) AS Average FROM orders"></div>
<div d-dataKey="orderItems"   d-dataType="query" d-dataValue="SELECT Count(Id)   AS Items   FROM orders"></div>

<span>Total: {{orderTotal.Total}}</span>
<span>Average: {{orderAverage.Average}}</span>
<span>Items: {{orderItems.Items}}</span>
```

Rendered live (engine `2026.6.30.2`): Total 60, Average 20, Items 3.

> **Not supported:** combining multiple aggregates in one `SELECT` (only the first resolves), and
> aggregating a positional/nested column (`Sum(Cells.[N].Value)` — the engine's parameter-name
> resolver replaces only the first dot, so the column never matches and the result is empty). Do
> **not** document these.

Every example added MUST pass `validate_drapo`, resolve its symbols, **and render non-empty live**
(constitution Principle III). `validate_drapo` is parse-level only and does not catch the
single-aggregate or positional limitations — live rendering is required.
