# Data Model: Drapo Language Server

**Feature**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

All entities already exist as `*VM` classes in WebDocs and **move unchanged** (same property names,
same JSON casing) into `Drapo.Tooling.Models`. The table columns show how each is projected to the
two consumers. Nothing here is persisted; everything is derived from files at read time.

## Entities

### Attribute (`AttributeVM`)

| Field | Type | Source | MCP (`get_attributes`) | LSP |
|-------|------|--------|------------------------|-----|
| `Name` | string | file name in `app/menu/0003 - Attributes/NNNN - <name>.html` minus prefix | as-is | `CompletionItem.label`, hover title |
| `Description` | string (markdown) | first `<p>` of the page | as-is | `CompletionItem.documentation`, hover body |
| `Details` | string (markdown) | full page (only when requested by name) | `get_attribute_details` | not used in v1 |

Rules:
- An attribute is *offered* (completion) only if `IDrapoEngineCatalog.IsValidAttribute(Name)` is true.
  Documented names that the engine does not know are never offered (Principle I).
- Engine attributes without a doc page are offered with no documentation.

### Attribute family / prefix (engine only, no VM)

| Field | Type | Source |
|-------|------|--------|
| `Prefix` | string, ends with `-` | `DrapoEngineCatalog` (`d-on-`, `d-attr-`, plus known dynamic `d-validation-`, `d-dataproperty-`) |

Offered as a completion item whose label is the prefix so the user keeps typing the suffix.

### Function (`FunctionVM`)

| Field | Type | Source | MCP | LSP |
|-------|------|--------|-----|-----|
| `Name` | string | folder name `app/functions/<Name>/` | as-is | hover title, signature label |
| `Description` | string (markdown) | `description.html` | as-is | hover body, `SignatureInformation.documentation` |
| `Signature` | string | synthesised by `DrapoDocContent.BuildFunctionSignature` | as-is (details only) | `SignatureInformation.label` |
| `Parameters` | `FunctionParameterVM[]` | `parameters.json` | as-is | `ParameterInformation[]`, hover table |
| `Samples` | `FunctionSampleVM[]` | `samples/NNN/` | as-is | not used in v1 |

Rules:
- Arity checks (validator) use `Parameters.Count(p => !p.Optional)` as the minimum; no maximum
  (variadic functions exist).
- A function is *recognised* if `IDrapoEngineCatalog.IsValidFunction(name)`; it is *documented* if a
  folder exists. Recognised-but-undocumented functions produce no arity diagnostic, no hover and no
  signature help (same as today).

### Parameter (`FunctionParameterVM`)

| Field | Type | Notes |
|-------|------|-------|
| `Name` | string | `ParameterInformation.label` |
| `Description` | string | `ParameterInformation.documentation` |
| `Types` | string[] | rendered `text|url` in signature; `any` when empty |
| `Optional` | bool | rendered `[…]` in signature |
| `DefaultValue` | string? | rendered ` = value` inside the brackets |

### Diagnostic (`DrapoDiagnosticVM`) → LSP `Diagnostic`

| VM field | Type | LSP mapping |
|----------|------|-------------|
| `Level` | `"error"` \| `"warning"` | `severity`: Error / Warning |
| `Rule` | string (`unknown-attribute`, `unknown-function`, `wrong-arity`, `malformed-dfor`, `unbalanced-mustache`) | `code` |
| `Message` | string | `message` (verbatim) |
| `Line` | int, 1-based | `range.start.line = Line - 1` |
| `Column` | int, 1-based, UTF-16 units | `range.start.character = Column - 1` |
| — | — | `range.end`: start + identifier-run length (`[A-Za-z0-9_-]+`), minimum 2 characters |
| — | — | `source = "drapo"` |

**Invariant (SC-001)**: for any text, `Map(Validate(text))` has the same length as
`Validate(text).Diagnostics` and element *i* preserves `Level`, `Rule`, `Message`, `Line`, `Column`.
The VM is not extended; the range end is derived in the server.

### Validation result (`DrapoValidationResultVM`)

Unchanged; MCP-only wrapper (`Valid`, `ErrorCount`, `WarningCount`, `EngineVersion`,
`Diagnostics[]`). The server consumes only `Diagnostics`.

### Document (server-only, `DocumentStore` entry)

| Field | Type | Notes |
|-------|------|-------|
| `Uri` | DocumentUri | key |
| `Text` | string | full text (sync kind Full) |
| `Version` | int? | last seen version, echoed in `publishDiagnostics` |
| `LanguageId` | string | `html`, `razor`, `aspnetcorerazor` |

Lifecycle: `didOpen` → created; `didChange`/`didSave` → replaced, diagnostics rescheduled (250 ms
debounce, previous run cancelled); `didClose` → removed and diagnostics published as an empty list.

### Content root (`IDrapoContentRoot`)

| Field | Type | WebDocs | Language server | Tests |
|-------|------|---------|-----------------|-------|
| `AppPath` | string (absolute) | `<wwwroot>/app` | `<exe dir>/content/app` or `--content <path>` | `<repo>/src/WebDocs/wwwroot/app` |

Layout under `AppPath` is the constitution's convention and is identical in all three.

### Symbol index (server-only, `DrapoSymbolIndex`)

Immutable snapshot built once at startup:

- `Attributes`: `IReadOnlyDictionary<string /*lower*/, AttributeVM>` for documented attributes that
  pass `IsValidAttribute`.
- `EngineAttributes`: `IReadOnlyCollection<string>` fixed names from the catalog.
- `Prefixes`: `IReadOnlyCollection<string>`.
- `Functions`: `IReadOnlyDictionary<string /*lower*/, FunctionVM>` with parameters and signature.

## Relationships

```
IDrapoContentRoot ──► FunctionService ──► FunctionVM ──► FunctionParameterVM
                  └─► AttributeService ─► AttributeVM
Drapo assembly ───► DrapoEngineCatalog (attributes, prefixes, functions)
DrapoValidatorService ◄── (catalog, FunctionService) ──► DrapoValidationResultVM ──► DrapoDiagnosticVM
DrapoSymbolIndex ◄── (catalog, FunctionService, AttributeService)
Providers ◄── (DrapoSymbolIndex, DocumentStore, DrapoValidatorService)
```
