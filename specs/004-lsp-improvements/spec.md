# Feature Specification: LSP Improvements — Function Arity Accuracy, Visual Studio Release, Syntax Highlighting

**Feature Branch**: `004-lsp-improvements`

**Created**: 2026-09-09

**Status**: Draft

**Tracking issue**: [#389](https://github.com/spadrapo/docs/issues/389) (follow-up to #385, feature `003-drapo-language-server`)

**Input**: User description: "LSP improvements (issue #389): (1) fix the false wrong-arity warning for ShowWindow, whose second parameter `did` is only required when the first argument is a URL, and audit every documented function's required parameter count against the bundled drapo.js engine, fixing all false "too few arguments" warnings; (2) ship a Visual Studio (2022 17.x and 2026 18.x) extension that hosts Drapo.LanguageServer through the built-in VS LSP client for .html files, built in the same CI/release workflow as the VS Code extension so one lsp-v* tag attaches both packages to one GitHub Release; (3) add syntax highlighting: a TextMate injection grammar in the VS Code extension for d-* attributes, {{mustache}} expressions and handler function calls, plus LSP semantic tokens from the server (known attribute, known function, mustache, unknown) for VS Code and Visual Studio."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - No false "too few arguments" warnings (Priority: P1)

A Drapo developer writes `ShowWindow(myWindow)` (a window definition name, no target element)
in a `d-on-click` handler. Today the editor underlines the call with *"Function 'ShowWindow'
expects at least 2 argument(s) but got 1"*, the docs' `validate_drapo` tool reports the same
warning, and the function reference page shows `did` as mandatory. The engine actually runs this
call without complaint: the second argument is only read when the first one is a URL.

The developer expects the editor, the documentation page and the validation tool to agree with the
engine: a call the engine accepts must not be flagged, and a call the engine cannot run (a URL
without a target) must still be flagged.

Because the same mechanism (the documented required-parameter count) drives the warning for every
function, the fix must cover the whole function catalog, not just `ShowWindow`: every documented
function's required-parameter count is compared against what the engine actually demands, and
every mismatch is corrected.

**Why this priority**: A false warning erodes trust in the whole tool; developers learn to ignore
the Problems panel. This is also the constitution's non-negotiable Principle I (accuracy over the
real engine) applied to the function catalog.

**Independent Test**: Open an HTML file containing `ShowWindow(myWindow)` and
`ShowWindow(~/app/x.html)` in the editor: the first produces no diagnostic, the second still warns
about a missing target. Run the automated parity corpus: every function sample and every audited
call shape passes with no `wrong-arity` diagnostic. The audit report lists every function whose
required count changed, with the engine evidence.

**Acceptance Scenarios**:

1. **Given** an HTML file with `d-on-click="ShowWindow(myWindow)"`, **When** diagnostics are
   computed, **Then** no diagnostic is produced.
2. **Given** an HTML file with `d-on-click="ShowWindow(~/app/shared/window.html)"`, **When**
   diagnostics are computed, **Then** a `wrong-arity` warning explains that a URL requires a target
   element.
3. **Given** the ShowWindow reference page, **When** a reader looks at the parameter table, **Then**
   `did` is marked optional, its description states when it is required, and a sample shows the
   window-definition form.
4. **Given** the complete function catalog, **When** each documented required-parameter count is
   compared with the engine's behaviour, **Then** every function's documented minimum equals the
   number of arguments the engine needs to run without error, and each correction has an automated
   test guarding it.
5. **Given** the hover and signature help for an audited function, **When** displayed, **Then**
   optional parameters are shown as optional, consistent with the diagnostics.

---

### User Story 2 - Install the Drapo language support in Visual Studio (Priority: P2)

A Drapo developer who works in Visual Studio (2022 or 2026) downloads a single installer package
from the same GitHub Release that already carries the VS Code packages, installs it, opens an
`.html` file and gets the same diagnostics, completion, hover and signature help that VS Code users
get today, from the same server and the same catalog. No separate runtime installation is needed.

The maintainer releases both editors' packages with the one-click "Release extension" action they
already use: one version, one tag, one GitHub Release listing every package.

**Why this priority**: Drapo is a .NET framework; a large share of its developers live in Visual
Studio, not VS Code. Without this, the language server only reaches half the audience.

**Independent Test**: Trigger the release action with a test version; the resulting GitHub Release
contains the four VS Code packages and one Visual Studio package. Install the Visual Studio package
on a machine with VS 2022 and VS 2026, open an `.html` file with `<div d-nope="x">`: `d-nope` is
underlined and hovering `d-if` shows its documentation.

**Acceptance Scenarios**:

1. **Given** the release action is run for version X, **When** it completes, **Then** the GitHub
   Release `lsp-vX` contains the VS Code packages *and* the Visual Studio package, and the release
   notes list all of them with the editor each targets.
2. **Given** the Visual Studio package is installed on VS 2022 (17.x) or VS 2026 (18.x), **When** an
   `.html` file containing an unknown `d-*` attribute is opened, **Then** the attribute is
   underlined with the same message the VS Code extension shows.
3. **Given** the Visual Studio extension is installed, **When** the developer types `d-` inside a
   tag in an `.html` file, **Then** completion offers the Drapo attributes.
4. **Given** the Visual Studio extension is installed, **When** a `.cshtml` or `.razor` file is
   opened, **Then** the extension does nothing (Razor files are out of scope for Visual Studio in
   this release).
5. **Given** a machine without any .NET runtime installed, **When** the Visual Studio package is
   installed, **Then** the language server starts and works.
6. **Given** any push or pull request, **When** CI runs, **Then** the Visual Studio extension is
   built (not only on release tags) so a broken build is caught before a release.

---

### User Story 3 - Drapo markup is visibly distinct in the editor (Priority: P3)

A Drapo developer opens an HTML file in VS Code and immediately sees Drapo markup stand out from
plain HTML: `d-*` attribute names are coloured differently from ordinary attributes, `{{ }}`
expressions are coloured inside text and attribute values, and function calls inside handler
attributes (`d-on-*`) show the function name and its parentheses distinctly. This works the
instant the file opens, before any language server has started.

Once the language server is running, colours become catalog-aware in both VS Code and Visual
Studio: a known attribute, a known function, a mustache expression and an unknown symbol each get
their own colour so an unknown function name looks different from a real one even before the
diagnostic appears.

**Why this priority**: Highlighting is the most visible everyday benefit of editor support and is
independent of the other two stories, but it does not fix incorrect behaviour (Story 1) nor
unlock a new audience (Story 2).

**Independent Test**: Open the fixture HTML file in VS Code with the extension: inspect the token
scopes on `d-if`, `{{item.name}}` and `ShowWindow(` and confirm each carries a Drapo-specific
scope. With the server running, request semantic tokens for the same file and confirm the known
attribute, known function, mustache and unknown symbol are classified as such, in VS Code and in
Visual Studio.

**Acceptance Scenarios**:

1. **Given** an HTML file open in VS Code with the extension installed and no server running,
   **When** the file renders, **Then** `d-*` attribute names, `{{ }}` expressions and handler
   function calls are each assigned a Drapo-specific token scope distinct from plain HTML.
2. **Given** the language server is running, **When** the editor requests semantic tokens for a
   file, **Then** every known `d-*` attribute, every known function name inside a `d-on-*` value,
   every `{{ }}` expression and every unknown attribute/function is classified with a distinct token
   type.
3. **Given** the Visual Studio extension is installed, **When** an `.html` file is opened, **Then**
   the same semantic classifications are applied (subject to the host's support for semantic
   tokens).
4. **Given** a file with no Drapo markup, **When** it is opened, **Then** highlighting of plain HTML
   is unchanged.
5. **Given** a large file (thousands of lines), **When** it is edited, **Then** highlighting keeps
   up with typing without perceptible lag.

---

### Edge Cases

- `ShowWindow` with a URL and an empty second argument (`ShowWindow(~/x.html,)`): the engine
  receives an empty target; the validator treats an explicitly empty argument as provided (the
  existing behaviour for every function) and does not warn.
- `ShowWindow` where the first argument is a mustache or a nested function call that resolves at
  runtime to a URL or a name: the validator cannot know which; it must not warn (no false
  positive) — the minimum is one argument.
- Functions whose parameter list is variadic or pairwise (`ParameterKey`/`ParameterValue` pairs):
  the audit only fixes the *minimum*; an upper bound is still never enforced.
- A function the engine accepts with zero arguments but the docs list as needing one: the audit
  marks the parameter optional and documents its default behaviour.
- A function the docs list as fully optional but the engine crashes on when called with no
  arguments: the audit marks the parameter required (the warning is correct, keep it).
- Visual Studio has an `.html` file open that is part of a Razor project: the extension still
  attaches only by file type, never by project type.
- Visual Studio starts the server before the user has any `.html` file open: the server must start
  lazily on first `.html` document and must not keep a process alive when no document is open.
- Both the VS Code extension and the Visual Studio extension are installed on the same machine:
  they bundle their own server copies and never interfere.
- Semantic tokens requested for a document that has been closed or that changed since the request:
  the server answers with the current content and never throws.
- A TextMate grammar edge: `{{` inside a `<script>` or `<style>` block, or an attribute whose
  value contains `(` but is not a `d-on-*` handler — no Drapo scope must be applied there.
- Semantic token overlap with the TextMate grammar in VS Code: semantic colours win where present,
  TextMate colours remain as the fallback; both must be consistent (a known function is never
  coloured as "unknown" by one layer and "known" by the other).

## Requirements *(mandatory)*

### Functional Requirements

**Arity accuracy**

- **FR-001**: The documented minimum number of arguments for `ShowWindow` MUST be one; the target
  element parameter MUST be documented as optional with a description that states it is required
  when the first argument is a URL and ignored when it is a window definition name.
- **FR-002**: The validator MUST NOT emit a `wrong-arity` diagnostic for `ShowWindow(<name>)` and
  MUST emit one for `ShowWindow(<url>)` when the url is a literal path (not a mustache or nested
  call) and no second argument is given, with a message that explains a URL needs a target element.
- **FR-003**: The ShowWindow reference page MUST include a sample that opens a window definition by
  name, and that sample MUST pass the validator like every other sample.
- **FR-004**: Every function in the documentation catalog MUST have its documented
  required-parameter count compared with the arguments the engine actually requires to execute the
  function without error; the comparison method and per-function verdict MUST be recorded in the
  feature's research artifact.
- **FR-005**: Every function whose documented minimum is higher than the engine's MUST be corrected
  (parameters marked optional, descriptions updated) so that no valid call is flagged; every
  function whose documented minimum is lower than the engine's MAY be corrected only when the
  engine demonstrably fails on the shorter call.
- **FR-006**: Each corrected function MUST gain an automated test asserting the previously
  false-positive call shape produces no diagnostic; the existing parity corpus MUST continue to pass.
- **FR-007**: Hover and signature help MUST display corrected parameters as optional, consistent with
  the reference page (single source of truth: the parameter file).

**Visual Studio extension**

- **FR-008**: A Visual Studio extension package MUST be produced that installs on Visual Studio 2022
  (17.x) and Visual Studio 2026 (18.x), 64-bit.
- **FR-009**: The Visual Studio extension MUST host the existing Drapo language server through the
  editor's built-in language server client, for documents of the HTML content type only.
- **FR-010**: The Visual Studio extension MUST provide diagnostics, completion, hover and signature
  help for `.html` files with the same results as the VS Code extension for the same document.
- **FR-011**: The Visual Studio extension MUST bundle a self-contained server so no .NET runtime
  installation is required on the developer machine.
- **FR-012**: The CI workflow MUST build the Visual Studio extension on every push and pull request
  and MUST package it on `lsp-v*` tags.
- **FR-013**: The release job MUST attach the Visual Studio package to the same GitHub Release as
  the VS Code packages and the release notes MUST list every package with its target editor and
  install instructions.
- **FR-014**: The "Release extension" one-click action MUST keep working unchanged from the
  maintainer's perspective (one version input, one tag, both editors released).
- **FR-015**: The Visual Studio extension version MUST equal the VS Code extension version for a
  given release tag (one version number for the whole editor-support release).
- **FR-016**: The extension README/documentation MUST describe how to install the Visual Studio
  package and which editors are supported.

**Syntax highlighting**

- **FR-017**: The VS Code extension MUST contribute an injection grammar for HTML documents that
  assigns Drapo-specific scopes to: `d-*` attribute names (including prefixed families such as
  `d-on-*`, `d-attr-*`), `{{ }}` expressions in text and in attribute values, and function names and
  parentheses inside `d-on-*` handler values.
- **FR-018**: The injection grammar MUST NOT alter the highlighting of documents or regions that
  contain no Drapo markup.
- **FR-019**: The language server MUST implement semantic tokens (full document) classifying at
  least: known Drapo attribute, unknown `d-*` attribute, known function name, unknown function name
  and mustache expression.
- **FR-020**: Semantic token classification of "known" vs "unknown" MUST come from the same catalog
  the diagnostics use, so highlighting and diagnostics never disagree.
- **FR-021**: The VS Code extension MUST map the server's token types to standard editor theme
  colours so that no theme customisation is required for the colours to appear.
- **FR-022**: The Visual Studio extension MUST advertise semantic token support to the server where
  the host supports it; where the host does not, the extension MUST still work for every other
  feature.
- **FR-023**: Semantic tokens MUST be covered by automated tests: a fixture document with the four
  classifications produces the expected token list, and the VS Code integration test asserts the
  grammar scopes and semantic tokens on the fixture.

### Key Entities

- **Function parameter definition**: the documented name, description, accepted types, optional
  flag and default value of one function parameter; the source of truth for arity checks, hover,
  signature help and the reference page.
- **Arity audit record**: for one function, the documented minimum, the engine-derived minimum,
  the evidence (how the engine reads its arguments) and the verdict (unchanged / corrected).
- **Editor package**: a release artifact for one editor and platform (VS Code × 4 platforms,
  Visual Studio × 1), sharing one version and one release tag.
- **Semantic token**: a classified span of a document (range + token type) served by the language
  server; token types: known attribute, unknown attribute, known function, unknown function,
  mustache expression.
- **Grammar scope**: a static classification of a span assigned by the editor's grammar without a
  server; must be consistent with the semantic token types.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Zero `wrong-arity` diagnostics on any call shape the engine executes successfully,
  across the full parity corpus (every documented function sample) and the audited call shapes.
- **SC-002**: 100% of documented functions have a recorded audit verdict; every correction has a
  guarding test.
- **SC-003**: One release action run produces one GitHub Release containing five editor packages
  (four VS Code, one Visual Studio); the maintainer performs no additional manual step.
- **SC-004**: A Visual Studio 2022 and a Visual Studio 2026 user can install the package and see a
  diagnostic on an unknown `d-*` attribute within 10 seconds of opening an `.html` file, with no
  runtime pre-installed.
- **SC-005**: In VS Code, Drapo attributes, mustaches and handler calls are visibly distinct from
  plain HTML immediately on open, and catalog-aware colours appear once the server is up.
- **SC-006**: All automated checks stay green: solution tests (tooling, parity corpus, language
  server), the VS Code integration test, and the Visual Studio extension build.
- **SC-007**: Highlighting introduces no perceptible typing lag on a 5,000-line HTML document.

## Assumptions

- The engine (`drapo.js` bundled in the Drapo package currently referenced by the solution) is the
  sole authority for what a function requires; the audit reads the engine source, not memory.
- "Required" for the audit means: the engine dereferences the argument unconditionally and would
  fail or misbehave without it. Arguments read behind a length check or a conditional are optional.
- When the requirement is conditional on the *value* of an earlier argument (as in `ShowWindow`),
  the documented minimum is the unconditional minimum, and the conditional case is handled by a
  targeted rule only when the condition is decidable from the literal text.
- Visual Studio support targets 64-bit VS 2022 (17.x) and VS 2026 (18.x) only; older versions are
  out of scope. Only the Windows x64 server is bundled, since Visual Studio runs only on Windows.
- Razor (`.cshtml`, `.razor`) support in Visual Studio is explicitly out of scope for this feature
  to avoid interactions with the built-in Razor language service; VS Code keeps its Razor support.
- The Visual Studio package is distributed through the GitHub Release only; publishing to the Visual
  Studio Marketplace is out of scope (mirrors the current VS Code distribution).
- Visual Studio extension builds need a Windows CI runner; the existing Linux jobs are unchanged.
- The Visual Studio extension cannot be integration-tested headlessly in CI in this feature; CI
  verifies it builds and packages, and a manual smoke test on VS 2022 and VS 2026 is part of the
  PR checklist.
- Semantic token colouring in Visual Studio depends on the host's language client supporting the
  semantic tokens capability; where unsupported, Visual Studio users still get every other feature.
- TextMate highlighting applies to the same document kinds the VS Code extension already activates
  for (HTML and Razor languages).
