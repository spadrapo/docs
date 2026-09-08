# Research: Drapo Language Server

**Feature**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Date**: 2026-09-08

Facts below were checked against the repository at branch creation (commit `cc892d8`) and against
the public package registries on 2026-09-08.

## R1. LSP framework for .NET

- **Decision**: `OmniSharp.Extensions.LanguageServer` 0.19.9 (latest stable on nuget.org).
- **Rationale**: mature (used by csharp-ls, PowerShell Editor Services, and many others), targets
  .NET Standard 2.0/.NET 6+, provides stdio hosting (`LanguageServer.From(o => o.WithInput(stdin)
  .WithOutput(stdout))`), typed handler base classes for every request used here
  (`TextDocumentSyncHandlerBase`, `CompletionHandlerBase`, `HoverHandlerBase`,
  `SignatureHelpHandlerBase`) and `Microsoft.Extensions.DependencyInjection` integration, so the
  existing service registrations can be reused verbatim.
- **Alternatives considered**:
  - `Microsoft.CommonLanguageServerProtocol.Framework`: used inside Roslyn but sparsely documented and
    still requires a JSON-RPC transport to be wired by hand.
  - `StreamJsonRpc` + hand-written LSP DTOs: too much protocol code for no product gain
    (Principle V).
  - TypeScript server reading a JSON catalog exported by the docs: would duplicate the validator in a
    second language and break "one validator" parity by construction (FR-002).

## R2. Where documentation content lives for the offline server

- **Decision**: copy the two content folders the library reads (`app/functions/**` and
  `app/menu/0003 - Attributes/*.html`) into `<server output>/content/app/…` via MSBuild
  `<Content Include="..." Link="content/app/..." CopyToOutputDirectory="PreserveNewest" />` in
  `Drapo.LanguageServer.csproj`. The server resolves `IDrapoContentRoot.AppPath` to
  `Path.Combine(AppContext.BaseDirectory, "content", "app")`, overridable by `--content <path>` for
  development against the live repo.
- **Rationale**: `FunctionService` and `AttributeService` are path-based and convention-driven
  (constitution Principle II). Preserving the identical relative layout means the *same* code reads
  content in the website, the tests and the server. Content weighs a few hundred KB.
- **Alternatives considered**: embedded resources (needs a stream-based abstraction and a second
  read path); fetching from the docs website at runtime (violates FR-004 offline requirement and
  adds a network dependency to an editor).

## R3. Removing the ASP.NET dependency from the services

- **Finding**: the only ASP.NET types used by the four services are
  `Microsoft.AspNetCore.Hosting.IWebHostEnvironment` (for `WebRootPath`) in `FunctionService` and
  `AttributeService`. `DrapoEngineCatalog` depends only on the `Drapo` package assembly
  (`Sysphera.Middleware.Drapo.DrapoMiddlewareOptions` for the assembly handle) and
  `DrapoValidatorService` depends only on the catalog and `IFunctionService`. `DrapoDocContent`
  uses `System.Net.WebUtility` only. `FunctionService` uses `Newtonsoft.Json`.
- **Decision**: introduce `IDrapoContentRoot { string AppPath { get; } }` in the library; WebDocs
  registers `new DrapoContentRoot(Path.Combine(env.WebRootPath, "app"))`. Library `PackageReference`s:
  `Drapo` 2026.8.6.5 (same version as WebDocs, `ExcludeAssets=contentfiles`), `Newtonsoft.Json`
  13.0.x (already transitively present in WebDocs via `Microsoft.AspNetCore.Mvc.NewtonsoftJson`).
- **JSON shape preservation**: the moved `*VM` classes keep property names and casing; WebDocs
  serialises through the same Newtonsoft settings as before, so MCP and REST payloads do not change.
  Namespaces change from `WebDocs.Models/Services/Helpers` to `Drapo.Tooling.Models/Services/Helpers`;
  callers in `Controllers/`, `Tools/`, `ConceptService`, `DataTypeService` update `using`s only.
- **Verification**: a unit test asserts `typeof(DrapoValidatorService).Assembly
  .GetReferencedAssemblies()` contains no name starting with `Microsoft.AspNetCore`.

## R4. Diagnostic position and range mapping

- **Finding**: `DrapoDiagnosticVM` carries 1-based `Line`/`Column` counted in UTF-16 code units,
  incrementing line on `\n` only. LSP wants 0-based `line` and UTF-16 `character` and a `Range`. The
  VM has no length.
- **Decision**: the server computes the range end by scanning forward from the start offset over
  `[A-Za-z0-9_\-]` characters (the attribute/function identifier); if the character at the offset is
  not an identifier character (e.g. unbalanced-mustache points at `{` or `}`), the range is two
  characters wide. `DrapoDiagnosticVM` is **not** extended, so `validate_drapo` output stays
  byte-identical (FR-003, Principle IV).
- **Line endings**: CRLF files — `\r` is counted as a column character before the break, which never
  shifts a token's column because it sits at line end; LSP line numbers match since both treat `\n`
  as the break.
- **Severity/code mapping**: `Level` `"error"` → `DiagnosticSeverity.Error`, `"warning"` → `Warning`;
  `Rule` → `Diagnostic.Code`; `Message` verbatim; `Source` = `"drapo"`. Parity tests compare count,
  line, column, level, rule and message between the VM list and the mapped list.

## R5. Document synchronisation and debouncing

- **Decision**: `TextDocumentSyncKind.Full`; diagnostics recomputed on open/change/save, debounced
  250 ms per document with cancellation of the previous pending run; cleared on close.
- **Rationale**: the validator is regex-based over the whole text and runs in single-digit
  milliseconds for thousands of lines, so incremental sync buys nothing (measured locally on the
  largest sample pages: < 10 ms). Full sync is the simplest correct choice (Principle V).

## R6. Completion, hover and signature-help context detection

- **Decision** (all implemented in `TextPosition` / providers, protocol-agnostic and unit-tested):
  - *Inside a tag*: scanning backwards from the cursor, the nearest `<` occurs after the nearest `>`
    and the cursor is not within a quoted attribute value (odd count of `"`/`'` since that `<`).
  - *Attribute token at cursor*: the maximal `[A-Za-z0-9_\-]` run around the cursor that starts with
    `d-` (case-insensitive) while inside a tag.
  - *Inside a `d-on-*` value*: the cursor is between the quotes of an attribute whose name starts
    with `d-on-` (found with the same regex the validator uses).
  - *Function at cursor*: identifier run around the cursor while inside a `d-on-*` value.
  - *Active call for signature help*: walk backwards from the cursor inside the value tracking
    parenthesis depth; the first unmatched `(` names the call; the active parameter is the number of
    top-level commas between that `(` and the cursor, computed by the shared
    `DrapoHandlerSyntax.SplitArguments` (moved out of the validator so both use one implementation).
- **Completion items**: union of (a) engine fixed attributes, (b) engine/known dynamic prefixes
  offered as the prefix text (e.g. `d-on-`, `d-attr-`, `d-validation-`, `d-dataproperty-`), and (c)
  documented attribute names that pass `IsValidAttribute` (e.g. `d-on-model-change`). Each item's
  `documentation` is the doc description when one exists. `IDrapoEngineCatalog` gains read-only
  `Attributes`, `AttributePrefixes` and `Functions` enumerations (already parsed; only exposed).
- **Hover content** (Markdown): attribute → `**d-name**` + description; function → signature line
  (`FunctionVM.Signature`), description, and a parameter table (name, types, optional, default).
- **Alternatives considered**: a real HTML parser (e.g. AngleSharp) — rejected: the validator is
  regex-based, so a parser would create *two* interpretations of the document and new failure modes;
  the heuristics above are sufficient for v1 and documented as such in the spec's assumptions.

## R7. Startup index and freshness

- **Decision**: `DrapoSymbolIndex` loads all attributes (`AttributeService.GetList`) and all
  functions with parameters (`FunctionService.Get` for each name) once at startup (~200 files,
  < 200 ms). The validator keeps using the services directly (unchanged code path = parity).
- **Rationale**: hover/completion must answer in a few ms; content is immutable for the process
  lifetime (it ships with the extension).

## R8. Packaging the server for VS Code

- **Decision**: `dotnet publish -c Release -r <rid> --self-contained -p:PublishSingleFile=false
  -p:PublishTrimmed=false` for `win-x64`, `linux-x64`, `osx-x64`, `osx-arm64` (script
  `src/Drapo.LanguageServer/publish.ps1`). The extension's `scripts/copy-server.ps1` places the
  output under `server/<rid>/`, and `vsce package --target <vscode-target>` builds one VSIX per
  platform (`win32-x64`, `linux-x64`, `darwin-x64`, `darwin-arm64`). The extension locates the
  binary as `server/<rid>/Drapo.LanguageServer[.exe]`; a `drapo.server.path` setting overrides it
  (used for development: point at `bin/Debug/net8.0/Drapo.LanguageServer.exe`).
- **Rationale**: FR-014 (no runtime install). Trimming is disabled because the engine catalog reads
  an embedded resource through reflection on the `Drapo` assembly and the LSP framework uses
  reflection-heavy JSON; correctness over size for v1 (~70 MB per platform is acceptable for a VSIX).
- **Alternatives considered**: framework-dependent publish + require .NET runtime (fails FR-014);
  `PublishAot` (OmniSharp + Newtonsoft are not AOT-safe).
- **Marketplace publication** is out of scope; CI uploads the VSIX files as workflow artifacts.

## R9. Test strategy and corpus

- **Decision**: single xunit project `src/Drapo.Tests` referencing both the library and the server
  (server providers are plain classes, so no protocol client is needed for unit tests).
  - *Parity corpus*: every `app/functions/*/samples/*/content.html` (currently all expected to
    validate with zero errors — this also enforces constitution Principle III) plus
    `Fixtures/*.html` bad snippets with expected diagnostics in sibling `*.json` files (unknown
    attribute, unknown function, arity warning, malformed `d-for`, both unbalanced-mustache cases,
    dynamic-prefix families that must *not* be flagged, case-insensitive names).
  - *Mapper parity*: for each corpus document, `DiagnosticMapper.Map(vm, text)` yields the same
    count/line/column/severity/code/message as the VM list (SC-001).
  - *Robustness*: a set of malformed documents (unclosed tags, unbalanced quotes, truncated files,
    empty text, binary noise) must not throw from any provider (SC-006).
  - *Stdio smoke*: start the built server process, send `initialize` → `initialized` →
    `textDocument/didOpen` with a bad attribute → expect a `publishDiagnostics` notification →
    `shutdown`/`exit`; asserts the process exits 0.
- **Rationale**: the repo has no test project today; this is the minimum that proves the spec's
  measurable outcomes without an editor in the loop.

## R10. CI

- **Decision**: add `.github/workflows/ci.yml` running `dotnet build src/docs.sln` and `dotnet test`
  on pull requests and pushes; on a `lsp-v*` tag it additionally runs `publish.ps1` for each RID,
  `npm ci && npm run package -- --target <t>` and uploads the VSIX artifacts. `docker-image.yml`
  stays untouched; the Dockerfile gains one `COPY` line for `Drapo.Tooling/Drapo.Tooling.csproj` in
  the restore layer.
