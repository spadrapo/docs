# Changelog

## Unreleased

- Visual Studio: completion, hover and signature help now work. VS registers every feature
  dynamically and only reads static server capabilities, so the server forces static registration
  for all of them (previously only semantic tokens). Hover and signature documentation are sent as
  plain text to clients that do not accept Markdown (VS).

## Unreleased

- Syntax highlighting: a TextMate injection grammar colours `d-*` attributes, `{{ }}` expressions
  and function calls inside `d-on-*` handlers as soon as a file opens; once the server is running,
  semantic tokens mark known attributes/functions (`defaultLibrary`) and unknown ones (`unknown`,
  painted as invalid).
- No more false "too few arguments" warnings: `ShowWindow(name)`, `AddDate()`, `HasDataChanges()`,
  `AcceptDataChanges()`, `CreateData(key)`, `CreateGuid()` and `GetSector()` match the engine;
  `ShowWindow` with a url still requires the container did, `CreateGuid` with exactly one argument
  warns, and `FilterData` / `ExecuteInstanceFunction` now require the slots the engine reads.
- The same language server ships in a Visual Studio 2022/2026 extension (`Drapo.VisualStudio-*.vsix`,
  `.html` files), attached to the same GitHub Release.

## 0.1.0

- Initial release: diagnostics, `d-*` attribute completion, hover documentation and signature help
  for HTML, Razor and cshtml files, backed by the Drapo documentation catalog and validator.
