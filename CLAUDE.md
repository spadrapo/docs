# CLAUDE.md

Guidance for AI agents working in this repository. Keep it accurate — it is read every session.

## What this repository is

`spadrapo/docs` is the official documentation for **Drapo**, a declarative .NET frontend framework
(reactive SPAs driven by `d-*` HTML attributes, server-integrated, "no JavaScript required"). The
repo is two things at once:

1. **Documentation content** — structured HTML/JSON files under `src/WebDocs/wwwroot/app/`.
2. **An ASP.NET Core (.NET 8) app** — serves that content to humans as a website **and** to AI
   agents through an **MCP server** (functions, attributes, concepts, data types, validation).

The site is built **with Drapo itself** (dogfooding); the engine is served at `/drapo.js` from the
`Sysphera.Middleware.Drapo` package.

## Layout

```
src/docs.sln                     # WebDocs + Drapo.Tooling + Drapo.LanguageServer + Drapo.Tests
src/Drapo.Tooling/               # class library, NO ASP.NET: DrapoEngineCatalog, DrapoValidatorService,
                                 #   FunctionService, AttributeService, their *VM models,
                                 #   DrapoDocContent/DrapoHandlerSyntax helpers, IDrapoContentRoot
src/Drapo.LanguageServer/        # LSP over stdio (OmniSharp) on top of Drapo.Tooling: diagnostics,
                                 #   completion, hover, signature help; content copied to bin/content/app
src/Drapo.Tests/                 # xunit: parity corpus (fixtures + every function sample), providers,
                                 #   stdio smoke test, "no ASP.NET reference" guard
src/vscode-drapo/                # VS Code extension (TypeScript) that launches the bundled server;
                                 #   syntaxes/drapo.injection.json = TextMate highlighting
src/Drapo.VisualStudio/          # Visual Studio 2022/2026 extension (VSIX, net472, ILanguageClient) hosting
                                 #   the same server for .html; built by build.ps1, NOT in docs.sln
src/WebDocs/                     # the ASP.NET Core app
  Program.cs / Startup.cs        # host + DI + MCP wiring (registers IDrapoContentRoot = wwwroot/app)
  Controllers/                   # Attribute, Function, Menu, Sample, Search, Chat, NuGet, Todo
  Services/                      # ConceptService, DataTypeService, NuGetService (+ I* interfaces);
                                 #   catalog/validator/function/attribute services live in Drapo.Tooling
  Models/                        # WebDocs-only *VM ViewModels (ConceptVM, DataTypeVM, MenuItemVM, ...)
  styles/                        # Less sources → compiled to wwwroot/css via Cake
  wwwroot/
    app/menu/NNNN - <Section>/   # doc pages, numbered: Guide, Data, Attributes, Functions,
                                 #   Debugging, Applications (NNNN prefixes drive ordering)
    app/functions/<Name>/        # one folder per Drapo function (see "Content conventions")
    components/<name>/           # Drapo components, rendered as <d-name>
  drapo.js (served)              # the real engine, embedded in Sysphera.Middleware.Drapo

.specify/                        # Spec Kit (constitution, templates, scripts, extensions)
.claude/skills/speckit-*         # Spec Kit slash-command skills
plugins/drapo-resolver/          # marketplace plugin / skill: app-specific symbol resolver
.github/copilot-instructions.md  # Copilot equivalent of this file
```

## Content conventions (follow exactly — services discover by convention)

- **Menu page**: `src/WebDocs/wwwroot/app/menu/NNNN - <Section>/NNNN - <title>.html`. The numeric
  prefixes order items; keep them consistent within a section. Pages use `<d-code>` for examples.
- **Function**: `src/WebDocs/wwwroot/app/functions/<Name>/` with:
  - `description.html` — what it does.
  - `parameters.json` — array of `{ Name, Description, Types[], Optional, DefaultValue? }`.
  - `samples/NNN/{description.html, content.html}` — numbered upward.
- **Component**: `src/WebDocs/wwwroot/components/<name>/` → tag `d-<name>`.

Off-convention files are invisible to `FunctionService` / `AttributeService` / `MenuController`.

## Non-negotiables (from `.specify/memory/constitution.md` — read it)

1. **Accuracy over the real engine.** Never document an attribute/function/parameter the engine
   doesn't have. Verify against `DrapoEngineCatalog`, `validate_drapo`, and the Drapo MCP — not
   memory. Engine wins when docs disagree.
2. **Examples must be valid Drapo.** Every sample passes `validate_drapo`; every `d-dataKey` /
   component / sector it names must resolve (use the `drapo-resolver` skill).
3. **Docs ↔ tooling parity.** A content-shape change (e.g. `parameters.json` schema) ships together
   with its Service / `*VM` / Controller / MCP counterpart.
4. **Simplicity & dogfooding.** Prefer Drapo-native over hand-written JS; add deps only with a
   stated reason; keep the .NET 8 / Less-Cake / Docker build as documented.

## Tooling available to you

- **Drapo MCP** (the framework reference): `get_attributes`, `get_functions`, `get_concepts`,
  `get_data_types`, `get_*_details`, and `validate_drapo` (is this snippet legal Drapo?).
- **drapo-resolver skill/plugin** (this app's symbols): `resolve-datakey` / `resolve-component` /
  `resolve-sector`, `find-references`, `scan-unresolved`. The MCP knows the framework; the resolver
  knows *this app*. Use both before claiming a Drapo edit is correct.
- **Spec Kit** (`/speckit-*` skills): `constitution`, `specify`, `clarify`, `plan`, `tasks`,
  `analyze`, `checklist`, `implement`, `taskstoissues`, `agent-context-update`. Artifacts live under
  `specs/<feature>/` and `.specify/`.

## Build & run

```bash
cd src/WebDocs
dotnet restore
dotnet run                    # https://localhost:5001  /  http://localhost:5000
dotnet build                  # before claiming a serving-layer change is done
# CSS (optional): dotnet cake build.cake --target=less

cd src
dotnet test docs.sln          # tooling + language server tests (parity corpus, stdio smoke)
dotnet run --project Drapo.LanguageServer -- --content WebDocs/wwwroot/app   # LSP on stdio
```

Editor support: `src/vscode-drapo/README.md` (F5 to debug, `npm test` for the VS Code integration
test, `npm run test:grammar` for the TextMate grammar, `publish.ps1` + `copy-server.ps1` +
`npm run package:<target>` to build a VSIX) and `src/Drapo.VisualStudio/README.md` (`build.ps1`,
needs Visual Studio with the extension-development workload; smoke test by installing the VSIX).

Docker: `docker build -t drapo-docs -f src/Dockerfile src/` then `docker run -p 8080:80 drapo-docs`.

## Conventions for changes

- Scope PRs narrowly (a family of attributes, one function, one section) — mirror the commit history.
- Validate before commit: examples pass `validate_drapo`; documented symbols exist; naming/numbering
  conventions held; `dotnet build` green for serving-layer changes.
- Default to Windows PowerShell for scripts (`.specify/scripts/powershell/`); Bash equivalents exist.

<!-- SPECKIT START -->
For additional context about technologies to be used, project structure,
shell commands, and other important information, read the current plan
at specs/004-lsp-improvements/plan.md
<!-- SPECKIT END -->
