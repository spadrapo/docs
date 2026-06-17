# Using Drapo correctly

Practical rules for writing Drapo that works. This is the *prescriptive* layer; for the full
reference of every attribute/function/concept, query the Drapo MCP (`get_attributes`,
`get_functions`, `get_concepts`, `get_*_details`). To confirm a symbol exists in *this* app,
use the resolver (`resolve-datakey`/`resolve-component`/`resolve-sector`).

## Storage & loading data

- Declare every piece of state with `d-dataKey` and a `d-dataType` (`value`, `object`, `array`, `function`, `querystring`, `parent`, `mapping`, `pointer`, `query`, `switch`).
- Initialize inline: `value` → `d-dataProperty="..."`; `object` → `d-dataProperty-<name>="..."`.
- Load from the server with `d-dataUrlGet`; post changes back with `d-dataUrlSet` (use `PostData(key)`). Control *when* it loads with `d-dataLoadType`.
- Storage declared inside a sector dies when the sector is cleared/replaced. For state that must survive navigation, declare it **outside any sector** (e.g. the root layout).
- Manipulate storage with functions, not ad-hoc JS: `AddDataItem`, `RemoveDataItem`, `UpdateDataField`, `ClearDataField`, `PostData`, `ReloadData`.
- **Do** name the key once and reuse it; **don't** bind a key you never declared (the #1 cause of "nothing shows up").

## Data binding

- One-way (read): `{{key.property}}` in any text node *or* attribute value (`src`, `href`, `class`, …). It re-evaluates whenever the storage changes.
- Two-way: `d-model="{{key.property}}"` on a form control. For non-interactive elements `d-model` is one-way.
- Expressions support nested paths (`{{order.address.city}}`), array length (`{{items.length}}`), and simple arithmetic/comparison inside attributes like `d-if`, `d-class`.
- Bindings resolve in the **current sector's** storage context. A key from another sector won't resolve unless it's a parent/global key or shared via `d-sector-friend`.

## Rendering (d-for / d-if)

- Loop: `d-for="alias in dataKey"` — `dataKey` must be an **array** (or an object, which iterates as `{{prop.Key}}`/`{{prop.Value}}`). Inside, bind with the alias: `{{alias.field}}`.
- Filter rows with `d-if="{{...}}"` on the `d-for` element (false → removed from DOM). Disable the whole loop with `d-for-if`.
- Large lists: set `d-for-render="item"` to patch individual rows instead of re-rendering the whole list.
- Ranges: `items[0..5]`, `items[1..^1]`, `items[^0..0]` (`^` indexes from the end).
- `d-if` (conditional render) is the everyday tool; reach for `d-render` / `d-for-render` only for render-control.

## Sectors & navigation

- A `d-sector="name"` element is a region you can load/clear/replace independently.
- Navigate by loading content into a sector: `UpdateSector(sectorName, ~/path/view.html)`. Clear with `ClearSector(sectorName)`.
- Lifecycle on `UpdateSector`: fetch HTML → destroy the old content's storage/bindings → replace inner HTML → process Drapo attributes in the new markup (initializing nested sectors).
- Convention-based layout: a child view can declare `d-parent`/`d-parentSector` to load itself into a parent layout's sector without an explicit `UpdateSector` call.
- The first argument to `UpdateSector` must be a sector that actually exists in the current DOM — verify with `resolve-sector`.

## Components

- A component is a folder `wwwroot/components/<name>/` with `<name>.html` (+ optional `<name>.ts`, `.css`). Use it as `<d-<name>>`.
- Pass data/config into a component instance with `dc-*` attributes (e.g. `dc-userName="{{user.name}}"`).
- A `<d-foo>` tag with no matching component folder renders nothing — catch these with `scan-unresolved` / `resolve-component`.

## Events

- Attach behavior with `d-on-<event>` (e.g. `d-on-click`, `d-on-change`). The value is a sequence of Drapo function calls separated by `;` — no JavaScript.
- `d-on-click="FuncA(...);FuncB(...)"` runs them in order. Debounce with `d-on-<event>-debounce`; prevent overlapping runs with `d-event-single`.
- Every function name must be a real Drapo function — confirm with `validate_drapo` / `get_functions`. Watch argument counts and balanced `{{ }}`.

## Validation, formatting, live data

- **Validation**: declare a validator once as a hidden element, then reference it from interactive elements (to block) and display elements (to show messages). Run/clear with `ExecuteValidation` / `ClearValidation`.
- **Formatting**: format numbers/dates/timespans in-template with `d-format`, and `d-culture` for locale-aware output. Don't hand-format in expressions.
- **Live data**: use **Polling** (`d-dataPollingKey` + interval, calls `ReloadData` on change) for interval refresh, or **Pipes** (SignalR push) for server-pushed updates.

## General do / don't

- ✅ Confirm a key/component/sector exists (`resolve-*`) before binding to it.
- ✅ Validate snippet syntax with `validate_drapo` before finishing an edit.
- ✅ Use Drapo functions for state changes so change-tracking notifies the UI.
- ❌ Don't write custom JavaScript to mutate the DOM that Drapo manages.
- ❌ Don't bind across sectors without a parent/global/friend relationship.
- ❌ Don't assume a binding works — verify it rendered (see `troubleshooting.md`).
