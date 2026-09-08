# Drapo for VS Code

Editor support for [Drapo](https://drapo.tech) `d-*` markup, driven by the same engine catalog and
validator that power the Drapo documentation site and its MCP `validate_drapo` tool. What the docs
say is valid is what the editor says is valid: there is one validator, not two.

## Features

- **Diagnostics** in the Problems panel: unknown `d-*` attributes, unknown functions inside
  `d-on-*` handlers, calls with too few arguments, malformed `d-for`, unbalanced `{{ }}`.
- **Completion** of every attribute the Drapo engine recognises, including prefix families such as
  `d-on-`, `d-attr-`, `d-validation-` and `d-dataproperty-`.
- **Hover** documentation for attributes and for functions used in handlers.
- **Signature help** while typing a function call inside a `d-on-*` value.

Applies to `html`, `razor` and `aspnetcorerazor` documents.

## Settings

| Setting | Default | Meaning |
|---------|---------|---------|
| `drapo.server.path` | `""` | Absolute path to a `Drapo.LanguageServer` executable. Empty uses the server bundled with the extension. |
| `drapo.trace.server` | `off` | Trace the client/server protocol in the *Drapo Language Server* output channel. |

Command: **Drapo: Restart Language Server**.

## Install

Download the `.vsix` for your platform from the
[GitHub Releases](https://github.com/spadrapo/docs/releases) page (tags `lsp-v*`) and run:

```powershell
code --install-extension vscode-drapo-<platform>-<version>.vsix
```

## Packages

The extension ships one package per platform, each bundling a self-contained server, so no .NET
runtime is required:

| VS Code target | Server runtime |
|----------------|----------------|
| `win32-x64` | `win-x64` |
| `linux-x64` | `linux-x64` |
| `darwin-x64` | `osx-x64` |
| `darwin-arm64` | `osx-arm64` |

## Development

Prerequisites: .NET SDK 8, Node.js 20+.

```powershell
# 1. build the server (Debug) so it exists at ../Drapo.LanguageServer/bin/Debug/net8.0/
dotnet build ../Drapo.LanguageServer

# 2. build the extension
npm install
npm run compile
```

Open this folder in VS Code and press **F5**. In the Extension Development Host set
`drapo.server.path` to the Debug server binary (for example
`<repo>/src/Drapo.LanguageServer/bin/Debug/net8.0/Drapo.LanguageServer.exe`) and open an HTML file
containing `<div d-nope="x"></div>`; `d-nope` is underlined.

### Integration test

`npm test` downloads a test build of VS Code into `.vscode-test/` (once), opens a fixture
workspace with the extension loaded from source and asserts, through the real VS Code API, that
diagnostics, completion, hover and signature help work and that fixing a problem clears its
diagnostic. It uses the Debug server by default; `DRAPO_SERVER_PATH` overrides it and
`DRAPO_TEST_BUNDLED=1` exercises the server bundled under `server/<rid>/` instead.

### Packaging

```powershell
../Drapo.LanguageServer/publish.ps1 -Rid win-x64      # self-contained server
./scripts/copy-server.ps1 -Rid win-x64                # place it under server/win-x64/
npm run package:win32-x64                             # vscode-drapo-win32-x64-<version>.vsix
code --install-extension vscode-drapo-win32-x64-0.1.0.vsix
```

Repeat with the other runtime identifiers for the other targets.

### Releasing

Actions → **Release extension** → *Run workflow*, optionally with a version. It bumps
`package.json` when needed, tags `lsp-v<version>` and starts the CI build that packages all four
platforms and attaches them to a GitHub Release. Pushing an `lsp-v*` tag by hand does the same.

## Source

The extension lives in the Drapo documentation repository:
<https://github.com/spadrapo/docs/tree/master/src/vscode-drapo>. The language server is
`src/Drapo.LanguageServer`, built on the shared `src/Drapo.Tooling` library.
