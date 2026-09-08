# Contract: Drapo language server (LSP 3.17 over stdio)

**Binary**: `Drapo.LanguageServer[.exe]`. **Transport**: stdin/stdout JSON-RPC with
`Content-Length` framing. **Logging**: stderr only (stdout is reserved for the protocol).

## Command line

| Argument | Meaning | Default |
|----------|---------|---------|
| `--content <dir>` | folder containing `functions/` and `menu/` | `<exe dir>/content/app` |
| `--version` | print engine + server version and exit | — |

Exit codes: `0` after `exit` following `shutdown`; `1` on fatal startup error (content folder missing,
engine resource not found) with a one-line reason on stderr.

## `initialize` result — `ServerCapabilities`

```json
{
  "textDocumentSync": { "openClose": true, "change": 1, "save": { "includeText": false } },
  "completionProvider": { "triggerCharacters": ["-"], "resolveProvider": false },
  "hoverProvider": true,
  "signatureHelpProvider": { "triggerCharacters": ["(", ","], "retriggerCharacters": [","] }
}
```

`serverInfo`: `{ "name": "drapo-language-server", "version": "<informational version>" }` (the engine
version is printed by `--version`).

Note: the framework advertises a static capability only when the client declares support for that
feature in `initialize` (VS Code does for all four); it also reports `save.includeText` as `true`.

Document selector (registered by the client; the server accepts any URI it is sent): languages
`html`, `razor`, `aspnetcorerazor`, schemes `file` and `untitled`.

## Notifications

### `textDocument/didOpen` / `didChange` / `didSave`

- Store full text (change kind 1 = Full; the last content change wins).
- Schedule validation after 250 ms of quiet; a newer change cancels the pending one.
- Publish `textDocument/publishDiagnostics` `{ uri, version, diagnostics[] }` where each item is:

| Field | Value |
|-------|-------|
| `range` | start `(Line-1, Column-1)`; end = start + identifier run length, min 2 chars, clamped to line end |
| `severity` | 1 for `Level == "error"`, 2 for `"warning"` |
| `code` | `Rule` |
| `source` | `"drapo"` |
| `message` | `Message` verbatim |

Ordering is the validator's (line, then column). An empty list is published when the document is
clean.

### `textDocument/didClose`

Remove the document and publish an empty diagnostics list for its URI.

## Requests

### `textDocument/completion`

Returns `CompletionList { isIncomplete: false, items }` when the cursor is *inside a tag* (see
research R6) and the word before the cursor is empty, `d`, or starts with `d-`; otherwise returns an
empty list. Items:

| Item kind | `label` | `kind` | `insertText` | `documentation` |
|-----------|---------|--------|--------------|-----------------|
| Fixed engine attribute (e.g. `d-for`) | name | `Property` (10) | name | doc description (markdown) or absent |
| Documented family member (e.g. `d-on-model-change`) | name | `Property` | name | doc description |
| Prefix family (e.g. `d-on-`) | prefix | `Keyword` (14) | prefix | `Prefix family: complete with the suffix` |

`textEdit` replaces the partial word already typed (from the word start to the cursor). Items are
de-duplicated case-insensitively and sorted by label; `sortText` puts fixed attributes before
prefixes.

### `textDocument/hover`

| Cursor context | Result |
|----------------|--------|
| Over a `d-*` token inside a tag that is documented | `MarkupContent` markdown: `**d-name**` + blank line + description |
| Over a `d-*` token recognised by the engine but undocumented | `**d-name**` + `_Recognised by the Drapo engine; no documentation page._` |
| Over a documented function name inside a `d-on-*` value | markdown: signature line in a code span, description, then a table `Parameter / Types / Optional / Default / Description` |
| Anything else | `null` |

`range` covers the token.

### `textDocument/signatureHelp`

When the cursor is inside the parentheses of a call in a `d-on-*` value and the callee is
documented:

```json
{
  "signatures": [{
    "label": "<FunctionVM.Signature>",
    "documentation": { "kind": "markdown", "value": "<description>" },
    "parameters": [{ "label": "<Name>", "documentation": "<Description> (types: a|b, optional, default: x)" }]
  }],
  "activeSignature": 0,
  "activeParameter": "<top-level commas before the cursor, clamped to parameters.Count - 1>"
}
```

Otherwise `null`. Nested calls resolve to the innermost unmatched `(`.

## Robustness

Every handler catches exceptions from provider logic, logs to stderr, and returns the empty result
for that request (`[]`, `null`) rather than an error response; the process never exits because of
document content (FR-016). `shutdown`/`exit` are honoured; an `exit` without `shutdown` returns 1
per the LSP spec.
