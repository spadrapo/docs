# Research: LSP Improvements

**Feature**: `004-lsp-improvements` · **Date**: 2026-09-09 · **Issue**: #389

Every decision below was verified against the real engine (`drapo.js` embedded in the `Drapo`
2026.9.9.9 package), the real Visual Studio installs on the maintainer's machine (VS 2022 17.14 and
VS 2026 18.7, MEF metadata + decompiled LSP client), or the library source in use. Nothing here is
from memory.

## R1. Why `ShowWindow(name)` was flagged, and the general arity rule

**Decision**: the documented minimum for every function is the *unconditional* minimum the engine
needs. When the engine's need for an argument depends on the *value* of an earlier one, the docs
mark it optional and the validator adds a targeted rule (`CheckConditionalArity`) that only fires
when the condition is decidable from the literal text.

**Rationale**: `ExecuteFunctionShowWindow` reads `Parameters[1]` (the `did`) only when
`Parser.IsUri(Parameters[0])` is true, and `IsUri` is "starts with `~` or `/`". A window definition
name (registered server-side with `options.Config.CreateWindow(name, path, did, parameters)`) needs
no did. The validator's `CheckArity` counts `!p.Optional`, and `did` was `Optional: false`.

**Alternatives considered**: (a) teaching `parameters.json` a "required when" expression — rejected:
it changes the content schema for every consumer (site, MCP, hover, signature help) for one case;
(b) dropping arity checks — rejected: the too-few-arguments warning is the only signal that catches
real bugs (e.g. `UpdateSector()`).

## R2. Full audit of the 96 documented functions against the engine

**Method**: extract every `ExecuteFunction<Name>` body; count leading `functionParsed.Parameters[i]`
reads that are unguarded (no `Parameters.length` check) *and* whose absence crashes or clearly
misbehaves. Engine facts that decide borderline cases: `ResolveFunctionParameter(undefined)` returns
`undefined` (no throw) except when `canUseReturnFunction` is set; `ParseMustache`, `IsUri`,
`ParseFunctions` and `.toLowerCase()` throw on `undefined`. Full table with line evidence for all 96
functions: [arity-audit.md](arity-audit.md).

**Result**: 86 OK, 7 documented too strictly (false warnings), 3 documented too loosely, 0 without
an engine implementation. One engine dispatch case (`external`) is an inert stub with no docs.

| Function | Doc min → Engine min | Fix |
|---|---|---|
| ShowWindow | 2 → 1 | `did` optional; conditional rule for a literal url |
| AddDate | 3 → 0 | `Date`, `Type`, `Increment` optional with engine defaults (now / day / 1) |
| HasDataChanges | 2 → 0 | `Sector`, `DataKey` optional (empty = all sectors / all data) |
| AcceptDataChanges | 2 → 0 | same as HasDataChanges |
| CreateData | 3 → 1 | `Key`, `Value` optional (pairwise loop; no pair = `{}`) |
| CreateGuid | 2 → 0 | `DataKey`, `DataField` optional; conditional rule: exactly 1 argument warns (engine has no valid 1-arg shape) |
| GetSector | 1 → 0 | `parameters.json` was a nameless `{ "Types": [""] }` entry; now `[]` (the repo's own `GetSector()` samples were flagged) |
| FilterData | 2 → 3 | engine returns `''` below 3 slots; `d-if` slot required (may be empty), `DataKey` explicitly required |
| ExecuteInstanceFunction | 1 → 3 | engine crashes on `ParseMustache(undefined)` with 2 args; `Sector` and `Return` slots required (may be empty) |
| PushStack | 0 → 1 | engine tolerates (pushes `undefined`), **left as is** per FR-005 (only fix too-low when the engine demonstrably fails) |

**Informational doc-shape findings** (not arity; recorded for a follow-up, out of scope here):
ClearData's second parameter is really `Notify`; UncheckItemField's `Notify` is never read; MoveItem's
`Key` is ignored; Notify has three undocumented optional parameters; ReplaceItemField's `Recursive`/
`Resolve` sit at engine indices 6/7 (docs say 5/6).

## R3. Visual Studio: hosting the server through the built-in LSP client

**Decision**: a classic VSSDK/MEF extension (`Drapo.VisualStudio`) exporting `ILanguageClient`
for content type `HTML`, SDK-style csproj, one VSIX for VS 2022 (17.x) and VS 2026 (18.x).

**Verified facts** (decompiled `Microsoft.VisualStudio.LanguageServer.Client.Implementation.dll`
in 17.14 and 18.7, both identical for this purpose):

- Packages: `Microsoft.VisualStudio.SDK` 17.14.40265 (`ExcludeAssets="runtime"`) brings
  `Microsoft.VisualStudio.LanguageServer.Client` 17.14.60; there is no 18.x SDK. VS 2026 supports
  API 17.x and ignores the upper bound of the installation target, so `Version="[17.0,)"` targets
  both. `Microsoft.VSSDK.BuildTools` 18.5+ makes SDK-style VSIX projects official; verified to
  build with MSBuild 17.14 (what `windows-latest` ships), MSBuild 18.7 and `dotnet build`.
- `.html`/`.htm` map to content type **`HTML`** (base `text`). `htmlx` does not exist. The new
  Razor editor uses content type `Razor`, which does not derive from `HTML`, so `.cshtml`/`.razor`
  are excluded by construction (only the legacy Razor editor's `LegacyRazorCSharp` derives from
  `HTML`; the server ignores non-`.html` URIs as a belt-and-braces measure).
- **Critical**: every LSP client MEF part in VS is exported for `languageserver-base`, and `didOpen`
  is only sent when the buffer's content type `IsOfType("languageserver-base")`. `HTML` does not
  derive from it. The registry merges same-name `ContentTypeDefinition` exports, so the extension
  exports `[Name("HTML")] [BaseDefinition("languageserver-base")]` to add the base without
  redefining HTML. Without this the client is never activated for `.html`.
- MEF export + a `MefComponent` asset is sufficient; no `AsyncPackage`/AutoLoad. The server is
  started lazily on the first `HTML` buffer. `Connection(reader: server stdout, writer: server stdin)`.
- Semantic tokens: supported (range, full, full/delta, refresh) in 17.14 and 18.7. Requires a legend;
  token types map through a fixed switch (`keyword`→keyword, `function`→method, `variable`→identifier,
  everything else→text); unknown modifiers are skipped. So the server's legend uses **standard token
  types** (`keyword`, `function`, `variable`) with a custom `unknown` modifier that only VS Code maps.
- Bundling: `<Content Include="server\**\*" IncludeInVSIX="true" VsixSubPath="server" />`; locate at
  runtime from the extension assembly's directory. Version via the manifest placeholder
  `|%CurrentProject%;GetVsixVersion|` + `-p:VsixVersion=a.b.c.d` (2–4 numeric parts, no prerelease).
- Install for smoke tests: `<VSInstallDir>\Common7\IDE\VSIXInstaller.exe /q <file>` and
  `/u:<Identity Id>`; find the install dir with `vswhere`.

**Alternatives considered**: the new out-of-process `VisualStudio.Extensibility` model — rejected: it
has no LSP client story equivalent to `ILanguageClient` for arbitrary content types, needs VS 17.9+
runtime pieces, and the classic model is confirmed present unchanged in 18.7.

## R4. CI: building the Visual Studio VSIX in the same workflow

**Decision**: add a `vs-extension` job on `windows-latest` to `ci.yml` (build on every push/PR;
package on `lsp-v*` tags), upload `Drapo.VisualStudio-<version>.vsix` as an artifact, and extend the
existing `release` job's download pattern and notes to include it. `release-extension.yml` is
unchanged for the maintainer (one version, one tag).

**Rationale**: `windows-latest` has VS 2022 17.14 with the VSSDK workload and MSBuild 17.14; the
VSSDK BuildTools NuGet ships its own targets. The VSIX version is the VS Code `package.json` version
(FR-015), padded to `major.minor.patch.0`.

## R5. Syntax highlighting: TextMate injection + semantic tokens

**Decision (grammar)**: one injection grammar `text.html.drapo.injection` injected into
`text.html.basic`, `text.html.derivative`, `text.html.cshtml` and `text.aspnetcorerazor` with
selector `L:text.html -comment -source.js -source.css -source.cs -source.ts`. It takes over
`d-*` attributes entirely (`d-on-*` first, with a nested function-call rule; other `d-*` with a
mustache rule) and colours `{{ }}` in text and in any attribute value. Scopes: attribute names get
`entity.other.attribute-name.drapo keyword.other.drapo` (the deeper `keyword` scope wins in themes,
so Drapo attributes differ from plain HTML attributes), function names `entity.name.function.drapo`,
mustaches `variable.other.mustache.drapo`, delimiters/punctuation standard scopes.

**Decision (semantic tokens)**: full + range, no delta. Legend types `keyword` (attribute),
`function` (handler function name), `variable` (mustache, delimiters included); modifiers
`defaultLibrary` (known) and `unknown` (not in the engine). VS Code maps `keyword.defaultLibrary`,
`keyword.unknown`, `function.defaultLibrary`, `function.unknown` to the grammar's scopes through
`semanticTokenScopes` (unknown → `invalid.illegal.*`, red in every default theme), so TextMate and
semantic layers agree. Known/unknown comes from `IDrapoEngineCatalog`, the same object the validator
uses (FR-020). Tokens never overlap and never span lines (VS advertises no multiline support).

**Alternatives considered**: custom token types (`drapoAttribute` etc.) with `superType` — rejected
because Visual Studio maps only standard type names (everything else renders as plain text); a
TextMate grammar that re-scopes plain HTML — rejected (FR-018: no change outside Drapo markup).

**Grammar test**: `vscode-textmate` + `vscode-oniguruma` tokenize a fixture with the real
`html.tmLanguage.json` shipped in the VS Code build the integration test already downloads, and
assert the scopes of `d-if`, `{{item.name}}`, `ShowWindow(` and that plain attributes keep
`entity.other.attribute-name.html`.

## R6. OmniSharp language server library support

`OmniSharp.Extensions.LanguageServer` 0.19.9 (already referenced) provides `SemanticTokensHandlerBase`,
`SemanticTokensBuilder`, `SemanticTokensDocument` and `SemanticTokensLegend`; no dependency change.
