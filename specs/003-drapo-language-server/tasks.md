# Tasks: Drapo Language Server

**Input**: Design documents from `/specs/003-drapo-language-server/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Included. The spec's success criteria (SC-001 parity, SC-003 completion set, SC-006
robustness) and issue #385's acceptance list explicitly require an automated parity test, so test
tasks are part of the deliverable.

**Organization**: Phases 1–2 build the shared library (a prerequisite for every story) and the
test scaffold. Phases 3–7 map to the five user stories in priority order. Phase 8 is polish/CI.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1–US5)
- Include exact file paths in descriptions

## Path Conventions

All new code lives under `src/` beside the existing solution (`src/docs.sln`):
`src/Drapo.Tooling/`, `src/Drapo.LanguageServer/`, `src/Drapo.Tests/`, `src/vscode-drapo/`.
Spec artifacts live under `specs/003-drapo-language-server/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the new projects and wire them into the solution so every later task builds.

- [ ] T001 Create `src/Drapo.Tooling/Drapo.Tooling.csproj` (`net8.0` class library; `PackageReference` `Drapo` 2026.8.6.5 with `ExcludeAssets=contentfiles`, `Newtonsoft.Json` 13.0.3; `Nullable` disabled to match WebDocs style)
- [ ] T002 [P] Create `src/Drapo.LanguageServer/Drapo.LanguageServer.csproj` (`net8.0` `Exe`, `OutputType Exe`, `ProjectReference` to Drapo.Tooling, `PackageReference` `OmniSharp.Extensions.LanguageServer` 0.19.9; MSBuild `<Content Include="..\WebDocs\wwwroot\app\functions\**" Link="content\app\functions\%(RecursiveDir)%(Filename)%(Extension)" CopyToOutputDirectory="PreserveNewest" />` and the same for `..\WebDocs\wwwroot\app\menu\0003 - Attributes\*.html` → `content\app\menu\0003 - Attributes\`)
- [ ] T003 [P] Create `src/Drapo.Tests/Drapo.Tests.csproj` (`net8.0`, `Microsoft.NET.Test.Sdk` 17.x, `xunit` 2.9.x, `xunit.runner.visualstudio` 2.8.x, `ProjectReference` to Drapo.Tooling and Drapo.LanguageServer; `Fixtures\**` as `Content` copied to output)
- [ ] T004 Add the three projects to `src/docs.sln` with `dotnet sln src/docs.sln add ...` and confirm `dotnet build src/docs.sln` restores (projects may be empty shells at this point)
- [ ] T005 [P] Add `COPY ["Drapo.Tooling/Drapo.Tooling.csproj", "Drapo.Tooling/"]` before `dotnet restore` in `src/Dockerfile`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Move the catalog/validator/services into `Drapo.Tooling` with no ASP.NET dependency,
keep WebDocs behaviour identical, and stand up the test scaffold that every story's tests use.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete (FR-001, FR-003, FR-004).

- [ ] T006 Create `src/Drapo.Tooling/Content/IDrapoContentRoot.cs` (`string AppPath { get; }`) and `src/Drapo.Tooling/Content/DrapoContentRoot.cs` (ctor validates non-empty path) per contracts/tooling-library-api.md
- [ ] T007 [P] Move `src/WebDocs/Models/{AttributeVM,FunctionVM,FunctionParameterVM,FunctionSampleVM,DrapoDiagnosticVM,DrapoValidationResultVM}.cs` to `src/Drapo.Tooling/Models/` with namespace `Drapo.Tooling.Models` (property names/casing unchanged; delete originals)
- [ ] T008 [P] Move `src/WebDocs/Helpers/DrapoDocContent.cs` to `src/Drapo.Tooling/Helpers/DrapoDocContent.cs` with namespace `Drapo.Tooling.Helpers` (delete original)
- [ ] T009 Create `src/Drapo.Tooling/Helpers/DrapoHandlerSyntax.cs` exposing `FunctionCall`, `ScanFunctionCalls(string)` and `SplitArguments(string)` extracted verbatim from `DrapoValidatorService`
- [ ] T010 Move `src/WebDocs/Services/{IDrapoEngineCatalog,DrapoEngineCatalog}.cs` to `src/Drapo.Tooling/Services/` (namespace `Drapo.Tooling.Services`); add read-only `Functions`, `Attributes` (fixed names only, excluding entries ending in `-`) and `AttributePrefixes` properties backed by the already-parsed sets (delete originals)
- [ ] T011 Move `src/WebDocs/Services/{IFunctionService,FunctionService}.cs` to `src/Drapo.Tooling/Services/`; replace `IWebHostEnvironment` with `IDrapoContentRoot` and every `Path.Combine(_env.WebRootPath, "app", ...)` with `Path.Combine(_root.AppPath, ...)` (delete originals)
- [ ] T012 Move `src/WebDocs/Services/{IAttributeService,AttributeService}.cs` to `src/Drapo.Tooling/Services/`; same `IDrapoContentRoot` substitution for the `menu` folder lookup (delete originals)
- [ ] T013 Move `src/WebDocs/Services/{IDrapoValidatorService,DrapoValidatorService}.cs` to `src/Drapo.Tooling/Services/`; replace the private `ScanFunctionCalls`/`SplitArguments`/`FunctionCall` with calls to `DrapoHandlerSyntax` (delete originals)
- [ ] T014 Add `<ProjectReference Include="..\Drapo.Tooling\Drapo.Tooling.csproj" />` to `src/WebDocs/WebDocs.csproj`
- [ ] T015 In `src/WebDocs/Startup.cs` register `services.AddSingleton<IDrapoContentRoot>(new DrapoContentRoot(Path.Combine(env.WebRootPath, "app")))` (obtain `IWebHostEnvironment` via the `Startup` constructor or `Configure` parameter as already done) and update `using`s to `Drapo.Tooling.Content` / `Drapo.Tooling.Services`
- [ ] T016 [P] Update `using` directives to `Drapo.Tooling.Models` / `Drapo.Tooling.Services` / `Drapo.Tooling.Helpers` in `src/WebDocs/Controllers/AttributeController.cs`, `src/WebDocs/Controllers/FunctionController.cs`, `src/WebDocs/Tools/AttributeTool.cs`, `src/WebDocs/Tools/FunctionTool.cs`, `src/WebDocs/Tools/ValidationTool.cs`, `src/WebDocs/Services/ConceptService.cs`, `src/WebDocs/Services/DataTypeService.cs` (and any other file `dotnet build` reports)
- [ ] T017 Run `dotnet build src/docs.sln`; fix remaining compile errors until green with no new warnings in WebDocs
- [ ] T018 Run `dotnet run --project src/WebDocs` and verify: an attribute page and a function page render; `validate_drapo`-equivalent call via `IDrapoValidatorService` on `<div d-nope="1"></div>` returns one `unknown-attribute` error at line 1 column 6 (use the Drapo MCP `validate_drapo` tool against the running app if configured, else a quick `dotnet test` from T022)
- [ ] T019 Create `src/Drapo.Tests/TestContentRoot.cs` — resolves `IDrapoContentRoot.AppPath` to `<repo>/src/WebDocs/wwwroot/app` by walking up from `AppContext.BaseDirectory` until `docs.sln` is found
- [ ] T020 [P] Create `src/Drapo.Tests/Tooling/NoAspNetReferenceTest.cs` asserting `typeof(DrapoValidatorService).Assembly.GetReferencedAssemblies()` has no name starting with `Microsoft.AspNetCore`
- [ ] T021 [P] Create `src/Drapo.Tests/Tooling/EngineCatalogTests.cs`: `Attributes ∪ AttributePrefixes` members all satisfy `IsValidAttribute`; every `Functions` member satisfies `IsValidFunction`; `d-on-click`, `D-For`, `d-validation-id`, `d-dataproperty-x-name` valid; `d-nope` invalid; `Functions` contains `updatesector`
- [ ] T022 Create parity fixtures under `src/Drapo.Tests/Fixtures/`: `unknown-attribute.html`, `unknown-function.html`, `wrong-arity.html`, `malformed-dfor.html`, `unbalanced-open.html`, `unbalanced-close.html`, `dynamic-prefixes-ok.html`, `case-insensitive-ok.html`, each with a sibling `.json` listing expected `{Level, Rule, Line, Column, Message}`; plus `src/Drapo.Tests/Tooling/ValidatorTests.cs` (theory over fixtures) and `src/Drapo.Tests/Tooling/FunctionSamplesValidateTest.cs` (every `functions/*/samples/*/content.html` → 0 errors)
- [ ] T023 Run `dotnet test src/docs.sln` — all Phase 2 tests green

**Checkpoint**: WebDocs behaves as before; the library is ASP.NET-free; the corpus and test scaffold exist.

---

## Phase 3: User Story 1 - See mistakes while editing Drapo markup (Priority: P1) 🎯 MVP

**Goal**: A stdio language server that publishes the validator's diagnostics for open documents.

**Independent Test**: `StdioSmokeTest` passes; in VS Code (via `drapo.server.path` and a throwaway
client, or after Phase 7) `<div d-nope="x">` shows an `unknown-attribute` squiggle that disappears
when fixed; `ParityTests` proves mapped diagnostics equal `validate_drapo` output.

### Implementation for User Story 1

- [ ] T024 [US1] Create `src/Drapo.LanguageServer/Documents/TextPosition.cs`: `OffsetOf(text, line, character)`, `PositionOf(text, offset)`, `IdentifierRunEnd(text, offset)` (`[A-Za-z0-9_-]+`), `LineEnd(text, offset)`
- [ ] T025 [P] [US1] Create `src/Drapo.LanguageServer/Documents/DocumentStore.cs`: thread-safe `Set(uri, text, version)`, `TryGet(uri)`, `Remove(uri)`
- [ ] T026 [US1] Create `src/Drapo.LanguageServer/Providers/DiagnosticMapper.cs`: `IReadOnlyList<Diagnostic> Map(IEnumerable<DrapoDiagnosticVM>, string text)` per contracts/lsp-capabilities.md (range end = identifier run, min 2 chars, clamp to line end; severity/code/source/message mapping)
- [ ] T027 [US1] Create `src/Drapo.LanguageServer/Handlers/TextDocumentSyncHandler.cs` (`TextDocumentSyncHandlerBase`, `TextDocumentSyncKind.Full`; on open/change/save store text and schedule validation with a 250 ms per-document debounce + `CancellationTokenSource` replacement; on close remove and publish empty diagnostics; all exceptions caught → logged to stderr)
- [ ] T028 [US1] Create `src/Drapo.LanguageServer/Program.cs`: parse `--content <dir>` (default `<AppContext.BaseDirectory>/content/app`) and `--version`; validate the content folder exists (exit 1 with stderr reason); `LanguageServer.From(o => o.WithInput(Console.OpenStandardInput()).WithOutput(Console.OpenStandardOutput()).WithServices(s => { register IDrapoContentRoot, IDrapoEngineCatalog (singleton), IFunctionService, IAttributeService, IDrapoValidatorService, DocumentStore }).WithHandler<TextDocumentSyncHandler>().OnInitialize(set serverInfo name/version))`; `await server.WaitForExit`
- [ ] T029 [P] [US1] Create `src/Drapo.Tests/LanguageServer/ParityTests.cs`: for every fixture and every function sample, `DiagnosticMapper.Map(validator.Validate(text).Diagnostics, text)` has equal count and, element-wise, `range.start == (Line-1, Column-1)`, severity ↔ Level, `code == Rule`, `message == Message`, `source == "drapo"`
- [ ] T030 [P] [US1] Create `src/Drapo.Tests/LanguageServer/RobustnessTests.cs` with an inline malformed corpus (unclosed tag, unbalanced quotes, truncated `d-on-click="Foo(`, empty string, `\0` noise, 3,000-line generated document) asserting `DiagnosticMapper` + validator never throw and the 3,000-line case completes under 500 ms
- [ ] T031 [US1] Create `src/Drapo.Tests/LanguageServer/StdioSmokeTest.cs`: start the built `Drapo.LanguageServer` executable (`--content` → repo `wwwroot/app`) with redirected stdio; send `initialize`, `initialized`, `textDocument/didOpen` (`<div d-nope="x"></div>`); read framed messages until a `textDocument/publishDiagnostics` with one `unknown-attribute` arrives (10 s timeout); send `shutdown` and `exit`; assert exit code 0 and that `initialize.result.capabilities` has `textDocumentSync`
- [ ] T032 [US1] Run `dotnet test src/docs.sln` — US1 tests green; `dotnet run --project src/Drapo.LanguageServer -- --version` prints server and engine version

**Checkpoint**: Diagnostics work end-to-end over stdio with proven parity (SC-001, SC-002, SC-006).

---

## Phase 4: User Story 2 - Complete `d-*` attribute names (Priority: P2)

**Goal**: Completion inside a tag offers exactly the engine's attributes (plus prefix families and
documented family members), with descriptions.

**Independent Test**: `CompletionProviderTests` green; in VS Code, `<div d-` + Ctrl+Space lists
`d-for`, `d-if`, `d-model`, `d-on-`…, and nothing in text content.

### Implementation for User Story 2

- [ ] T033 [US2] Create `src/Drapo.LanguageServer/DrapoSymbolIndex.cs`: built once at startup from `IDrapoEngineCatalog`, `IAttributeService.GetList()` and `IFunctionService.Get(name)` for each `GetNames()`; exposes `EngineAttributes`, `Prefixes`, `Attributes` (documented + `IsValidAttribute`, keyed lower-case), `Functions` (keyed lower-case, with parameters and signature); register as singleton in `Program.cs`
- [ ] T034 [US2] Add to `src/Drapo.LanguageServer/Documents/TextPosition.cs`: `IsInsideTag(text, offset)` (nearest `<` after nearest `>`, not inside quotes), `WordBefore(text, offset)` (`[A-Za-z0-9_-]*` run ending at offset)
- [ ] T035 [US2] Create `src/Drapo.LanguageServer/Providers/CompletionProvider.cs`: `CompletionList GetCompletions(string text, Position pos)` per contracts/lsp-capabilities.md (in-tag gate; word must be empty, `d` or start with `d-`; items = fixed engine attributes ∪ documented valid names ∪ prefixes, de-duplicated case-insensitively, `textEdit` replacing the typed word, `kind` Property/Keyword, `sortText` fixed-before-prefix, `documentation` markdown from the index)
- [ ] T036 [US2] Create `src/Drapo.LanguageServer/Handlers/CompletionHandler.cs` (`CompletionHandlerBase`, trigger character `-`, `resolveProvider=false`, delegates to the provider, exceptions → empty list) and register it in `Program.cs`
- [ ] T037 [P] [US2] Create `src/Drapo.Tests/LanguageServer/CompletionProviderTests.cs`: item labels (fixed) == `catalog.Attributes` exactly (SC-003); prefixes present; `d-on-model-change` present with documentation; `<div d-` yields items, `<div>d-` yields none, inside `d-if="d-"` yields none; `textEdit` range covers the typed `d-`
- [ ] T038 [US2] Run `dotnet test src/docs.sln` — green

**Checkpoint**: Completion shipped; US1 unaffected.

---

## Phase 5: User Story 3 - Read documentation on hover (Priority: P2)

**Goal**: Hovering an attribute or a handler function shows its documentation.

**Independent Test**: `HoverProviderTests` green; hovering `d-for` and `UpdateSector` in VS Code
shows the documented text.

### Implementation for User Story 3

- [ ] T039 [US3] Add to `src/Drapo.LanguageServer/Documents/TextPosition.cs`: `IdentifierAt(text, offset)` (run around the cursor with start/end), `TryGetEnclosingAttributeValue(text, offset, out name, out valueStart, out valueEnd)` using the validator's attribute regex `(?<=\s)(d-[A-Za-z][\w-]*)\s*=\s*(?:"([^"]*)"|'([^']*)')`
- [ ] T040 [US3] Create `src/Drapo.LanguageServer/Providers/HoverProvider.cs`: `Hover GetHover(string text, Position pos)` per contracts/lsp-capabilities.md (attribute → `**name**` + description or the "recognised, undocumented" line; function inside `d-on-*` value → signature code span, description, parameter table; else null; `range` = token)
- [ ] T041 [US3] Create `src/Drapo.LanguageServer/Handlers/HoverHandler.cs` (`HoverHandlerBase`, delegates, exceptions → null) and register in `Program.cs`
- [ ] T042 [P] [US3] Create `src/Drapo.Tests/LanguageServer/HoverProviderTests.cs`: `d-for` hover contains its description text from `AttributeService`; `UpdateSector` hover contains `UpdateSector(SectorName: text, Url: url, ...` and a table row for `Title`; hover on plain text and on `d-nope` returns null; engine-only attribute returns the "recognised" line
- [ ] T043 [US3] Run `dotnet test src/docs.sln` — green

**Checkpoint**: Hover shipped.

---

## Phase 6: User Story 4 - Signature help while typing a function call (Priority: P3)

**Goal**: Signature popup with the active parameter inside `d-on-*` calls.

**Independent Test**: `SignatureHelpProviderTests` green; typing `UpdateSector(` then `a,` in VS Code
moves the highlight to the second parameter.

### Implementation for User Story 4

- [ ] T044 [US4] Create `src/Drapo.LanguageServer/Providers/SignatureHelpProvider.cs`: `SignatureHelp GetSignatureHelp(string text, Position pos)` — inside a `d-on-*` value walk backwards tracking depth to the innermost unmatched `(`; callee = identifier before it; look up in `DrapoSymbolIndex.Functions`; `activeParameter = DrapoHandlerSyntax.SplitArguments(text between '(' and cursor).Count - 1` clamped to `[0, parameters.Count - 1]`; build `SignatureInformation` per contract; null otherwise
- [ ] T045 [US4] Create `src/Drapo.LanguageServer/Handlers/SignatureHelpHandler.cs` (`SignatureHelpHandlerBase`, trigger `(` `,`, retrigger `,`, exceptions → null) and register in `Program.cs`
- [ ] T046 [P] [US4] Create `src/Drapo.Tests/LanguageServer/SignatureHelpProviderTests.cs`: `UpdateSector(` → active 0; `UpdateSector(a, ` → active 1; `UpdateSector(a, Foo(b, c), ` → active 2 (nested ignored); `Unknown(` → null; cursor outside a `d-on-*` value → null; mustache `{{a,b}}` inside an argument does not advance the parameter
- [ ] T047 [US4] Run `dotnet test src/docs.sln` — green; the `StdioSmokeTest` assertion on capabilities now also checks `completionProvider`, `hoverProvider`, `signatureHelpProvider`

**Checkpoint**: All language features shipped and tested.

---

## Phase 7: User Story 5 - Install once, no separate runtime setup (Priority: P3)

**Goal**: A VS Code extension that finds and starts the bundled self-contained server.

**Independent Test**: F5 in `src/vscode-drapo` with `drapo.server.path` pointed at the Debug build
shows diagnostics; a packaged `win32-x64` VSIX installs on a machine without .NET and shows a
diagnostic (quickstart §5–6); a bad `drapo.server.path` produces an error notification.

### Implementation for User Story 5

- [ ] T048 [P] [US5] Create `src/Drapo.LanguageServer/publish.ps1`: for `win-x64`, `linux-x64`, `osx-x64`, `osx-arm64` run `dotnet publish -c Release -r <rid> --self-contained -p:PublishSingleFile=false -p:PublishTrimmed=false -o bin/publish/<rid>` (parameter `-Rid` to publish a single target)
- [ ] T049 [P] [US5] Create `src/vscode-drapo/package.json` per contracts/vscode-extension.md (name `vscode-drapo`, publisher `spadrapo`, `engines.vscode ^1.90.0`, activation events, `main ./out/extension.js`, configuration `drapo.server.path` / `drapo.trace.server`, command `drapo.restartServer`, scripts `compile`/`watch`/`package`, deps `vscode-languageclient ^10`, devDeps `@types/vscode`, `@types/node`, `typescript ^5`, `esbuild`, `@vscode/vsce ^3`)
- [ ] T050 [P] [US5] Create `src/vscode-drapo/tsconfig.json` (target ES2022, module commonjs, strict, `rootDir src`, `outDir out`), `src/vscode-drapo/.vscodeignore` (exclude `src/**`, `node_modules/**`, `**/*.map`, `scripts/**`), `src/vscode-drapo/.gitignore` (`node_modules/`, `out/`, `server/`, `*.vsix`)
- [ ] T051 [US5] Create `src/vscode-drapo/src/extension.ts`: resolve server path (setting → bundled `server/<rid>/Drapo.LanguageServer[.exe]` by `process.platform`/`process.arch` → error notification and return); `chmod 755` on non-Windows; `LanguageClient` with `documentSelector` for `html`/`razor`/`aspnetcorerazor` on `file`+`untitled`, transport stdio, `outputChannelName "Drapo Language Server"`; `drapo.restartServer` command; `client.start()` rejection → `showErrorMessage`; `deactivate` stops the client
- [ ] T052 [P] [US5] Create `src/vscode-drapo/scripts/copy-server.ps1 -Rid <rid>`: clear `server/*`, copy `../Drapo.LanguageServer/bin/publish/<rid>/` into `server/<rid>/`
- [ ] T053 [P] [US5] Create `src/vscode-drapo/README.md` (what it does, supported languages, settings, development via F5 + `drapo.server.path`, packaging matrix) and `src/vscode-drapo/CHANGELOG.md` (0.1.0)
- [ ] T054 [US5] Run `npm install` then `npm run compile` in `src/vscode-drapo`; commit the generated `package-lock.json`; launch the Extension Development Host and execute quickstart §5 rows 1–8 and 10–11, recording results in `specs/003-drapo-language-server/quickstart.md` (append a "Verified on <date>" note)
- [ ] T055 [US5] Run `publish.ps1 -Rid win-x64`, `copy-server.ps1 -Rid win-x64`, `npm run package -- --target win32-x64`; install the VSIX with `code --install-extension` and confirm a diagnostic appears on an HTML file with `d-nope` without any `drapo.server.path` setting

**Checkpoint**: Extension installs and works from a package (SC-004, FR-013–015).

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: CI, documentation and repo guidance updates.

- [ ] T056 [P] Create `.github/workflows/ci.yml`: on `pull_request` and `push` (all branches) run `actions/setup-dotnet` 8.0.x, `dotnet build src/docs.sln -c Release`, `dotnet test src/docs.sln -c Release --no-build`; on tags `lsp-v*` add a matrix job (`win-x64/win32-x64`, `linux-x64/linux-x64`, `osx-x64/darwin-x64`, `osx-arm64/darwin-arm64`) that runs `publish.ps1 -Rid`, `npm ci`, `copy-server.ps1 -Rid`, `npm run package -- --target` and uploads `*.vsix` via `actions/upload-artifact`
- [ ] T057 [P] Update `CLAUDE.md` Layout section to add `src/Drapo.Tooling/`, `src/Drapo.LanguageServer/`, `src/Drapo.Tests/`, `src/vscode-drapo/` with one-line descriptions; update the Services line to say catalog/validator/function/attribute services now live in `Drapo.Tooling`; add `dotnet test src/docs.sln` to Build & run
- [ ] T058 [P] Mirror the same Layout/Build changes in `.github/copilot-instructions.md`
- [ ] T059 [P] Add a "Editor support (VS Code)" subsection to the repository `README.md` pointing at `src/vscode-drapo/README.md`
- [ ] T060 Run the full quickstart (§1–§4 automated parts) one final time: `dotnet build src/docs.sln`, `dotnet test src/docs.sln`, `dotnet run --project src/Drapo.LanguageServer -- --version`, WebDocs smoke via `dotnet run --project src/WebDocs`; record the outcome in `specs/003-drapo-language-server/quickstart.md`
- [ ] T061 Open a PR from `003-drapo-language-server` to `master` referencing `Closes #385`, summarising the three deliverables and the parity guarantee

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies; T002/T003/T005 parallel after T001.
- **Foundational (Phase 2)**: depends on Phase 1; **blocks all stories** (the server and tests
  reference the library).
- **US1 (Phase 3)**: depends on Phase 2. This is the MVP.
- **US2 (Phase 4)**: depends on Phase 3 for `Program.cs` registration and `TextPosition.cs`; the
  provider and its tests are otherwise independent.
- **US3 (Phase 5)**: depends on T033 (`DrapoSymbolIndex`) from US2 and on `TextPosition.cs`.
- **US4 (Phase 6)**: depends on T033 and T039.
- **US5 (Phase 7)**: depends only on Phase 3 (a server that publishes diagnostics); can run in
  parallel with Phases 4–6.
- **Polish (Phase 8)**: after the stories you intend to ship.

### Within Phase 2 (ordered)

T006 → {T007, T008} → T009 → T010 → T011 → T012 → T013 → T014 → T015 → T016 → T017 → T018 →
T019 → {T020, T021, T022} → T023.

### Parallel Opportunities

- Phase 1: T002, T003, T005 in parallel after T001.
- Phase 2: T007 ∥ T008; T020 ∥ T021 ∥ T022.
- Phase 3: T025 ∥ T024; T029 ∥ T030 once T026 exists.
- Phases 4–6 provider tests (T037, T042, T046) can be written in parallel with their providers.
- Phase 7: T048, T049, T050, T052, T053 all parallel; T051 after T049/T050.
- Phase 8: T056–T059 parallel.

---

## Parallel Example: User Story 1

```bash
# After T026 (DiagnosticMapper) exists:
Task: "Create src/Drapo.Tests/LanguageServer/ParityTests.cs"
Task: "Create src/Drapo.Tests/LanguageServer/RobustnessTests.cs"
Task: "Create src/Drapo.LanguageServer/Documents/DocumentStore.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1 + Phase 2 (library extraction — the riskiest step; validate WebDocs unchanged at T018).
2. Phase 3 → a working stdio server with proven parity. Demo with any LSP client.
3. Phase 7 next if a demo in VS Code is wanted before the remaining features.

### Incremental Delivery

Each of Phases 4, 5, 6 adds one capability behind one handler and one provider with its own tests;
none changes the diagnostics path, so US1 stays green throughout.

### Notes

- Never extend `DrapoDiagnosticVM`; ranges are computed in `DiagnosticMapper` (FR-003).
- The validator's regexes are reused, not re-implemented, in `TextPosition` (Principle I).
- Commit after each phase checkpoint; keep the extraction (Phase 2) as its own commit so the
  "WebDocs unchanged" diff is reviewable on its own.
