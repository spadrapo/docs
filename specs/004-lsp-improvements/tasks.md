# Tasks: LSP Improvements

**Input**: Design documents from `/specs/004-lsp-improvements/` · **Issue**: #389

**Tests**: Included (the spec's FR-006, FR-023 and SC-001/SC-006 require automated guards).

**Organization**: Phase 1 is the engine audit (input to US1). Phases 2–4 map to the three user
stories in priority order; each is independently testable. Phase 5 is docs/CI polish.

## Format: `[ID] [P?] [Story] Description`

---

## Phase 1: Research (blocking for US1)

- [X] T001 Extract `drapo.js` from the `Drapo` 2026.9.9.9 package and audit all 96 `ExecuteFunction*` bodies against `parameters.json`; write `specs/004-lsp-improvements/arity-audit.md`
- [X] T002 [P] Verify Visual Studio LSP client facts (packages, content type `HTML`, `languageserver-base` bridge, semantic tokens support, SDK-style VSIX build) on VS 2022 17.14 and VS 2026 18.7; record in `research.md` R3

## Phase 2: US1 — No false arity warnings

- [X] T003 [US1] `ShowWindow`: `did` → optional, descriptions rewritten, `samples/002` (window definition form) in `src/WebDocs/wwwroot/app/functions/ShowWindow/`
- [X] T004 [US1] Register the `center` window definition in `src/WebDocs/Startup.cs` (`options.Config.CreateWindow`) so the new sample runs on the site
- [X] T005 [US1] `DrapoValidatorService.CheckConditionalArity`: ShowWindow literal-url rule; CreateGuid exactly-one-argument rule (`src/Drapo.Tooling/Services/DrapoValidatorService.cs`)
- [X] T006 [US1] Apply audit corrections: AddDate, HasDataChanges, AcceptDataChanges, CreateData, CreateGuid, GetSector (too high); FilterData, ExecuteInstanceFunction (too low) — `parameters.json` of each
- [X] T007 [US1] Fixtures `showwindow-definition-ok`, `showwindow-url-without-did`, `arity-audit-ok`, `arity-audit-warn` (`src/Drapo.Tests/Fixtures/`)
- [X] T008 [US1] Guard test `FunctionSamplesArityTest` (no documented sample yields `wrong-arity`) in `src/Drapo.Tests/Tooling/ValidatorTests.cs`

## Phase 3: US3 — Syntax highlighting (done before US2 because US2's smoke test exercises it)

- [X] T009 [US3] TextMate injection grammar `src/vscode-drapo/syntaxes/drapo.injection.json`
- [X] T010 [US3] `package.json`: `grammars`, `semanticTokenModifiers` (`unknown`), `semanticTokenScopes` mappings
- [X] T011 [US3] `SemanticTokensProvider` + `DrapoSemanticTokens` legend (`src/Drapo.LanguageServer/Providers/SemanticTokensProvider.cs`)
- [X] T012 [US3] `SemanticTokensHandler` (full + range) and registration in `Program.cs`
- [X] T013 [US3] `SemanticTokensProviderTests` (classification, validator agreement, prose exclusion, multi-line split, overlap, legend, 5,000-line timing)
- [X] T014 [US3] Grammar tokenizer test `src/vscode-drapo/test/grammar/` (`vscode-textmate` + `vscode-oniguruma` over the downloaded VS Code's `html.tmLanguage.json`), `npm run test:grammar`
- [X] T015 [US3] VS Code integration test: assert semantic tokens via `vscode.provideDocumentSemanticTokens` / legend on the fixture (`src/vscode-drapo/test/suite/index.ts`)

## Phase 4: US2 — Visual Studio extension and release

- [X] T016 [US2] `TextDocumentSyncHandler`: normalise unknown `languageId` by extension (`.html/.htm` → `html`) so non-VS Code clients route to the handlers
- [X] T017 [US2] Project `src/Drapo.VisualStudio/Drapo.VisualStudio.csproj` (SDK-style, net472, VSSDK BuildTools, bundles `server\**`)
- [X] T018 [US2] `source.extension.vsixmanifest` (Identity `Drapo.VisualStudio`, `[17.0,)` amd64, MefComponent asset, version placeholder)
- [X] T019 [US2] `DrapoLanguageClient.cs` (`ILanguageClient`, content type `HTML`, starts bundled server) + `HtmlContentType.cs` (`languageserver-base` bridge)
- [X] T020 [US2] `build.ps1`: publish server win-x64 → copy into `server/` → `msbuild -restore` with `-p:VsixVersion`; `README.md`
- [X] T021 [US2] Local smoke test on VS 2022 and VS 2026: install VSIX, open `.html` with `d-nope`, verify diagnostic, completion, hover, colours; record result in `quickstart.md`
- [X] T022 [US2] `.github/workflows/ci.yml`: `vs-extension` job (windows-latest; build always, package on `lsp-v*`), `release` job downloads both patterns and lists the VS package in the notes
- [X] T023 [US2] `.gitignore`: `src/Drapo.VisualStudio/server/`, `bin/`, `obj/`

## Phase 5: Docs & polish

- [X] T024 [P] Rename/extend `src/WebDocs/wwwroot/app/menu/0001 - Guide/0019 - VS Code Extension.html` → `0019 - Editor Extensions.html` (Visual Studio install, highlighting, updated release notes table)
- [X] T025 [P] `src/vscode-drapo/README.md` + `CHANGELOG.md` (highlighting, semantic tokens, VS package); `CLAUDE.md` layout entry for `src/Drapo.VisualStudio/`
- [X] T026 `quickstart.md`; run `dotnet test src/docs.sln`, `npm test`, `npm run test:grammar`, `build.ps1`; open PR closing #389
