---
name: drapo-resolver
description: Project-aware resolver for any Drapo application. Answers go-to-definition / find-references for app-specific Drapo symbols — d-dataKeys, components, and sectors — and scans for dangling references. Use before trusting that a binding key, component tag, or sector name actually exists in this app (the shared Drapo MCP only knows the framework, not your app's symbols).
license: MIT
---

# Drapo Resolver Skill

A **generic** Drapo skill: it indexes any Drapo app's frontend (`wwwroot/app` + `wwwroot/components`) and answers structured questions the shared Drapo docs MCP cannot — *which* `d-dataKey`s / components / sectors **this app** actually defined, and where they are used.

> **Install** (hosted in `spadrapo/docs`):
> - **Any agent** (Copilot CLI / Claude Code / Cursor), via the cross-agent installer: `gh skill install spadrapo/docs drapo-resolver --agent github-copilot` (swap `--agent` for `claude-code` or `cursor`).
> - **Claude Code**, as a marketplace plugin: `/plugin marketplace add spadrapo/docs` then `/plugin install drapo-resolver@drapo`.

## Drapo know-how bundled with this skill

Beyond the resolver commands, this skill ships agnostic Drapo guidance — read the relevant file when working on Drapo:

- **[`reference/using-drapo-correctly.md`](reference/using-drapo-correctly.md)** — how to write Drapo that works: storage & data loading, data binding, rendering (`d-for`/`d-if`), sectors & navigation, components, events, validation/formatting/live data, and do/don't rules. Read it **before writing or editing Drapo markup**.
- **[`reference/troubleshooting.md`](reference/troubleshooting.md)** — a symptom → cause → confirm → fix playbook (binding shows literal `{{}}`, data not loading, `d-for` empty, component/sector not rendering, handler does nothing, …), built around inspecting the running app (`window.drapo.Introspection.GetSnapshot()`), the resolver, and `validate_drapo`. Read it **when something doesn't work**.

These complement the live Drapo MCP (the exhaustive framework reference); see the division-of-labor table below.

## When to use this skill

- Before binding to a `d-dataKey`, using a `<d-component>`, or targeting a `d-sector` you didn't just author — confirm it exists (catches plausible-but-wrong guesses).
- After editing Drapo HTML — run `scan-unresolved` to catch component tags that point at no component.
- Renaming/refactoring a data key — use `find-references` to find every binding site first.

## Recommended workflow

Treat this skill as a correctness gate around any Drapo edit:

1. **Before binding** — `resolve-datakey <key>` / `resolve-component <tag>` / `resolve-sector <name>`. If it returns `found:false`, you're about to bind to something that doesn't exist — define it or fix the name instead of guessing.
2. **After editing markup** — run `scan-unresolved`; every `unresolvedComponents` entry is a real dangling tag to fix. Review `unresolvedSectorCandidates`.
3. **Before renaming a key/sector** — `find-references` first, then update every site it lists.
4. **Don't claim an edit is correct** until the symbols you used resolve and `scan-unresolved` is clean.

## Works with the Drapo MCP (division of labor)

This skill and the shared Drapo MCP are complementary — the **MCP knows the framework, this skill knows your app**. Use both:

| Question | Use |
|----------|-----|
| Does this `d-*` attribute / function exist? What parameters does it take? | Drapo MCP — `get_attributes`, `get_functions`, `get_*_details` |
| Is this snippet's Drapo syntax valid (unknown attrs/functions, arity, `d-for`, mustaches)? | Drapo MCP — `validate_drapo` |
| How does a Drapo concept work (sectors, data binding, pipes, routing…)? | Drapo MCP — `get_concepts` / `get_concept_details` |
| Does **this app's** `d-dataKey` / component / sector exist, and where? | **This skill** — `resolve-*` |
| Everywhere a key is bound (for a safe rename) | **This skill** — `find-references` |

Pair `validate_drapo` (*is the syntax legal?*) with `resolve-*` (*do the names I used actually exist in this app?*) for a complete check before finishing a Drapo edit.

## Usage

The resolver is a PowerShell script (`drapo-resolver.ps1`) that ships **in this skill's own folder**. Invoke it with `pwsh`, pointing at wherever this skill was installed:

```
pwsh "<this-skill-dir>/drapo-resolver.ps1" <command> [name] [-Root <wwwroot>]
```

- Installed as a Claude Code **plugin**: `pwsh "${CLAUDE_PLUGIN_ROOT}/skills/drapo-resolver/drapo-resolver.ps1" ...`
- Installed via **`gh skill install`** (Copilot/Cursor/Claude): the script sits next to `SKILL.md` in the agent's skills directory (e.g. `.github/skills/drapo-resolver/` or `~/.copilot/skills/drapo-resolver/`).

`-Root` is optional: if omitted, the script auto-detects the Drapo `wwwroot` (a `wwwroot` folder containing `app/`) under the current directory. Pass it explicitly if the repo has more than one. All output is JSON.

### Commands

| Command | Example | Returns |
|---------|---------|---------|
| `resolve-datakey <key>` | `resolve-datakey myDataKey` | declaration site(s): `file`, `line`, `dataType`, `dataUrlGet`, `dataUrlSet`, `dataValue`. `found:false` if undefined. |
| `resolve-component <tag>` | `resolve-component d-mycomponent` | the component `folder` and its `files`, or `found:false`. Accepts `d-foo` or `foo`. |
| `resolve-sector <name>` | `resolve-sector myContentSector` | declaration site(s) of the `d-sector`. |
| `find-references <key>` | `find-references myDataKey` | every `declaration` (`d-dataKey=`) and `reference` (mustache / `d-*` attribute) site, with `file`/`line`/`text`. |
| `scan-unresolved` | `scan-unresolved` | `unresolvedComponents` (high-confidence: a `<d-x>` tag with no `wwwroot/components/x` folder) and `unresolvedSectorCandidates` (`UpdateSector()` targets with no static `d-sector` declaration — review, not errors). |

### Examples

```bash
# (paths below use the skill's own folder — see Usage above)

# Does this data key exist, and what is it?
pwsh ./drapo-resolver.ps1 resolve-datakey myDataKey

# Where is a component defined?
pwsh ./drapo-resolver.ps1 resolve-component d-mycomponent

# Find everything that binds a key before renaming it.
pwsh ./drapo-resolver.ps1 find-references myDataKey

# Catch component tags that point at no component.
pwsh ./drapo-resolver.ps1 scan-unresolved
```

## Notes & limitations

- **Components** resolve to `wwwroot/components/<name>` (tag `d-<name>`). An app that registers components from additional paths may produce `unresolvedComponents` that are actually valid — treat them as candidates to verify.
- **Sectors and data keys** can be declared in dynamically loaded or parent content, so `unresolvedSectorCandidates` is intentionally a *candidate* list, not an error list. Use `resolve-*` / `find-references` to check a specific symbol with confidence.
- **Routes**: Drapo apps that navigate via `UpdateSector` (rather than client `CreateRoute`) have no separate route table to resolve; sector resolution covers those targets.
- Parsing is convention-based (regex over `d-*` attributes), consistent with the docs-side `validate_drapo` tool.
