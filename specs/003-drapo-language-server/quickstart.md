# Quickstart: Drapo Language Server

How to build, run and verify the feature end-to-end. Contracts:
[tooling-library-api](contracts/tooling-library-api.md), [lsp-capabilities](contracts/lsp-capabilities.md),
[vscode-extension](contracts/vscode-extension.md).

## Prerequisites

- .NET SDK 8.0.x (pinned by `src/global.json`).
- Node.js 20+ and npm (extension only).
- VS Code 1.90+ (manual scenarios only).
- PowerShell 7 (scripts; Bash equivalents are one-liners shown inline).

## 1. Build everything

```powershell
cd src
dotnet build docs.sln
```

Expected: `Drapo.Tooling`, `Drapo.LanguageServer`, `Drapo.Tests` and `WebDocs` build with 0 errors.
`Drapo.LanguageServer/bin/Debug/net8.0/content/app/functions/` and `.../menu/0003 - Attributes/`
exist (content copied at build).

## 2. Automated checks (SC-001, SC-003, SC-006, FR-003/004)

```powershell
cd src
dotnet test docs.sln
```

Expected green tests, grouped:

| Test class | Proves |
|------------|--------|
| `NoAspNetReferenceTest` | `Drapo.Tooling.dll` references no `Microsoft.AspNetCore.*` (FR-004) |
| `EngineCatalogTests` | `Attributes`/`Prefixes`/`Functions` agree with `IsValid*`; `d-on-click`, `D-For`, `d-validation-id` valid; `d-nope` invalid |
| `ValidatorTests` | every fixture in `Fixtures/*.html` yields exactly the diagnostics in its `*.json` |
| `FunctionSamplesValidateTest` | every `app/functions/*/samples/*/content.html` has 0 errors (Principle III) |
| `ParityTests` | for every corpus document, mapped LSP diagnostics == validator diagnostics in count/line/column/severity/code/message (SC-001) |
| `CompletionProviderTests` | in-tag detection; item set == engine set (SC-003); prefixes offered; nothing outside a tag |
| `HoverProviderTests` | attribute and function hover contents; null elsewhere |
| `SignatureHelpProviderTests` | active parameter by comma count; nested calls; null for unknown callee |
| `RobustnessTests` | malformed corpus never throws from any provider (SC-006) |
| `StdioSmokeTest` | real process: initialize → didOpen(bad attribute) → publishDiagnostics received → shutdown/exit 0 |

## 3. Website and MCP unchanged (FR-003, SC-005)

```powershell
cd src/WebDocs
dotnet run
```

- Open `https://localhost:5001`, browse an attribute page and a function page — rendered as before.
- MCP smoke (any MCP client, or the Drapo MCP tools available to the agent): `get_functions`,
  `get_function_details UpdateSector`, `validate_drapo` with `<div d-nope="1"></div>` → one
  `unknown-attribute` error at line 1, column 6. Compare the JSON to a pre-change capture: identical
  keys and values.

## 4. Run the server by hand (contract check)

```powershell
cd src/Drapo.LanguageServer
dotnet run -- --version          # prints server + engine version, exit 0
```

Then, in a terminal, pipe a minimal session (Bash shown; the framing is `Content-Length`):

```bash
printf 'Content-Length: 88\r\n\r\n{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"capabilities":{},"rootUri":null}}' \
  | dotnet run --no-build
```

Expected: a response whose `result.capabilities` contains `completionProvider`, `hoverProvider`,
`signatureHelpProvider` and `textDocumentSync`.

## 5. VS Code manual scenarios (US1–US5)

Development mode (no packaging needed):

```powershell
cd src/vscode-drapo
npm ci
npm run compile
```

Open `src/vscode-drapo` in VS Code, press F5 (Extension Development Host). In the host, set
`drapo.server.path` to `<repo>/src/Drapo.LanguageServer/bin/Debug/net8.0/Drapo.LanguageServer.exe`
(or the Linux/macOS binary) if you want the freshly built server rather than a bundled one; then
open any `.html` file and check:

| # | Action | Expected |
|---|--------|----------|
| 1 | Type `<div d-nope="x"></div>` | red squiggle on `d-nope`, Problems: `Unknown Drapo attribute 'd-nope'…` code `unknown-attribute`, source `drapo` |
| 2 | Change `d-nope` to `d-if` | squiggle disappears within ~0.5 s |
| 3 | Type `<button d-on-click="UpdateSector()">` | warning: `Function 'UpdateSector' expects at least 2 argument(s) but got 0.` |
| 4 | Inside a tag type `d-` and press Ctrl+Space | list contains `d-for`, `d-if`, `d-model`, `d-on-`, …; no non-Drapo items from this extension |
| 5 | Ctrl+Space in text content | no Drapo attribute items |
| 6 | Hover `d-for` | tooltip with the documented description |
| 7 | Hover `UpdateSector` inside `d-on-click="UpdateSector(a, b)"` | signature line, description, parameter table |
| 8 | Type `d-on-click="UpdateSector(` | signature popup, first parameter highlighted; type `a,` → second highlighted |
| 9 | Paste a 2,000-line page (e.g. concatenate several docs pages) and edit | diagnostics update under 0.5 s; editor stays responsive |
| 10 | Open an ordinary HTML page without Drapo | no diagnostics |
| 11 | Point `drapo.server.path` at a non-existent file and reload | error notification naming the path; no crash |

## 6. Package the extension (US5 / FR-014)

```powershell
cd src/Drapo.LanguageServer
./publish.ps1                       # publishes self-contained builds to bin/publish/<rid>/
cd ../vscode-drapo
./scripts/copy-server.ps1 -Rid win-x64
npm run package -- --target win32-x64   # produces vscode-drapo-<version>@win32-x64.vsix
```

Install the VSIX on a machine with no .NET SDK/runtime (`code --install-extension <file>`), open an
HTML file with `d-nope`, see the diagnostic. Repeat for the other targets as needed. Time from
install to first diagnostic must be under 2 minutes with no configuration (SC-004).

## 7. CI

`.github/workflows/ci.yml` runs steps 1–2 on every PR/push. On a tag matching `lsp-v*` it also runs
step 6 for all four targets and uploads the VSIX files as artifacts.
