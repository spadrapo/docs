# Implementation Plan: Drapo Language Server

**Branch**: `003-drapo-language-server` | **Date**: 2026-09-08 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/003-drapo-language-server/spec.md` and the technical
direction in [spadrapo/docs#385](https://github.com/spadrapo/docs/issues/385).

## Summary

Expose the knowledge the docs app already holds (engine attribute/function sets, documented function
signatures, the template validator) to code editors through the Language Server Protocol, without
forking that knowledge. Three deliverables:

1. **`src/Drapo.Tooling`** — a .NET 8 class library with *no* ASP.NET dependency, created by
   moving `DrapoEngineCatalog`, `DrapoValidatorService`, `FunctionService`, `AttributeService`, their
   interfaces, their `*VM` models and the `DrapoDocContent` helper out of `WebDocs`. The only new
   abstraction is `IDrapoContentRoot` (where the `app/` content folder lives), which replaces the
   direct `IWebHostEnvironment.WebRootPath` dependency. `WebDocs` references the library; its
   controllers and MCP tools keep their public behaviour and JSON shapes byte-for-byte.
2. **`src/Drapo.LanguageServer`** — a stdio console app built on `OmniSharp.Extensions.LanguageServer`
   that maps the validator to `textDocument/publishDiagnostics` and adds completion, hover and
   signature help on top of the same catalog. Documentation content is copied next to the binary at
   build time so it runs offline.
3. **`src/vscode-drapo`** — a minimal TypeScript VS Code extension (`vscode-languageclient`) that
   launches a self-contained, per-platform published server for `html`, `razor` and
   `aspnetcorerazor` documents.

Parity is enforced by construction (one validator) and verified by an xunit test project that runs a
corpus (every documented function sample plus hand-written bad snippets) through the validator and
through the server's diagnostic mapping.

## Technical Context

**Language/Version**: C# on .NET 8 (`net8.0`; `global.json` pins SDK 8.0.x, `rollForward:
latestFeature`); TypeScript 5.x for the VS Code extension (Node 22 for tooling only).

**Primary Dependencies**:
- Existing: `Drapo` 2026.8.6.5 (engine assembly, embedded `drapo.js`), `Newtonsoft.Json` (via
  `Microsoft.AspNetCore.Mvc.NewtonsoftJson` today; referenced directly by the library).
- New (justified in research.md): `OmniSharp.Extensions.LanguageServer` 0.19.9 (server);
  `xunit` 2.9.x + `Microsoft.NET.Test.Sdk` (tests); `vscode-languageclient` 10.x and
  `@vscode/vsce` 3.x (extension, dev-time).

**Storage**: file system only. Documentation content under `src/WebDocs/wwwroot/app/` is the source;
the server ships a copy under `<server>/content/app/` produced at build/publish time.

**Testing**: `dotnet test` on `src/Drapo.Tests` (xunit): validator parity corpus, catalog sanity,
provider unit tests, one stdio process smoke test, and a "no ASP.NET reference" assertion on the
tooling assembly. Manual: `quickstart.md` VS Code scenarios.

**Target Platform**: server published self-contained for `win-x64`, `linux-x64`, `osx-x64`,
`osx-arm64`; extension packaged per target with `vsce package --target`. WebDocs unchanged
(Linux container, .NET 8).

**Project Type**: multi-project .NET solution (library + console + tests) plus a small Node/TS
extension package.

**Performance Goals**: diagnostics for a 2,000-line document delivered within 500 ms of the last
edit (SC-002); server start-to-`initialize` response under 2 s cold (SC-004 headroom).

**Constraints**: offline-capable (FR-004); identical diagnostics to the MCP tool (FR-002); WebDocs
output unchanged (FR-003); never crash on malformed input (FR-016); no new dependency on WebDocs
beyond the library reference (Principle V).

**Scale/Scope**: ~98 documented attributes, ~95 documented functions, engine sets of similar size;
single-user editor process; documents up to a few thousand lines.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Gate | Pre-design | Post-design |
|-----------|------|-----------|-------------|
| I. Accuracy over the real engine | All editor knowledge comes from `DrapoEngineCatalog` (engine) and `parameters.json` (docs); no hand-maintained lists. | PASS — library is a move, not a rewrite; completion enumerates the catalog. | PASS — `IDrapoEngineCatalog` gains read-only enumerations of the same parsed sets; nothing is typed in by hand. |
| II. Content is structured data | Content conventions and locations unchanged; consumers still discover by path. | PASS — services keep reading `app/functions/<Name>/…` and `app/menu/0003 - Attributes/…`; only the root path becomes injectable. | PASS — the server's copied content keeps the identical relative layout, so the same services read it. |
| III. Every example is valid Drapo | No new doc content is added. Parity corpus *uses* existing samples as fixtures. | PASS | PASS — the parity test doubles as a regression check that all function samples still validate. |
| IV. Docs ↔ tooling parity | Content-shape changes must update Service/VM/Controller/MCP together. | PASS — no content-shape change. The move keeps `*VM` property names, so MCP JSON is unchanged. | PASS — `DrapoDiagnosticVM` is NOT extended (diagnostic ranges are computed in the server), so `validate_drapo` output is byte-identical. |
| V. Simplicity & dogfooding | New deps need a stated reason; .NET 8 / Less-Cake / Docker build preserved. | PASS with justification (see Complexity Tracking): one LSP framework package, one test framework, one npm client package. | PASS — Dockerfile only gains the extra `csproj` in the restore step; build commands unchanged. |

**Gate result**: PASS (no unjustified violations). Justified additions are listed under Complexity
Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/003-drapo-language-server/
├── plan.md              # This file
├── research.md          # Phase 0: decisions and alternatives
├── data-model.md        # Phase 1: entities shared by MCP and LSP
├── quickstart.md        # Phase 1: how to build, run and verify end-to-end
├── contracts/
│   ├── lsp-capabilities.md      # what the server advertises and how each request is answered
│   ├── tooling-library-api.md   # public surface of Drapo.Tooling consumed by WebDocs and the server
│   └── vscode-extension.md      # extension manifest contract (activation, settings, packaging)
└── tasks.md             # Phase 2 output (/speckit-tasks)
```

### Source Code (repository root)

```text
src/
├── docs.sln                          # + Drapo.Tooling, Drapo.LanguageServer, Drapo.Tests
├── Dockerfile                        # restore step also copies Drapo.Tooling/Drapo.Tooling.csproj
├── WebDocs/                          # existing app; now references Drapo.Tooling
│   ├── Startup.cs                    # DI: AddSingleton<IDrapoContentRoot>(wwwroot) + existing registrations
│   ├── Controllers/                  # unchanged behaviour; `using` updated to Drapo.Tooling.*
│   ├── Tools/                        # MCP tools unchanged behaviour; `using` updated
│   └── Services/                     # ConceptService, DataTypeService, NuGetService stay here
├── Drapo.Tooling/                    # NEW class library (net8.0, no ASP.NET)
│   ├── Drapo.Tooling.csproj          # refs: Drapo, Newtonsoft.Json
│   ├── Content/
│   │   ├── IDrapoContentRoot.cs      # string AppPath  (…/app)
│   │   └── DrapoContentRoot.cs       # simple path-based implementation
│   ├── Models/                       # moved: FunctionVM, FunctionParameterVM, FunctionSampleVM,
│   │                                 #        AttributeVM, DrapoDiagnosticVM, DrapoValidationResultVM
│   ├── Services/                     # moved: I/DrapoEngineCatalog, I/FunctionService,
│   │                                 #        I/AttributeService, I/DrapoValidatorService
│   └── Helpers/                      # moved: DrapoDocContent; NEW: DrapoHandlerSyntax
│                                     #        (ScanFunctionCalls / SplitArguments shared with LSP)
├── Drapo.LanguageServer/             # NEW console app (net8.0, stdio)
│   ├── Drapo.LanguageServer.csproj   # refs: Drapo.Tooling, OmniSharp.Extensions.LanguageServer;
│   │                                 # copies ../WebDocs/wwwroot/app/{functions,menu/0003 - Attributes}
│   │                                 # to $(OutDir)/content/app/… on build and publish
│   ├── Program.cs                    # LanguageServer.From(stdio) + DI + handler registration
│   ├── DrapoSymbolIndex.cs           # startup snapshot: attributes (+desc), functions (+params)
│   ├── Documents/
│   │   ├── DocumentStore.cs          # uri -> text (full sync)
│   │   └── TextPosition.cs           # offset <-> (line, character) helpers, token-at-position
│   ├── Providers/                    # pure, testable logic (no protocol types leak in)
│   │   ├── DiagnosticMapper.cs       # DrapoDiagnosticVM -> LSP Diagnostic (range by token scan)
│   │   ├── CompletionProvider.cs
│   │   ├── HoverProvider.cs
│   │   └── SignatureHelpProvider.cs
│   ├── Handlers/                     # thin OmniSharp handlers delegating to Providers
│   │   ├── TextDocumentSyncHandler.cs
│   │   ├── CompletionHandler.cs
│   │   ├── HoverHandler.cs
│   │   └── SignatureHelpHandler.cs
│   └── publish.ps1                   # dotnet publish -r <rid> --self-contained for each RID
├── Drapo.Tests/                      # NEW xunit project
│   ├── Drapo.Tests.csproj            # refs: Drapo.Tooling, Drapo.LanguageServer
│   ├── Fixtures/                     # bad-snippet corpus (*.html) + expected diagnostics (*.json)
│   ├── Tooling/                      # EngineCatalogTests, ValidatorTests, NoAspNetReferenceTest
│   ├── LanguageServer/               # ParityTests, CompletionProviderTests, HoverProviderTests,
│   │                                 # SignatureHelpProviderTests, StdioSmokeTest
│   └── TestContentRoot.cs            # points IDrapoContentRoot at ../../WebDocs/wwwroot/app
└── vscode-drapo/                     # NEW VS Code extension
    ├── package.json                  # activation: onLanguage:html|razor|aspnetcorerazor
    ├── tsconfig.json
    ├── src/extension.ts              # locate server (bundled RID dir or drapo.server.path), start client
    ├── scripts/copy-server.ps1       # copies published server into server/<rid>/ before packaging
    ├── .vscodeignore
    └── README.md

.github/workflows/
├── docker-image.yml                  # unchanged
└── ci.yml                            # NEW: dotnet build + dotnet test on PRs; on tag: publish server
                                      #      per RID + vsce package --target, upload .vsix artifacts
```

**Structure Decision**: everything lives under `src/` beside the existing solution so `docs.sln`,
the Dockerfile context (`./src`) and the documented `dotnet` commands keep working. The library is a
*move* with namespace rename (`WebDocs.*` → `Drapo.Tooling.*`), which is the smallest change that
removes the ASP.NET dependency; WebDocs-only services (`ConceptService`, `DataTypeService`,
`NuGetService`, `MenuController`) stay in WebDocs and take the content root from the same
`IDrapoContentRoot` only where they already used `WebRootPath`.

## Phase 0 — Research

See [research.md](research.md). All Technical Context items are resolved; no NEEDS CLARIFICATION
remain. Key decisions: OmniSharp LSP framework; content copied at build (not embedded resources);
diagnostic ranges computed server-side so `DrapoDiagnosticVM` stays unchanged; full document sync
with a short debounce; self-contained per-RID publish bundled into platform-specific VSIX files.

## Phase 1 — Design

- [data-model.md](data-model.md): shared entities and how each maps to MCP JSON and LSP types.
- [contracts/tooling-library-api.md](contracts/tooling-library-api.md): the public surface of
  `Drapo.Tooling` (what moves, what is added, what WebDocs must change).
- [contracts/lsp-capabilities.md](contracts/lsp-capabilities.md): server capabilities and per-request
  behaviour, including the in-tag / in-handler detection rules.
- [contracts/vscode-extension.md](contracts/vscode-extension.md): manifest, settings, server lookup
  order, packaging matrix.
- [quickstart.md](quickstart.md): build, run, and the manual + automated checks that prove SC-001…006.

Agent context: the managed block in `CLAUDE.md` is updated to point to this plan (Phase 1 step 4 /
`after_plan` hook).

## Phase 2 — Task generation approach

`/speckit-tasks` should produce dependency-ordered tasks in this order, each independently buildable:

1. **Extract library** (US1 prerequisite, FR-001/003/004): create `Drapo.Tooling`, move files,
   add `IDrapoContentRoot`, rewire WebDocs DI/usings, add to sln + Dockerfile, `dotnet build`, run the
   site and MCP smoke check. *Checkpoint: WebDocs behaviour unchanged.*
2. **Tests scaffold + parity corpus** (SC-001/006): `Drapo.Tests` with validator tests running
   against real content; no-ASP.NET assertion.
3. **Language server: diagnostics** (US1): project, stdio host, document sync, `DiagnosticMapper`,
   parity tests through the mapper, stdio smoke test.
4. **Completion** (US2), **Hover** (US3), **Signature help** (US4): provider + handler + tests each.
5. **VS Code extension** (US5): manifest, client, server lookup, `publish.ps1`, packaging script,
   README; manual quickstart run.
6. **CI + docs**: `ci.yml`, CLAUDE.md layout section, README mention.

## Complexity Tracking

| Addition | Why Needed | Simpler Alternative Rejected Because |
|----------|------------|-------------------------------------|
| `OmniSharp.Extensions.LanguageServer` package | Implements JSON-RPC framing, LSP message types, capability negotiation and handler routing. | Hand-rolling JSON-RPC + LSP types is several thousand lines of protocol code with no product value; `Microsoft.CommonLanguageServerProtocol.Framework` is less documented and still needs a JSON-RPC layer. |
| Second/third .NET project (`Drapo.LanguageServer`, `Drapo.Tests`) | The server must run without ASP.NET or the website; tests are required to prove parity (SC-001) and robustness (SC-006). | Putting the server inside WebDocs would drag ASP.NET into the editor binary and break FR-004; the repo has no test project today, so one must be added. |
| npm dev dependencies (`vscode-languageclient`, `@vscode/vsce`, TypeScript) | A VS Code extension cannot exist without the client library and packager. | No alternative; kept to the minimal set with no runtime deps beyond the client library. |
| Content copy at build into the server output | Server must work offline (FR-004) and reuse the unchanged file-based services (Principle II). | Embedding as assembly resources would require rewriting `FunctionService`/`AttributeService` to read streams instead of paths, i.e. a second code path to keep in parity. |
