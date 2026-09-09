# Drapo function arity audit — docs `parameters.json` vs engine `drapo.js`

Engine source: `drapo.js` embedded in Sysphera.Middleware.Drapo 2026.9.9.9 (the `Drapo` NuGet package the solution references), extracted from the assembly resource. Dispatch is an
`if (functionParsed.Name === '...')` chain in `ExecuteFunctionContextSwitch` (L7338–L7536), not a switch.
Docs: `src/WebDocs/wwwroot/app/functions/<Name>/parameters.json` (96 folders).

## Summary

| Metric | Count |
|---|---|
| Documented functions | **96** |
| OK | **86** |
| DOC-TOO-HIGH (doc requires more than the engine) | **7** at the branch point (all fixed by this feature) |
| DOC-TOO-LOW (engine needs more than the doc says) | **3** |
| NO-ENGINE-IMPL | **0** |
| Engine dispatch cases with no docs folder | **1** (`external`, a no-op stub) |

### How "Doc min" and "Engine min" were computed

- **Doc min** = what `CheckArity` actually uses: `parameters.Count(p => !p.Optional)`. `FunctionParameterVM.Optional`
  is a non-nullable `bool`, so an entry with **no `Optional` key counts as required**. This matters for two files
  (`FilterData` — `DataKey` has no `Optional`; `GetSector` — a nameless `{ "Types": [""] }` entry).
- **Engine min** = number of leading `functionParsed.Parameters[i]` the engine dereferences with no
  `Parameters.length` guard **and** whose absence crashes or clearly misbehaves. Two engine facts drive this:
  - `ResolveFunctionParameter(undefined)` (L7268) does **not** throw: `HasMustache` rejects non-strings and the
    value is returned as-is (`undefined`). It **does** throw when called with `canUseReturnFunction=true`
    (`ParseFunction(undefined)` → `data.indexOf` TypeError, L11184) — used by UpdateItemField/UpdateDataField/
    ReplaceItemField/UpdateData for the *value* argument.
  - `Parser.ParseMustache(undefined)` (L11079, `data.substr`), `Parser.IsUri(undefined)` (L11588, `data.length`),
    `ParseFunctions(undefined)` (via `ParseBlock`, `data.length`), `key.toLowerCase()` all throw.
    `IsMustache`, `GetStringAsNumber`, `ParseNumber`, `Tokenize/ParseFor`, `ParseDateCulture(null)` tolerate null/undefined.
- Reads that are unguarded but immediately null-checked (`(x == null || x == '') ? default : ...`) are counted as
  **optional** (the engine's own default applies). The "strict" count of unguarded reads is given in Notes when it differs.
- Reads whose necessity depends on the *value* of an earlier argument are recorded in Notes; Engine min is the
  unconditional (lower) number.

## Table (mismatches first)

| Function | Doc min | Engine min | Verdict | Engine evidence (line + quote) | Notes |
|---|---|---|---|---|---|
| AddDate | 3 | 0 | DOC-TOO-HIGH | L8390 `functionParsed.Parameters.length > 0 ? ... : null`; L8393 `> 1 ? ... : 'day'`; L8395 `> 2 ? ... : '1'`; L8403 `> 3 ? ... : 'date'` | Every argument is guarded with a default: missing Date → `new Date()` (L8392), Type → `'day'`, Increment → `1`, Return Type → `'date'`. `AddDate()` is a legal "tomorrow". Doc marks Date, Type, Increment as required. |
| HasDataChanges | 2 | 0 | DOC-TOO-HIGH | L8159 `Parameters.length <= 0 ? null : ...`; L8162 `Parameters.length <= 1 ? null : ...` | Both guarded. `RetrieveStorageItemsCached(null, null)` (L15821) treats null/'' sector as *all sectors* and null/'' key as *all data*, so `HasDataChanges()` = "any change anywhere". Doc marks both required. |
| AcceptDataChanges | 2 | 0 | DOC-TOO-HIGH | L8172 `Parameters.length <= 0 ? null : ...`; L8175 `Parameters.length <= 1 ? null : ...` | Same shape as HasDataChanges (same helper). `AcceptDataChanges()` accepts everything. Doc marks both required. |
| CreateData | 3 | 1 | DOC-TOO-HIGH | L8046 `const dataKey = functionParsed.Parameters[0];` (unguarded); L8050 `for (let i = 2; i < functionParsed.Parameters.length - 1; i = i + 2)` | Only DataKey is read unconditionally; Notify ([1]) is null-checked (L8048); Key/Value are consumed by the pairwise loop, so `CreateData(k)` / `CreateData(k,true)` legally create an empty object `{}`. Doc marks Key ([2]) and Value ([3]) required. Note the doc's own inline comment in the validator already calls CreateData variadic. |
| CreateGuid | 2 | 0 | DOC-TOO-HIGH | L8359 `if (functionParsed.Parameters.length == 0) return (value);` then L8361–8362 `Parameters[0]` / `Parameters[1]` unguarded | Two valid shapes: **0 args** (returns the guid as the expression value, e.g. inside `UpdateItemField({{item.Id}},CreateGuid())`) or **≥ 2 args** (writes it to DataKey/DataField). With exactly 1 arg `dataField` is `undefined` → `SetDataKeyField(dataKey, sector, [undefined], ...)` (L8365) misbehaves. Doc marks DataKey and DataField required, so a bare `CreateGuid()` is flagged. |
| GetSector | 1 | 0 | DOC-TOO-HIGH | L8710–8712 `return (this.Application.Document.GetSector(element));` — no `Parameters` read at all | `parameters.json` is `[ { "Types": [ "" ] } ]`: one nameless, description-less entry with **no `Optional` key**, which `!p.Optional` counts as required. The repo's own samples call `GetSector()` (2 occurrences) → the validator warns on them today. Doc file should be `[]`. |
| ShowWindow | 2 | 1 | DOC-TOO-HIGH | L8312 `Parameters[0]` unguarded; L8313 `IsUri(windowNameOrUri)`; L8316 `const did = isUri ? await ...Parameters[1] : null;`; L8317 `for (let i = isUri ? 2 : 1; i < length - 1; i += 2)` | `did` is only needed when arg 0 starts with `~` or `/` (Parser.IsUri L11586). A definition name alone is valid (`ShowWindow(center)` appears in the samples). `IsUri(undefined)` throws, so 0 args crashes → min 1. Fixed: `did` → `Optional:true`, plus a `CheckConditionalArity` rule for the url case. |
| FilterData | 2 | 3 | DOC-TOO-LOW | L8104 `if (functionParsed.Parameters.length < 3) return ('');` | Engine **silently no-ops** (no crash) with fewer than 3 slots. Doc: `d-for` required, `d-if` optional, `DataKey` has **no `Optional` key** (counts as required by accident), `Notify` optional → validator min 2. A call `FilterData(item in data,dataFiltered)` passes the validator but does nothing in the engine (and would put the datakey in the d-if slot anyway). |
| ExecuteInstanceFunction | 1 | 3 | DOC-TOO-LOW | L8520 `const mustacheReturn = functionParsed.Parameters[2];` L8521 `if ((mustacheReturn !== null) && (mustacheReturn !== ''))` → L8522 `ParseMustache(mustacheReturn)` | The `!== null` test does **not** catch `undefined`, so with 2 args the engine reaches `ParseMustache(undefined)` → `data.substr` **TypeError (crash)** right after the instance function has already run. With 1 arg it no-ops (`functionName` undefined → `instance[undefined] == null → return ''`, L8507–8510). [0] Sector is null-checked (empty = current sector, L8503) but the *slot* must exist; [3+] are the variadic Parameters loop (L8512). Samples always pass 4 (`ExecuteInstanceFunction(,Increment,{{internal.value}},{{internal.value}})`). |
| PushStack | 0 | 1 | DOC-TOO-LOW | L8409 `const value = await this.ResolveFunctionParameter(..., functionParsed.Parameters[0]);` L8410 `executionContext.Stack.Push(value);` | Unguarded read; engine **tolerates** it (pushes `undefined`, no crash) but a `PushStack()` is meaningless and a later `PopStack` yields `undefined`. Doc marks Data optional with `DefaultValue: "Empty"`. |
| *— end of mismatches; every row below is OK —* | | | | | |
| AddClass | 1 | 1 | OK | L8786 `Parameters[0]` (unguarded, `?.trim() ?? ''`); L8787 `if (Parameters.length > 1)` for elementId | Missing class → `''` → `classList.add('')` throws a DOMException, so 1 is right. |
| AddDataItem | 2 | 2 | OK | L7875 `source = Parameters[0]`; L7879 `itemText = Parameters[1]`; L7902/7904 `notifyText`/`isCloneText` null-checked | Strict unguarded count is 4, but [2] and [3] are `(x == null || x == '') ? default`. |
| AddRequestHeader | 2 | 2 | OK | L8562–8563 `Parameters[0]`, `Parameters[1]` unguarded | |
| ApplyRoute | 1 | 0 | OK (doc stricter, harmless) | L8151 `Parameters.length <= 0 ? null : ...`; L8152 `Parameters.length <= 0 ? null : ...Parameters[1]` (guard is copy-pasted `<= 0`, harmless: undefined → null-checked at L8153) | `ApplyRoutePath(null, false)` (L13442) regex-tests `null` against every route and returns `false` → `'false'`. Keeping Url required is the useful choice; flipping it would be technically accurate but low value. |
| Async | 1 | 1 | OK | L8270 `content = Parameters[0]` unguarded; L8272 `Parameters.length > 1 ? ... : null` | Missing content → `ResolveFunctionContext(undefined)` crashes. |
| Break | 0 | 0 | OK | L8706–8709 no `Parameters` read | |
| Cast | 2 | 2 | OK | L8531 `ResolveFunctionExpression(..., Parameters[0])`; L8532 `Parameters[1]` | `ResolveFunctionExpression(undefined)` → `ParseFunctions` → `ParseBlock(undefined).length` crashes. `type` undefined would silently skip the number cast; doc says required, keep. |
| CheckDataField | 2 | 2 | OK | L7773 `Parameters[0]`; L7774 `ResolveFunctionParameterDataFields(..., Parameters[1])`; L7775 notify null-checked | |
| CheckItemField | 1 | 1 | OK | L7829 `ParseMustache(Parameters[0])` (crash if missing); L7830 notify null-checked | |
| ClearData | 1 | 1 | OK | L8032 `dataKey = Parameters[0]`; L8033 `notifyText = Parameters[1]` null-checked | **Doc-shape issue:** doc lists `[1]` as `Sector` and `[2]` as `Notify`; the engine reads `[1]` as **Notify** and never reads `[2]`. `ClearData(k, mySector)` would be parsed as a notify flag. |
| ClearDataField | 2 | 2 | OK | L7789–7791 same shape as CheckDataField | |
| ClearItemField | 1 | 1 | OK | L7707 `ParseMustache(Parameters[0])`; L7708 notify null-checked | |
| ClearPlumber | 0 | 0 | OK | L8695–8698 no `Parameters` read | |
| ClearSector | 1 | 1 | OK | L8018 `Parameters[0]` | |
| ClearToken | 0 | 0 | OK | L8230–8233 no `Parameters` read | |
| ClearValidation | 1 | 1 | OK | L8727 `validation = Parameters[0]` | |
| ClosePage | 0 | 0 | OK | L8208–8211 no `Parameters` read | |
| CloseWindow | 0 | 0 | OK | L8330 `Parameters.length > 0 ? ... : null`; L8333 `> 1 ? ... : 'false'`; L8335 `> 2 ? ... : null` | |
| ContainsDataItem | 2 | 2 | OK | L7940 `Parameters[1]`; L7950 `Parameters[0]` unguarded | |
| CreateReference | 1 | 1 | OK | L8588 `value = Parameters[0]` | |
| CreateTick | 0 | 0 | OK | L8371 `if (Parameters.length == 0) return (value);`; L8377 `> 1 ? ... : null` | 0 or ≥1 args; with 1 arg it parses the mustache (L8374). |
| CreateTimer | 2 | 2 | OK | L8573 `content = Parameters[0]`; L8574 `Parameters[1]`; L8575 loop null-checked | `time` undefined → `ParseNumber(undefined, 0)` → fires immediately (tolerated but wrong); doc's "required" is right. |
| Debugger | 1 | 1 | OK | L8701 `for (i = 0; i < Parameters.length; i++)` then `Debugger.ExecuteFunctionDebugger(parameters)` L4293 `parameters[0].toLowerCase()` | Helper crashes with 0 args → min 1. Conditional: `highlight` needs `parameters[1]` (L4303) and `[2]`/`[3]` (L4310–4311, L4321–4322); `reload`/`persist` need nothing more. Docs already model this (Command required, rest optional). |
| DestroyContainer | 1 | 1 | OK | L8243 `itemText = Parameters[0]` | |
| DetectView | 0 | 0 | OK | L8642–8655 no `Parameters` read | |
| DownloadData | 1 | 1 | OK | L8599 `dataKeyFile = Parameters[0]` | |
| EncodeUrl | 1 | 1 | OK | L8556 `ResolveFunctionExpression(..., Parameters[0])` (crash if missing) | |
| Execute | 1 | 1 | OK | L8440 `Parameters.length > 1 ? ...Parameters[1] : sector`; L8441 `Parameters[0]` unguarded | |
| ExecuteComponentFunction | 2 | 2 | OK | L8480 `Parameters[0]` (null → return ''); L8486 `Parameters[1]`; L8491 `for (i = 2; ...)` | Engine no-ops (no crash) when either is missing; doc's 2 is the meaningful minimum. |
| ExecuteDataItem | 2 | 2 | OK | L8446 `expression = Parameters[0]`; L8447 `forText = Parameters[1]`; L8448/8450 `[2]`,`[3]` guarded; L8455 `forHierarchyText = Parameters[4]` unguarded | `[4]` undefined is tolerated (`ControlFlow.ExecuteDataItem` checks `forText == null`). `ParseFor(undefined)` returns null → no-op. |
| ExecuteValidation | 1 | 1 | OK | L8722 `validation = Parameters[0]` | |
| Focus | 0 | 0 | OK | L8294 `Parameters[0]`; L8295 `if ((did === null) \|\| (did === '') \|\| (did === undefined))` → blur active element | Explicit `undefined` check; [1] null-checked (L8304). The heuristic flagged 2 unguarded reads; hand-check says 0. |
| GetClipboard | 1 | 1 | OK | L8714 `ParseMustache(Parameters[0])` (crash if missing); L8715 notify null-checked | |
| GetConfig | 1 | 1 | OK | L8666 `Parameters[0]` then L8667 `key.toLowerCase()` (crash if missing) | |
| GetDate | 0 | 0 | OK | L8384 `Parameters.length > 0 ? ... : 'date'` | |
| GetExternal | 2 | 2 | OK | L7554–7556 `[0]`,`[1]`,`[2]`; L7557 isClone null-checked | Strict unguarded count 3. |
| GetExternalFrame | 3 | 3 | OK | L7595–7598 `[0]`..`[3]`; L7599 isClone null-checked | Strict 4. |
| GetExternalFrameMessage | 3 | 3 | OK | L7637–7640 `[0]`..`[3]`; L7641 isClone null-checked | Strict 4. |
| GetWindow | 0 | 0 | OK | L8351–8356 no `Parameters` read | |
| HasToken | 0 | 0 | OK | L8234–8236 no `Parameters` read | |
| HideWindow | 0 | 0 | OK | L8340 `> 0 ? Parameters[0] : null`; L8341 `> 1 ? ... : 'false'`; L8343 `> 2 ? ... : null` | |
| IF | 2 | 2 | OK | L8256 `conditional = Parameters[0]`; L8260 `statementTrue = Parameters[1]` (inside `if (conditionalResult)`); L8263 `else if (Parameters.length > 2)` | `[1]` is read only when the condition is true, but it is unguarded there (`ResolveFunctionContext(undefined)` crashes), so 2 is right. |
| LoadPack | 1 | 0 | OK (doc stricter, harmless) | L8090 `if (Parameters.length < 1) return ('');`; L8096 `if (Parameters.length > 1)` | Engine explicitly no-ops with 0 args; documenting PackName as required is the useful choice. |
| LoadSectorContent | 2 | 2 | OK | L8024–8025 `[0]`, `[1]` | |
| LockData | 1 | 1 | OK | L8684 `Parameters[0]` | |
| LockPlumber | 0 | 0 | OK | L8675–8678 no `Parameters` read | |
| MoveItem | 2 | 2 | OK | L7836 `key = ...Parameters[0]`; L7837 `rangeIndex = ...Parameters[1]`; L7838 notify null-checked | **Doc-shape issue:** `key` ([0]) is resolved but **never used** — the engine moves `contextItem.Data` inside `contextItem.DataKey` (L7839–7845). The slot must exist positionally, so keeping it required is correct for arity. |
| Notify | 1 | 1 | OK | L8285 `Parameters[0]`; L8286 `GetStringAsNumber(...Parameters[1])` (null-safe); L8287 `ResolveFunctionParameterDataFields(...Parameters[2])` (null-safe); L8288 `> 3 ? ... : null` | **Doc-shape issue:** engine reads 4 params (DataKey, DataIndex, DataFields, CanUseDifference); doc lists only DataKey. `[1]`/`[2]` undefined → null (tolerated). |
| PeekStack | 0 | 0 | OK | L8428 `if (Parameters.length == 0) return (value);`; L8434 `> 1 ? ... : null` | |
| PopStack | 0 | 0 | OK | L8415 `if (Parameters.length == 0) return (value);`; L8421 `> 1 ? ... : null` | |
| PostData | 1 | 1 | OK | L8185 `Parameters[0]`; L8186–8188 `dataKeyResponse = Parameters[1]; if (== null) dataKeyResponse = dataKey`; L8189 notify null-checked | Strict 3. |
| PostDataItem | 1 | 1 | OK | L8195–8199 same shape as PostData | Strict 3. |
| RedirectPage | 1 | 1 | OK | L8213 `Parameters[0]` | |
| ReloadData | 1 | 1 | OK | L8074 `Parameters[0]`; L8075 notify null-checked | |
| ReloadPage | 0 | 0 | OK | L8204–8207 no `Parameters` read | |
| ReloadPipe | 1 | 1 | OK | L8081 `Parameters[0]` then `if (dataPipe == null \|\| '') return ''`; L8084 `> 1 ? ... : null` | Engine no-ops without it; doc's required is the useful choice. |
| ReloadSector | 1 | 1 | OK | L8009 `Parameters[0]` | |
| RemoveClass | 1 | 1 | OK | L8796 `Parameters[0]`; L8797 `if (Parameters.length > 1)` | Same as AddClass. |
| RemoveDataItem | 2 | 2 | OK | L7909 `Parameters[0]`; L7913 `Parameters[1]`; L7924 notify null-checked | Strict 3. |
| RemoveDataItemLookup | 3 | 3 | OK | L7931–7933 `[0]`,`[1]`,`[2]`; L7934 `> 3 ? ... : null` | |
| ReplaceItemField | 3 | 3 | OK | L7734 `ParseMustache(Parameters[0])`; L7747–7748 `Parameters[1]`, `Parameters[2]` with `canUseReturnFunction=true` (crash if missing); L7749 `length < 4 ? null`; L7751 `length < 5 ? null`; L7743 `length > 5 ? Parameters[6]`; L7745 `length > 6 ? Parameters[7]` | **Doc-shape issue:** engine's Recursive is **index 6** and Resolve **index 7** (index 5 is never read — engine off-by-one); doc lists Recursive at 5 and Resolve at 6. Arity is fine. |
| Round | 1 | 1 | OK | L8538 `ResolveFunctionExpression(..., Parameters[0])`; L8540 `> 1 ? ... : null`; L8542 `> 2 ? ... : null` | |
| SetClipboard | 1 | 1 | OK | L8568 `Parameters[0]` | |
| SetConfig | 2 | 2 | OK | L8657–8658 `[0]`,`[1]`; L8660 `key.toLowerCase()` (crash if key missing) | value undefined → `ParseNumber(undefined,0)` → 0 (tolerated but wrong); keep required. |
| SetExternal | 2 | 2 | OK | L7543–7545 `[0]`,`[1]`,`[2]`; L7546 isClone null-checked | Strict 3. |
| SetExternalFrame | 3 | 3 | OK | L7566–7569 `[0]`..`[3]`; L7570 isClone null-checked | Strict 4. |
| SetExternalFrameMessage | 3 | 3 | OK | L7612–7615 `[0]`..`[3]`; L7616 isClone null-checked | Strict 4. |
| SwitchSector | 2 | 2 | OK | L8003–8004 `[0]`,`[1]` unguarded → `SectorContainerHandler.Switch(sectorName, container)` | `container` undefined behaves like `ClearSector` (`Switch(sector, containerCode = null)` L13625; ClearSector passes `null` at L8020); doc's required is right. |
| ToggleData | 2 | 2 | OK | L7669 `Parameters[0]`; L7673 `Parameters[1]`; L7696 notify null-checked | Strict 3. |
| ToggleItemField | 1 | 1 | OK | L7659 `ParseMustache(Parameters[0])`; L7660 notify null-checked | |
| UncheckDataField | 2 | 2 | OK | L7781–7783 same shape as CheckDataField | |
| UncheckItemField | 1 | 1 | OK | L7702 `ParseMustache(Parameters[0])` — only read | **Doc-shape issue:** doc lists a `Notify` [1]; the engine never reads it (always notifies, L7703). |
| UnloadData | 1 | 1 | OK | L8039 `Parameters[0]`; L8040 notify read but **unused** (L8042 `UnloadData(dataKey, sector)`) | |
| UnlockData | 1 | 1 | OK | L8689 `Parameters[0]`; L8690 `> 1 ? ... : null` | |
| UnlockPlumber | 0 | 0 | OK | L8679–8682 no `Parameters` read | |
| UpdateData | 2 | 2 | OK | L8060 `Parameters[0]`; L8065 `value = Parameters[1]` → L8066 `ResolveFunctionParameter(value, true, recursive)` (`recursive` defaults **true** → `ParseFunction(undefined)` crash); L8068 notify null-checked; L8061/8063 `> 3`/`> 4` guarded | |
| UpdateDataField | 3 | 3 | OK | L7797 `[0]`; L7798 `DataFields([1])`; L7803 `Parameters[2]` with `canUseReturnFunction=true` (crash if missing); L7804 `> 3`; L7799 `> 4`; L7801 `> 5` guarded | |
| UpdateDataFieldLookup | 5 | 5 | OK | L7810–7814 `[0]`..`[4]` unguarded; L7823 `Parameters.length > 3 ? Parameters[5] : null` | `[4]` undefined → writes `undefined` into the field (clearly wrong) → required. Guard for `[5]` is `> 3` (engine off-by-one, harmless). |
| UpdateDataURL | 2 | 2 | OK | L7848–7849 `[0]`,`[1]` | |
| UpdateDataUrlSet | 2 | 2 | OK | L7862–7863 `[0]`,`[1]` | |
| UpdateItemField | 2 | 2 | OK | L7714 `ParseMustache(Parameters[0])`; L7727 `Parameters[1]` with `canUseReturnFunction=true` (crash if missing); L7728 notify null-checked; L7723/7725 `> 3`/`> 4` guarded | |
| UpdateSector | 2 | 2 | OK | L7996–7997 `[0]`,`[1]` unguarded; L7974 `if (length >= 3) Parameters[2]`; L7976 `canRouteText = Parameters[3]` null-checked; L7978 `>= 4 ? Parameters[4]`; L7980 `>= 5 ? Parameters[5]` | Guards for `[4]`/`[5]` are off by one (harmless: undefined → null path). |
| UpdateToken | 1 | 1 | OK | L8226 `Parameters[0]` | |
| UpdateTokenAntiforgery | 1 | 1 | OK | L8238 `Parameters[0]` | |
| UpdateURL | 1 | 1 | OK | L8219 `Parameters[0]`; L8220 `> 1 ? ... : null` | |
| Wait | 1 | 1 | OK | L8593 `Parameters[0]` → `ParseNumber(time, 0)` | undefined → 0 ms (tolerated); doc's required is right. |

(The "AcceptDataChanges …" separator row above is just a visual divider between mismatches and OK rows.)

## Coverage cross-check

- **Documented functions with no engine implementation / dispatch case:** none. All 96 folders map to an
  `if (functionParsed.Name === '<name>')` branch and an `ExecuteFunction<Name>` method.
- **Engine dispatch cases with no docs folder:** `external` (L7340 → `ExecuteFunctionExternal`, L7538–7540:
  `return ('')`). It is an inert placeholder; nothing to document.
- 97 dispatch names, 100 `ExecuteFunction*` methods (the extra three are non-dispatched helpers/stubs:
  `ExecuteFunctionExternal`, `ExecuteFunctionContextSwitch`, and `Debugger.ExecuteFunctionDebugger`).

## Recommended `parameters.json` changes

### DOC-TOO-HIGH (flip to `Optional: true`)

1. **AddDate** — flip `Date`, `Type`, `Increment` to `Optional: true`.
   - `Date`: *"The date to add to. When omitted (or not parseable) the current date/time is used."* (`DefaultValue: "now"`)
   - `Type`: *"The unit to add: day, month or year. Used only when you want something other than the default of day."* (`DefaultValue: "day"`)
   - `Increment`: *"How many units to add (negative values subtract). Defaults to 1 when omitted."* (`DefaultValue: "1"`)
2. **HasDataChanges** — flip `Sector` and `DataKey` to `Optional: true`.
   - `Sector`: *"Restricts the check to one sector; `=` means the current sector. When omitted or empty every sector is checked."* (`DefaultValue: "all sectors"`)
   - `DataKey`: *"Restricts the check to one data key or data group. When omitted or empty every storage item in the selected sector(s) is checked."* (`DefaultValue: "all data"`)
3. **AcceptDataChanges** — same two flips and the same wording as HasDataChanges (replace "checked" with "accepted").
4. **CreateData** — flip `Key` and `Value` to `Optional: true`.
   - `Key`: *"Name of a property to set on the new object. Key/Value pairs may be repeated; when no pairs are given an empty object `{}` is created."*
   - `Value`: *"Value for the preceding Key. Must always be paired with a Key (a trailing Key without a Value is ignored by the engine)."*
5. **CreateGuid** — flip `DataKey` and `DataField` to `Optional: true`.
   - `DataKey`: *"Storage that receives the guid. When omitted the function returns the guid as its value instead, so it can be used as an expression (e.g. `UpdateItemField({{item.Id}},CreateGuid())`). When DataKey is given, DataField is required too."*
   - `DataField`: *"The field of DataKey that receives the guid. Required whenever DataKey is given; ignored when both are omitted."*
   - Optional follow-up for the tooling: a `CheckConditionalArity` rule "`createguid` with exactly 1 argument → warn" mirrors the ShowWindow rule (the engine has no valid 1-argument shape).
6. **GetSector** — replace the file content with `[]` (delete the nameless `{ "Types": [ "" ] }` entry). The engine reads no parameters; the current entry makes the validator flag the repo's own `GetSector()` samples.
7. **ShowWindow** — `did` → `Optional: true`, plus a `CheckConditionalArity` rule that warns when a literal url (starts with `~` or `/`) is passed without a did.

### DOC-TOO-LOW (engine needs more)

1. **FilterData** — engine returns `''` (silent no-op, **no crash**) below 3 slots (L8104). Set `"Optional": false`
   explicitly on `DataKey` (it is currently missing and only counts as required by accident) and flip `d-if` to
   `Optional: false` with a description such as *"Conditional applied to each item. The slot must be present but may
   be left empty (`FilterData(item in data,,dataFiltered)`) to keep every item."* That makes the documented minimum 3,
   matching the engine's own `length < 3` guard.
2. **ExecuteInstanceFunction** — engine **crashes** (unguarded `ParseMustache(undefined)`, L8520–8522) when the third
   slot is absent, and silently no-ops when the second is absent. Because both `Sector` ([0]) and `Return` ([2]) are
   *optional by value* but *positionally required*, the only way to express a 3-slot minimum with the current
   `Optional` bool is to mark both `Optional: false` with wording like:
   - `Sector`: *"Sector whose component instance is used. Leave the slot empty (`ExecuteInstanceFunction(,fn,...)`) to use the current sector — the slot itself must be present."*
   - `Return`: *"Mustache that receives the function's return value. Leave empty to discard the result, but the slot must be present: the engine dereferences it unconditionally."*
   Alternative that keeps the docs semantically "optional": add a `CheckConditionalArity` rule
   "`executeinstancefunction` with fewer than 3 arguments → warn".
3. **PushStack** — engine **tolerates** it (pushes `undefined`, no crash) but the call is meaningless. Flip `Data` to
   `Optional: false` and drop `DefaultValue: "Empty"`; description: *"The value (mustache or text) pushed onto the
   execution-context stack; a later PopStack/PeekStack returns it."*

### Informational doc-shape findings (not arity, but worth a follow-up PR)

- **ClearData**: doc's second parameter is `Sector`; the engine reads `[1]` as **Notify** and reads nothing at `[2]`. Docs should be `DataKey`, `Notify` (the engine always uses the current sector).
- **UncheckItemField**: doc lists `Notify`; the engine never reads it (always notifies).
- **UnloadData**: `Notify` is read but not passed on (`Storage.UnloadData(dataKey, sector)`).
- **MoveItem**: `Key` ([0]) is resolved and then ignored; the engine moves `contextItem.Data` inside `contextItem.DataKey`.
- **Notify**: engine accepts `DataIndex` ([1]), `DataFields` ([2]) and `CanUseDifference` ([3], default true); docs list only `DataKey`.
- **ReplaceItemField**: engine reads `Recursive` at index **6** and `Resolve` at index **7** (index 5 is skipped — `length > 5 ? Parameters[6]`); docs place them at 5 and 6, so a call following the docs would put Recursive into an ignored slot.
- **FilterData**: `DataKey` lacks an explicit `Optional` key (works only because `bool` defaults to false).
- Engine off-by-one guards (harmless, undefined falls through to the null default): `ApplyRoute` `[1]` guarded by `length <= 0`, `UpdateDataFieldLookup` `[5]` by `length > 3`, `UpdateSector` `[4]`/`[5]` by `>= 4`/`>= 5`.
