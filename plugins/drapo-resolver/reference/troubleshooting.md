# Troubleshooting Drapo

A symptom → likely cause → how to confirm → fix playbook. Confirm against the **running app**
before guessing, then fix at the source.

## Inspect the running app first

When a Drapo app is loaded in the browser, the engine is exposed on `window.drapo`:

- `window.drapo.Introspection.GetSnapshot()` → `{ state, sectors, dataKeysBySector, diagnostics }`. The fastest way to see *what is actually mounted*: which sectors exist and which data keys are live in each. (Via Playwright/Selenium: `page.evaluate(() => window.drapo.Introspection.GetSnapshot())`.)
- `window.drapo.Diagnostics.GetErrorBuffer()` → recent client errors/warnings.
- Press **Ctrl+F2** for the built-in visual debugger (highlight sectors/components, reload config).

Pair this with the static tools: the resolver (`resolve-*`, `find-references`, `scan-unresolved`) for *what the source defines*, and the MCP `validate_drapo` for *whether the markup is legal*.

## Symptom → cause → fix

### Text shows literal `{{something}}` instead of a value
- **Cause**: the element was never processed by Drapo — it's inside a `d-pre` block, or it lives in markup that was injected outside Drapo, or its sector never loaded.
- **Confirm**: `GetSnapshot()` — is the expected sector in `sectors`? Is the key in `dataKeysBySector`?
- **Fix**: ensure the markup is loaded into a Drapo sector (`UpdateSector`), and remove stray `d-pre`.

### A binding/list doesn't update when data changes
- **Cause**: the storage was changed in a way that didn't notify observers (e.g. mutated outside Drapo functions, or while `LockData` was in effect).
- **Confirm**: does the value update after a `ReloadData`/`Notify`? Then it's a notification issue.
- **Fix**: change storage via Drapo functions (`UpdateDataField`, `AddDataItem`, …) or call `Notify(key)`; unlock with `UnlockData` if you locked it.

### Data never appears at all
- **Cause** (in order of likelihood): the `d-dataKey` isn't declared; wrong `d-dataType`; the `d-dataUrlGet` request failed; `d-dataLoadType` defers loading.
- **Confirm**: `resolve-datakey <key>` → `found:false` means it's undocumented in source. If declared, check `Diagnostics.GetErrorBuffer()` and the network tab for the `d-dataUrlGet` call.
- **Fix**: declare the key / correct the type / fix the endpoint or load type.

### `d-for` renders nothing (or errors)
- **Cause**: the iterator isn't an array; the data key is wrong/undeclared; `d-for-if` is false; or the data hasn't loaded yet.
- **Confirm**: `resolve-datakey <key>` (does it exist, and is `dataType` `array`?); `GetSnapshot()` to see if it's loaded.
- **Fix**: point `d-for` at an array key that's loaded; verify the syntax is `alias in dataKey`.

### A `<d-component>` renders nothing
- **Cause**: no component folder matches the tag, or the component isn't registered from a scanned path, or the tag is misspelled.
- **Confirm**: `resolve-component <tag>` → `found:false`; or `scan-unresolved` lists it under `unresolvedComponents`.
- **Fix**: correct the tag, or create/register `wwwroot/components/<name>/<name>.html`.

### `UpdateSector(...)` does nothing / wrong region updates
- **Cause**: the target sector name doesn't exist in the current DOM, or the view URL 404s.
- **Confirm**: `resolve-sector <name>` (declared in source?) and `GetSnapshot().sectors` (mounted right now?); check the network request for the view.
- **Fix**: use a sector that exists; fix the `~/path/view.html` URL.

### `d-model` isn't two-way
- **Cause**: the element isn't an interactive control (then `d-model` is one-way), or the change event differs.
- **Fix**: use a real form control; set `d-model-event` if the control fires a non-standard event.

### A click/handler does nothing, or "Invalid Function" appears
- **Cause**: an unknown function name, wrong argument count, or unbalanced `{{ }}` in the `d-on-*` expression.
- **Confirm**: run the handler markup through MCP `validate_drapo`; check the name with `get_functions`.
- **Fix**: correct the function name/arguments; balance mustaches.

### A `d-*` attribute is silently ignored
- **Cause**: typo'd or non-existent attribute (e.g. `d-modl`, `d-datapollingtimepsan`).
- **Confirm**: MCP `validate_drapo` flags unknown attributes; `get_attributes` lists the real ones.
- **Fix**: use the correct attribute name.

## Rule of thumb

Most "Drapo is broken" reports are one of: a **name that doesn't exist** (key/component/sector — use the resolver), **invalid syntax** (use `validate_drapo`), or **a sector/data that isn't loaded at runtime** (use `GetSnapshot()`). Check those three first.
