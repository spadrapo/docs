# Implementation Plan: LSP Improvements

**Branch**: `004-lsp-improvements` | **Date**: 2026-09-09 | **Spec**: [spec.md](spec.md) | **Issue**: #389

**Input**: Feature specification from `/specs/004-lsp-improvements/spec.md`

## Summary

Three improvements to the Drapo editor tooling shipped in feature 003: (1) correct the documented
required-parameter counts so the validator, the docs site, the MCP and the editors never flag a call
the engine runs (`ShowWindow(name)` first, then every function, audited against `drapo.js`);
(2) a Visual Studio 2022/2026 extension hosting the same `Drapo.LanguageServer` through the
built-in `ILanguageClient`, built in the same CI/release workflow and attached to the same GitHub
Release; (3) syntax highlighting through a TextMate injection grammar in the VS Code extension plus
LSP semantic tokens from the server for both editors. Details and evidence: [research.md](research.md),
[arity-audit.md](arity-audit.md).

## Technical Context

**Language/Version**: C# / .NET 8 (server, tooling, tests); C# / .NET Framework 4.7.2 (Visual Studio
extension, required by the VS host); TypeScript 5 / Node 20 (VS Code extension); JSON (TextMate grammar)

**Primary Dependencies**: `OmniSharp.Extensions.LanguageServer` 0.19.9 (unchanged);
`Microsoft.VisualStudio.SDK` 17.14.40265 + `Microsoft.VSSDK.BuildTools` 18.5.40034 (new, VS
extension only); `vscode-textmate` + `vscode-oniguruma` (new dev-only, grammar test)

**Storage**: N/A (content files under `src/WebDocs/wwwroot/app/functions/*/parameters.json`)

**Testing**: xunit (`dotnet test src/docs.sln`), VS Code integration test (`npm test`), grammar
tokenizer test (`npm run test:grammar`), Visual Studio VSIX build in CI + manual smoke test

**Target Platform**: VS Code (win/linux/mac, unchanged); Visual Studio 2022 17.x and 2026 18.x, x64
Windows; server published self-contained win-x64 for the VS package

**Project Type**: multi-project tooling (class library + LSP server + two editor extensions + docs app)

**Performance Goals**: semantic tokens for a 5,000-line document under 1 s (test-enforced; measured
~10 ms); no perceptible typing lag (SC-007)

**Constraints**: one validator (docs site = MCP = editors); no change to `parameters.json` schema;
Visual Studio HTML only; no Marketplace publishing; VS extension cannot be integration-tested headlessly

**Scale/Scope**: 96 documented functions; 5 release packages per tag

## Constitution Check

*GATE: passed before research; re-checked after design.*

| Principle | Status | Notes |
|---|---|---|
| I. Accuracy over the real engine | ✅ | Every arity change cites the engine line (arity-audit.md). Token known/unknown comes from `DrapoEngineCatalog`. |
| II. Content is structured data | ✅ | Only `parameters.json` / `description.html` / `samples/NNN` edited, within the existing schema. New sample `ShowWindow/samples/002`. |
| III. Every example is valid Drapo | ✅ | New sample validates; the docs app registers the `center` window definition so it runs. New guard test: no documented sample yields a `wrong-arity` warning. |
| IV. Docs ↔ tooling parity | ✅ | No schema change. The validator's conditional rules are documented in the parameter descriptions they complement. |
| V. Simplicity & dogfooding | ✅ | New deps only where unavoidable (VS SDK for a VS extension; textmate libs for testing a grammar). Server legend uses standard LSP token types to work in every client without custom mapping. |

## Project Structure

### Documentation (this feature)

```text
specs/004-lsp-improvements/
├── spec.md
├── plan.md              # this file
├── research.md          # decisions + evidence
├── arity-audit.md       # per-function audit table (96 functions)
├── quickstart.md        # how to verify each story locally
├── checklists/requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── Drapo.Tooling/Services/DrapoValidatorService.cs     # CheckConditionalArity (ShowWindow url, CreateGuid 1-arg)
├── Drapo.LanguageServer/
│   ├── Providers/SemanticTokensProvider.cs             # DrapoToken, DrapoSemanticTokens legend, scanner
│   ├── Handlers/SemanticTokensHandler.cs               # textDocument/semanticTokens full + range
│   ├── Handlers/TextDocumentSyncHandler.cs             # language id normalisation for non-VS Code clients
│   └── Program.cs                                      # registers the handler
├── Drapo.VisualStudio/                                  # NEW: VS 2022/2026 extension (VSIX)
│   ├── Drapo.VisualStudio.csproj                       # SDK-style, net472, bundles server/** into the VSIX
│   ├── source.extension.vsixmanifest
│   ├── DrapoLanguageClient.cs                          # ILanguageClient for content type HTML
│   ├── HtmlContentType.cs                              # adds languageserver-base to HTML
│   ├── build.ps1                                       # publish server + msbuild VSIX (local + CI)
│   └── README.md
├── Drapo.Tests/
│   ├── Fixtures/{showwindow-*,arity-audit-*}.html|json # corpus additions
│   ├── LanguageServer/SemanticTokensProviderTests.cs
│   └── Tooling/ValidatorTests.cs                       # FunctionSamplesArityTest guard
├── vscode-drapo/
│   ├── syntaxes/drapo.injection.json                   # TextMate injection grammar
│   ├── package.json                                    # grammars, semanticTokenModifiers/Scopes
│   ├── test/grammar/                                   # tokenizer test over the real HTML grammar
│   └── test/suite/index.ts                             # + semantic tokens assertion
├── WebDocs/Startup.cs                                   # CreateWindow("center", ...) for the sample
├── WebDocs/wwwroot/app/functions/{ShowWindow,AddDate,...}/parameters.json
├── WebDocs/wwwroot/app/menu/0001 - Guide/0019 - Editor Extensions.html   # renamed from "VS Code Extension"
└── docs.sln                                             # VS extension is NOT in the solution (needs VS SDK/MSBuild)
.github/workflows/ci.yml                                 # + vs-extension job (windows-latest), release attaches it
```

**Structure Decision**: the Visual Studio extension is a sibling project under `src/` but is
deliberately **not** added to `docs.sln`: it targets net472 and needs the VSSDK build tools, which
would break `dotnet build src/docs.sln` on Linux (the CI build/test job and the Docker image). It
is built by its own `build.ps1` and its own CI job.

## Design notes

### Arity (US1)
- `parameters.json` stays the single source of truth; `Optional` flips per research R2.
- `DrapoValidatorService.CheckConditionalArity` holds the two value-dependent rules with the engine
  evidence in comments. A rule only fires on literal text (mustaches/nested calls are undecidable).
- Guard tests: fixtures `showwindow-definition-ok`, `showwindow-url-without-did`, `arity-audit-ok`,
  `arity-audit-warn`; `FunctionSamplesArityTest` asserts no documented sample yields `wrong-arity`.

### Visual Studio (US2)
- `DrapoLanguageClient : ILanguageClient`, `[ContentType("HTML")]`, starts
  `server\Drapo.LanguageServer.exe` (self-contained win-x64, content under `server\content\app`).
- `HtmlContentType` exports `[Name("HTML")][BaseDefinition("languageserver-base")]` (see R3).
- The VS client sends `languageId` derived from the content type; `TextDocumentSyncHandler` normalises
  unknown ids by file extension so OmniSharp routes requests to the handlers.
- CI: `vs-extension` job on `windows-latest`: publish server win-x64 → `msbuild -restore` with
  `-p:VsixVersion` → upload artifact; `release` job downloads `vscode-drapo-*` and `vs-drapo-*`.

### Highlighting (US3)
- Grammar and semantic legend per research R5; both layers share scope names via `semanticTokenScopes`.
- `vscode-languageclient` 10 negotiates semantic tokens automatically; nothing to add in `extension.ts`.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| New project `Drapo.VisualStudio` outside `docs.sln` | VS extensions must be net472 + VSSDK, built by MSBuild on Windows | Adding it to the solution breaks the Linux `dotnet build`/Docker path used by CI and the site |
| Conditional arity rules in code (not in `parameters.json`) | Two functions need a value-dependent minimum | A schema extension would ripple through site, MCP, hover and signature help for two cases |
