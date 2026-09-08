# Feature Specification: Drapo Language Server

**Feature Branch**: `003-drapo-language-server`

**Created**: 2026-09-08

**Status**: Draft

**Tracking issue**: [spadrapo/docs#385](https://github.com/spadrapo/docs/issues/385)

**Input**: User description: "Drapo language server (LSP) built on the docs catalog/validator — GitHub
issue #385. Provide a Drapo language server so editors such as VS Code get completion for d-*
attributes, hover documentation, signature help for functions inside d-on-* handlers, and diagnostics
identical to what the MCP validate_drapo tool returns today. Build it as a thin protocol layer over the
existing DrapoEngineCatalog, FunctionService/AttributeService and DrapoValidatorService: extract them
into a shared class library with no ASP.NET dependency (WebDocs references it, MCP behaviour
unchanged), add a stdio language server console app, and a minimal VS Code extension that launches
it. v1 is framework-level only; workspace-aware symbols (d-dataKey, sectors, components), expression
parsing beyond the current validator, and a Visual Studio extension shell are out of scope."

## Context

Today the only way to learn whether a piece of Drapo markup is correct is to open the documentation
site, or (for AI agents) to call the documentation server's validation tool. Editors treat every `d-*`
attribute as an opaque string: no completion, no hover help, no error markers. Meanwhile, the
documentation project already holds everything an editor would need: the authoritative list of
attributes and functions read from the real engine, the documented signature of every function, and a
validator that produces line/column diagnostics.

This feature exposes that existing knowledge to code editors through the standard editor language
protocol, so that a person editing Drapo markup gets the same feedback an AI agent gets from the
documentation server today, and both keep getting *identical* answers because they share one source.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See mistakes while editing Drapo markup (Priority: P1)

A developer opens an HTML (or Razor/cshtml) file containing Drapo markup in VS Code with the Drapo
extension installed. As they type, any problem the documentation validator would report (an attribute
the engine does not know, a function called with too few arguments, a malformed `d-for`, unbalanced
`{{ }}`) is underlined in the editor and listed in the Problems panel, with the same message the
documentation server's validation tool would return for the same text.

**Why this priority**: Diagnostics are the single highest-value feature. They turn silent runtime
failures into visible edit-time errors, and they are what AI coding assistants operating inside the
editor read back, so this story alone delivers value to both humans and agents.

**Independent Test**: Open a file containing `<div d-nope="x"></div>`; an error marker appears on
`d-nope` with an "unknown attribute" message. Fix the attribute; the marker disappears without
reopening the file. Paste the same snippet into the documentation validation tool; the message text,
severity and code match.

**Acceptance Scenarios**:

1. **Given** a file with an unknown `d-*` attribute is open, **When** the file is opened or edited,
   **Then** an error diagnostic appears at the attribute's line and column with the same message and
   code the documentation validator returns.
2. **Given** a `d-on-click` handler calls a documented function with fewer arguments than required,
   **When** the file is edited, **Then** a diagnostic appears at the call with the arity message the
   documentation validator returns.
3. **Given** a file with no problems, **When** it is opened, **Then** no diagnostics are shown.
4. **Given** a diagnostic is shown, **When** the user fixes the problem, **Then** the diagnostic is
   removed within a moment of the edit, with no manual refresh.
5. **Given** the same markup text, **When** validated by the editor and by the documentation
   server's validation tool, **Then** the two diagnostic lists are identical in count, position,
   severity, code and message.

---

### User Story 2 - Complete `d-*` attribute names (Priority: P2)

While typing inside an HTML tag, the developer types `d-` and is offered the list of every Drapo
attribute the engine supports. Selecting one inserts the attribute name. Attribute families that take a
suffix (for example the event and dynamic-attribute families) are offered as their prefix so the
developer can continue typing the suffix.

**Why this priority**: Completion removes the need to memorise or look up the attribute catalogue and
is the most visible "the editor understands Drapo" signal, second only to diagnostics.

**Independent Test**: In an HTML file type `<div d-` and trigger completion; the list contains `d-for`,
`d-if`, `d-model` and the other engine attributes, and does not contain any attribute the engine does
not recognise.

**Acceptance Scenarios**:

1. **Given** the cursor is inside a tag after a `d-` prefix, **When** completion is requested,
   **Then** every attribute the engine recognises is offered, and nothing else.
2. **Given** the cursor is in text content (outside any tag), **When** completion is requested,
   **Then** no Drapo attribute items are offered.
3. **Given** an attribute item is selected, **When** it is accepted, **Then** the full attribute name
   is inserted, replacing the partial `d-` prefix already typed.
4. **Given** an attribute has documentation, **When** its completion item is highlighted, **Then** a
   short description is shown alongside it.

---

### User Story 3 - Read documentation on hover (Priority: P2)

Hovering over a `d-*` attribute name, or over a function name inside a `d-on-*` handler, shows the
documented description for that attribute or function without leaving the editor. For functions the
hover also lists the parameters with their types and whether they are optional.

**Why this priority**: Same value as completion (no context switch to the website) but consumed after
the fact rather than during typing, so slightly less critical.

**Independent Test**: Hover over `d-for`; a tooltip shows the attribute's documented description. Hover
over `Notify` inside `d-on-click="Notify(...)"`; the tooltip shows the function description and its
parameter list.

**Acceptance Scenarios**:

1. **Given** the cursor is over a documented attribute, **When** hover is requested, **Then** the
   attribute's description from the documentation is shown.
2. **Given** the cursor is over a documented function name inside a `d-on-*` value, **When** hover
   is requested, **Then** the function description and parameter list are shown.
3. **Given** the cursor is over plain text or an unknown symbol, **When** hover is requested, **Then**
   no Drapo hover is shown (the editor's default behaviour applies).

---

### User Story 4 - Signature help while typing a function call (Priority: P3)

While typing arguments inside a function call in a `d-on-*` handler, the editor shows the function's
signature with the current parameter highlighted, along with each parameter's description, accepted
types, optionality and default value where documented.

**Why this priority**: Useful but narrower than the other stories; only applies while inside a call's
parentheses.

**Independent Test**: Type `d-on-click="UpdateSector(` and the signature popup shows the function's
parameters with the first highlighted; typing a comma advances the highlight.

**Acceptance Scenarios**:

1. **Given** the cursor is inside the parentheses of a documented function call in a `d-on-*`
   value, **When** signature help is requested, **Then** the signature is shown with the parameter at
   the cursor's argument position highlighted.
2. **Given** the cursor is inside a call to an unknown function, **When** signature help is
   requested, **Then** nothing is shown.

---

### User Story 5 - Install once, no separate runtime setup (Priority: P3)

A developer installs the Drapo extension from a package file (or a marketplace listing) and it works
immediately on their platform without them having to install a separate runtime or configure paths.

**Why this priority**: Adoption depends on frictionless install, but this is packaging rather than
language behaviour and can follow the core stories.

**Independent Test**: On a clean machine with only VS Code installed, install the extension package,
open an HTML file with a Drapo error, and see the diagnostic within a few seconds.

**Acceptance Scenarios**:

1. **Given** a machine without any developer runtime beyond the editor, **When** the extension is
   installed and an HTML file is opened, **Then** the language features activate without further
   configuration.
2. **Given** the server cannot start, **When** the extension activates, **Then** the editor shows a
   clear error message naming the cause rather than failing silently.

---

### Edge Cases

- **Non-Drapo files**: HTML files with no `d-*` attributes at all produce no diagnostics and no
  Drapo completions outside a `d-` prefix; the extension must not degrade editing ordinary HTML.
- **Large files**: a file of several thousand lines must still be validated on each edit without the
  editor becoming unresponsive.
- **Partial or invalid markup**: the developer is mid-edit and the document is not well-formed; the
  server must return best-effort results and never crash or stop serving later requests.
- **Case variations**: the engine matches attribute names case-insensitively; `D-For` must be
  accepted where `d-for` is, exactly as the documentation validator does today.
- **Dynamic attribute families**: `d-on-*`, `d-attr-*`, `d-validation-*`, `d-dataproperty-*` must
  not be flagged as unknown, exactly as the documentation validator handles them today.
- **Unknown function inside a handler**: flagged as the documentation validator flags it; no hover or
  signature help offered.
- **Documentation content changes**: when the documentation for a function changes (for example a
  parameter is added), the editor features reflect the change after the extension is rebuilt from the
  updated documentation. No live network dependency on the documentation website is required.
- **Multiple editors / multiple files**: each open document is validated independently; closing a
  document clears its diagnostics.

## Requirements *(mandatory)*

### Functional Requirements

**Parity with the documentation server**

- **FR-001**: The editor features MUST derive attribute and function knowledge from the same source
  the documentation server uses (the bundled engine and the documented function signatures), never
  from a separately maintained list.
- **FR-002**: For any given text, the diagnostics produced for the editor MUST be identical (count,
  line, column, severity, code, message) to those the documentation server's validation tool returns.
- **FR-003**: The documentation website and its AI-facing tools MUST continue to behave exactly as
  before this feature; no user-visible or agent-visible change to their output.
- **FR-004**: The shared knowledge component MUST be usable without the web application running, so
  that the editor integration works offline and on a machine that has never opened the docs site.

**Diagnostics (User Story 1)**

- **FR-005**: The editor MUST show diagnostics for HTML, Razor and cshtml documents when they are
  opened, changed and saved, and MUST clear them when the document is closed.
- **FR-006**: Diagnostics MUST be positioned at the exact line and column the documentation validator
  reports, and MUST carry the validator's severity (error/warning), code and message.

**Completion (User Story 2)**

- **FR-007**: When the cursor is inside a tag, the editor MUST offer every attribute the engine
  recognises as a completion item, with a short description where documented.
- **FR-008**: Attribute families defined by prefix (events, dynamic attributes, validations, data
  properties) MUST be offered as their prefix so the developer can continue typing the suffix.
- **FR-009**: Completion MUST NOT offer Drapo attributes when the cursor is outside a tag.

**Hover (User Story 3)**

- **FR-010**: Hovering an attribute name MUST show the attribute's documented description.
- **FR-011**: Hovering a function name inside a `d-on-*` value MUST show the function's documented
  description and parameter list (name, types, optional flag, default value when present).

**Signature help (User Story 4)**

- **FR-012**: Inside the parentheses of a documented function call in a `d-on-*` value, the editor
  MUST show the function signature with the active parameter highlighted, based on the comma count
  before the cursor.

**Editor integration (User Story 5)**

- **FR-013**: The extension MUST activate for HTML, Razor and cshtml files and start the language
  service automatically, with no per-user configuration required.
- **FR-014**: The extension package MUST include everything needed to run on each supported platform
  (Windows, macOS, Linux) without requiring a separately installed runtime.
- **FR-015**: If the language service fails to start, the extension MUST surface an actionable error
  to the user.

**Robustness**

- **FR-016**: The language service MUST never terminate because of malformed or incomplete document
  content; each request is answered on a best-effort basis.

### Key Entities

- **Attribute**: a Drapo `d-*` attribute recognised by the bundled engine. Has a name, optional
  documentation description, and may be a fixed name or a prefix family.
- **Function**: a Drapo function recognised by the engine and documented with a description and an
  ordered list of Parameters.
- **Parameter**: name, description, accepted types, optional flag, optional default value.
- **Diagnostic**: a problem found in a document: line, column, severity, code, message. The same
  shape is produced for the documentation server and for the editor.
- **Document**: the text of one open editor file, identified by its location, validated independently.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For a corpus of at least 20 Drapo markup samples (including every documented function
  sample), the editor diagnostics and the documentation server's validation output are identical in
  100% of cases.
- **SC-002**: After an edit, updated diagnostics appear in the editor within 500 ms for files up to
  2,000 lines.
- **SC-003**: Completion after typing `d-` lists 100% of the attributes the engine recognises and 0
  attributes it does not.
- **SC-004**: A developer on a clean machine can install the extension and see their first Drapo
  diagnostic in under 2 minutes with no configuration steps.
- **SC-005**: The documentation site's existing automated checks and manual smoke test (site renders,
  AI-facing tools answer) pass unchanged after the feature is merged.
- **SC-006**: The language service handles a corpus of intentionally malformed documents (unclosed
  tags, unbalanced quotes, truncated files) without a single crash.

## Scope

### In scope (v1)

- Diagnostics, attribute completion, hover, and signature help based on framework-level knowledge
  (engine attribute/function sets and documented signatures).
- A shared knowledge component reused by both the documentation server and the language service.
- A language service that speaks the standard editor language protocol over standard input/output.
- A minimal VS Code extension that launches the service for HTML, Razor and cshtml files.

### Out of scope (deferred)

- Workspace-aware symbols: resolving `d-dataKey` names, sectors and components across a project
  (today served by the `drapo-resolver` skill).
- Expression parsing or semantic checks beyond what the current documentation validator performs.
- A Visual Studio (full IDE) extension shell; the language service itself will be reusable by one.
- Marketplace publication process and signing (the deliverable is an installable package; listing
  is a follow-up).
- Formatting, refactoring, go-to-definition, rename.

## Assumptions

- The bundled engine and the documented function signatures in this repository remain the single
  source of truth (constitution Principle I); the editor features are a projection of them, not a
  new catalogue.
- Documentation content is packaged with the extension at build time; there is no runtime dependency
  on the documentation website. A content change requires rebuilding the extension.
- "Identical diagnostics" means the editor and the documentation server share one validation
  implementation; parity is guaranteed by construction and verified by a test corpus.
- Target editor for v1 is VS Code; the language service is editor-agnostic so other clients can be
  added later without changing it.
- Supported platforms are Windows, macOS and Linux on 64-bit architectures currently supported by
  the project's runtime.
- The existing documentation validator's known limitations (text-pattern based, no expression
  parser) carry over unchanged; improving the validator is a separate feature that would then benefit
  both consumers automatically.
- Users of the extension are developers writing Drapo applications; no authentication, telemetry or
  network access is required or added.
