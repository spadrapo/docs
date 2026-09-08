# Contract: `vscode-drapo` extension

**Location**: `src/vscode-drapo/`. **Publisher/id**: `spadrapo.vscode-drapo` (name `vscode-drapo`,
display name "Drapo"). **Engine**: `vscode ^1.90.0`. **Runtime dependency**: `vscode-languageclient`
^10.

## Manifest (`package.json`) essentials

```json
{
  "activationEvents": ["onLanguage:html", "onLanguage:razor", "onLanguage:aspnetcorerazor"],
  "main": "./out/extension.js",
  "contributes": {
    "configuration": {
      "title": "Drapo",
      "properties": {
        "drapo.server.path": {
          "type": "string", "default": "",
          "description": "Absolute path to a Drapo.LanguageServer executable. Leave empty to use the bundled server."
        },
        "drapo.trace.server": {
          "type": "string", "enum": ["off", "messages", "verbose"], "default": "off"
        }
      }
    },
    "commands": [{ "command": "drapo.restartServer", "title": "Drapo: Restart Language Server" }]
  }
}
```

No other contributions (no grammars, no snippets) in v1.

## Server lookup order (`extension.ts`)

1. `drapo.server.path` setting, if non-empty and the file exists.
2. `<extension dir>/server/<rid>/Drapo.LanguageServer[.exe]` where `rid` is derived from
   `process.platform` / `process.arch`: `win32-x64 → win-x64`, `linux-x64 → linux-x64`,
   `darwin-x64 → osx-x64`, `darwin-arm64 → osx-arm64`.
3. Otherwise show an error notification: *"Drapo language server not found for <platform>. Set
   `drapo.server.path` or install the platform-specific package."* and do not start the client
   (FR-015).

On non-Windows the bundled binary is `chmod 755` at activation if not executable.

## Client options

- `documentSelector`: `file` and `untitled` schemes for languages `html`, `razor`,
  `aspnetcorerazor`.
- `synchronize.fileEvents`: none (v1 has no workspace features).
- `outputChannelName`: `Drapo Language Server`.
- Server start failure surfaces through `client.start()` rejection → error notification with the
  exception message.

## Packaging matrix

| `vsce --target` | bundled `server/<rid>` |
|-----------------|------------------------|
| `win32-x64` | `win-x64` |
| `linux-x64` | `linux-x64` |
| `darwin-x64` | `osx-x64` |
| `darwin-arm64` | `osx-arm64` |

`.vscodeignore` excludes `src/`, `node_modules/` (bundled by esbuild into `out/extension.js`) and all
`server/*` directories except the one for the current target (the copy script populates only that
directory before each `vsce package`).

## Scripts

| npm script | Does |
|------------|------|
| `compile` | `esbuild src/extension.ts --bundle --platform=node --external:vscode --outfile=out/extension.js` |
| `package` | `vsce package --target <target>` (target passed through) |

`scripts/copy-server.ps1 -Rid <rid>` copies `../Drapo.LanguageServer/bin/publish/<rid>/` to
`server/<rid>/`, clearing other `server/*` folders first.
