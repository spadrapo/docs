# Copilot instructions — Drapo Documentation

This repository (`spadrapo/docs`) is the official documentation for **Drapo**, a declarative .NET
frontend framework (reactive SPAs driven by `d-*` HTML attributes, server-integrated). It is two
things at once:

1. **Documentation content** — structured HTML/JSON under `src/WebDocs/wwwroot/app/`.
2. **An ASP.NET Core (.NET 8) app** that serves that content as a website **and** to AI agents via
   an **MCP server** (functions, attributes, concepts, data types, validation).

The site is built **with Drapo itself**; the engine is served at `/drapo.js` from the
`Sysphera.Middleware.Drapo` package.

## Project layout

- `src/WebDocs/` — the app: `Program.cs`/`Startup.cs` (host, DI, MCP), `Controllers/`, `Services/`
  (`FunctionService`, `AttributeService`, `ConceptService`, `DataTypeService`, `DrapoEngineCatalog`,
  `DrapoValidatorService`, `NuGetService` + `I*` interfaces), `Models/` (`*VM` ViewModels),
  `styles/` (Less → CSS via Cake).
- `src/WebDocs/wwwroot/app/menu/NNNN - <Section>/` — doc pages (Guide, Data, Attributes, Functions,
  Debugging, Applications); numeric prefixes drive ordering.
- `src/WebDocs/wwwroot/app/functions/<Name>/` — one folder per Drapo function.
- `src/WebDocs/wwwroot/components/<name>/` — Drapo components, rendered as `<d-name>`.

## Content conventions (services discover by convention — follow exactly)

- **Menu page**: `app/menu/NNNN - <Section>/NNNN - <title>.html`; keep numbering consistent; use
  `<d-code>` for examples.
- **Function** `app/functions/<Name>/`:
  - `description.html` — what it does.
  - `parameters.json` — array of `{ Name, Description, Types[], Optional, DefaultValue? }`.
  - `samples/NNN/{description.html, content.html}` — numbered upward.
- **Component**: `components/<name>/` → tag `d-<name>`.

Files that ignore these conventions are invisible to the serving layer and the MCP.

## Rules (see `.specify/memory/constitution.md`)

1. **Accuracy over the real engine.** Never document an attribute/function/parameter the engine
   doesn't have. Verify against the engine catalog and `validate_drapo` — not memory. The engine is
   authoritative when it disagrees with a doc.
2. **Examples must be valid Drapo** — they must validate, and the `d-dataKey`s / components / sectors
   they reference must actually exist in this app.
3. **Docs ↔ tooling parity** — a content-shape change ships with its Service / `*VM` / Controller /
   MCP counterpart in the same change.
4. **Simplicity & dogfooding** — prefer Drapo-native over hand-written JavaScript; add dependencies
   only with a clear reason; keep the .NET 8 / Less-Cake / Docker build as documented.

## Build & run

```bash
cd src/WebDocs
dotnet restore && dotnet run     # https://localhost:5001 / http://localhost:5000
dotnet build                     # before claiming a serving-layer change is done
```

## How to add a Drapo function

Create `src/WebDocs/wwwroot/app/functions/<Name>/` with `description.html`, `parameters.json`, and
`samples/NNN/{description.html, content.html}`. To extend the MCP/serving layer, update the matching
service/controller in `src/WebDocs` and keep it in parity with the content (rule 3).
