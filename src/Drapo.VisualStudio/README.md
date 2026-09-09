# Drapo for Visual Studio

Visual Studio 2022 (17.x) and 2026 (18.x) extension that hosts the same `Drapo.LanguageServer`
the VS Code extension uses, through Visual Studio's built-in Language Server Protocol client.
It applies to **`.html` / `.htm` files** (content type `HTML`). Razor files use the `Razor`
content type and are deliberately left to the Razor editor.

## What you get

- **Diagnostics**: unknown `d-*` attributes, unknown functions inside `d-on-*` handlers, calls
  with too few arguments, malformed `d-for`, unbalanced `{{ }}` (Error List + squiggles).
- **Completion** of the `d-*` attributes the engine recognises, **hover** documentation and
  **signature help** for functions in handlers.
- **Semantic highlighting**: `d-*` attribute names are classified as keywords, handler function
  names as methods, mustaches as identifiers. Visual Studio maps LSP token types through a fixed
  table, so unlike VS Code it has no distinct colour for symbols the engine does not know; those
  still get a diagnostic.

## Install

Download `Drapo.VisualStudio-<version>.vsix` from the
[GitHub Releases](https://github.com/spadrapo/docs/releases) page (tags `lsp-v*`) and
double-click it, or run it with `VSIXInstaller.exe` (under `<VS>\Common7\IDE\`). The package
bundles a self-contained server, so **no .NET runtime is required**. To remove it:
*Extensions → Manage Extensions → Installed → Drapo → Uninstall*, or

```powershell
& "<VS>\Common7\IDE\VSIXInstaller.exe" /quiet /uninstall:Drapo.VisualStudio
```

## How it works

- `DrapoLanguageClient` exports `ILanguageClient` for content type `HTML` and starts
  `server\Drapo.LanguageServer.exe --content server\content\app` over stdio.
- `HtmlContentType` adds `languageserver-base` as an extra base of the `HTML` content type.
  Every LSP client part in Visual Studio is exported for `languageserver-base`; without this
  bridge the client is never activated for `.html` files.
- The server answers semantic tokens **statically** in the `initialize` result even though the VS
  client advertises dynamic registration (`Program.ForceStaticSemanticTokens`); the VS tagger only
  reads static capabilities. Full/delta is supported because VS re-polls every two seconds while
  a document is visible.

## Build

Prerequisites: Visual Studio 2022 or 2026 with the *Visual Studio extension development* workload
(MSBuild + VSSDK), .NET SDK 8.

```powershell
./build.ps1                 # publishes the server (win-x64), builds Drapo.VisualStudio-<version>.vsix
./build.ps1 -SkipPublish    # reuse server/ from a previous run
./build.ps1 -Version 0.2.0.0
```

The version defaults to the VS Code extension's `package.json` version plus `.0`, so one number
covers both editors per release. The project is **not** part of `docs.sln`: it targets
.NET Framework 4.7.2 and needs the VSSDK build tools, which the Linux CI/Docker build of the
solution does not have. CI builds it in its own `vs-extension` job (`.github/workflows/ci.yml`)
and the release job attaches the VSIX next to the VS Code packages.

## Debugging a client problem

Set `DRAPO_LSP_LOG=<file>` in the environment Visual Studio starts from (for example from a
Developer PowerShell: `$env:DRAPO_LSP_LOG = "$env:TEMP\drapo-lsp.log"; devenv`). The server
appends what it receives (`didOpen`, semantic tokens requests) to that file. Visual Studio only
recomposes its MEF cache when the extension is (re)installed; after swapping files by hand under
`%LocalAppData%\Microsoft\VisualStudio\<ver>\Extensions\...`, clear
`%LocalAppData%\Microsoft\VisualStudio\<ver>\ComponentModelCache`.

## Smoke test

Verified on VS 2022 17.14 and VS 2026 18.7 (see `specs/004-lsp-improvements/quickstart.md`):
open an `.html` file containing

```html
<div d-nope="x" d-if="{{show}}">
    <button d-on-click="UpdateSector();Bogus(1)">Go</button>
    <input type="button" value="Show" d-on-click="ShowWindow(center)" />
</div>
```

`d-nope` and `Bogus` are underlined in red, `UpdateSector()` in green (warning), the Error List
shows 2 errors and 1 warning, `ShowWindow(center)` is clean, and the `d-*` names are keyword-blue.
