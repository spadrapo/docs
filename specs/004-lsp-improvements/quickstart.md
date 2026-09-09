# Quickstart: verifying LSP Improvements (004)

**Issue**: #389 · **Branch**: `004-lsp-improvements`

## 1. Automated checks (all green on 2026-09-09)

```powershell
cd src
dotnet build docs.sln                       # WebDocs, Tooling, LanguageServer, Tests
dotnet test docs.sln                        # parity corpus, validator fixtures, providers, stdio smoke
cd vscode-drapo
npm ci
npm run test:grammar                        # TextMate scopes over the real VS Code HTML grammar
npm test                                    # headless VS Code: diagnostics, completion, hover, signature help, semantic tokens
cd ../Drapo.VisualStudio
./build.ps1                                 # publishes the server, builds Drapo.VisualStudio-<version>.vsix (needs VS + VSSDK)
```

What the .NET tests now cover on top of feature 003:

| Test | Guards |
|---|---|
| `ValidatorTests` fixtures `showwindow-definition-ok`, `showwindow-url-without-did` | `ShowWindow(name)` clean; literal url without did warns; `ShowWindow()` warns |
| fixtures `arity-audit-ok`, `arity-audit-warn` | every call shape corrected by the audit, and the shapes the engine cannot run |
| `FunctionSamplesArityTest` | no documented sample yields a `wrong-arity` warning (SC-001) |
| `SemanticTokensProviderTests` | classification, agreement with the validator, prose exclusion, multi-line split, no overlaps, 5,000-line timing |
| `StdioSmokeTest` | with a Visual Studio-like client (dynamic registration): static legend in `initialize`, `full`, `full/delta` (empty + real edit) |
| `LanguageIdTests` | Visual Studio's language ids map onto the handlers' document selector |

## 2. US1 — arity, by hand

Open `src/Drapo.Tests/Fixtures/arity-audit-ok.html` in VS Code with the extension: no warning.
Open `arity-audit-warn.html`: four `wrong-arity` warnings. On the docs site (`dotnet run` in
`src/WebDocs`), the ShowWindow page shows `did` as optional with the new description, and sample 002
opens the window by definition name (`center`, registered in `Startup.cs`).

## 3. US2 — Visual Studio, by hand (done on VS 2022 17.14 and VS 2026 18.7)

1. Build or download `Drapo.VisualStudio-<version>.vsix`; close every Visual Studio; install:
   `& "<VS>\Common7\IDE\VSIXInstaller.exe" /quiet /instanceIds:<id> <file>` (ids from `vswhere -property instanceId`).
2. Open an `.html` file containing:
   ```html
   <div d-nope="x" d-if="{{show}}">
       <button d-on-click="UpdateSector();Bogus(1)">Go</button>
       <ul d-for="item in {{items}}"><li>{{item.name}}</li></ul>
       <input type="button" value="Show" d-on-click="ShowWindow(center)" />
   </div>
   ```
3. Expected (observed on both versions): `d-nope` and `Bogus` red squiggles, `UpdateSector()` green
   squiggle, Error List "2 errors, 1 warning", `ShowWindow(center)` clean, `d-*` names in keyword
   blue (distinct from `type`/`value`); a `Drapo.LanguageServer.exe` process under
   `%LocalAppData%\Microsoft\VisualStudio\<ver>\Extensions\...\server\`.
4. Type `d-` inside a tag → completion; hover `d-if` → documentation (host-provided LSP features; not part of the automated smoke test, check by hand).
5. Uninstall: `VSIXInstaller.exe /quiet /uninstall:Drapo.VisualStudio`.

Tracing a client: set `DRAPO_LSP_LOG=<file>` before starting the editor; the server appends the
`didOpen` and semantic tokens requests it receives.

Findings that shaped the implementation (see research.md R3):
- Without the `HTML` → `languageserver-base` content type bridge, VS never sends `didOpen`.
- VS advertises dynamic registration for semantic tokens but its tagger reads only the static
  `initialize` capabilities: the server now forces static registration.
- VS re-requests tokens every 2 s while the document is visible; the server supports `full/delta`
  so those are cheap.
- Files swapped by hand under the Extensions folder are ignored until `ComponentModelCache` is cleared.

## 4. US3 — highlighting, by hand

VS Code: open the fixture above. Before the server starts, `d-*` names, `{{ }}` and `UpdateSector(`
are already coloured (TextMate). After ~1 s, `d-nope` and `Bogus` turn to the theme's invalid colour
(semantic `unknown` modifier). *Developer: Inspect Editor Tokens and Scopes* shows
`entity.other.attribute-name.drapo keyword.other.drapo` on `d-if` and the semantic token type
`keyword` with modifier `defaultLibrary`.

## 5. Release

Actions → *Release extension* → Run workflow (version optional). The `lsp-v<version>` release
carries four `vscode-drapo-*.vsix` plus `Drapo.VisualStudio-<version>.0.vsix`.
