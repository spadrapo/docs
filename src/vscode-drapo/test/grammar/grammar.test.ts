import * as assert from "assert";
import * as fs from "fs";
import * as path from "path";
import { downloadAndUnzipVSCode } from "@vscode/test-electron";
import * as oniguruma from "vscode-oniguruma";
import * as textmate from "vscode-textmate";

/**
 * Tokenizes a fixture with the real HTML grammar shipped in VS Code plus the Drapo injection
 * grammar, and asserts the scopes (FR-017/FR-018/FR-023). Runs under plain Node: no editor
 * process. The VS Code build is the one `npm test` downloads into .vscode-test/ (cached).
 */
const INJECTION = path.resolve(__dirname, "..", "..", "..", "syntaxes", "drapo.injection.json");

interface Token {
  text: string;
  scopes: string[];
}

async function createRegistry(): Promise<textmate.Registry> {
  const vscodeExe = await downloadAndUnzipVSCode();
  // The layout differs per platform (win: <dir>/<hash>/resources/app, mac: Contents/Resources/app),
  // so look for the html extension folder below the install directory.
  const extensions = findExtensionsDir(path.dirname(vscodeExe), 4);
  assert.ok(extensions, `resources/app/extensions not found below ${path.dirname(vscodeExe)}`);
  const htmlGrammar = path.join(extensions, "html", "syntaxes", "html.tmLanguage.json");
  const derivativeGrammar = path.join(extensions, "html", "syntaxes", "html-derivative.tmLanguage.json");
  assert.ok(fs.existsSync(htmlGrammar), `HTML grammar not found at ${htmlGrammar}`);

  const wasm = fs.readFileSync(path.join(require.resolve("vscode-oniguruma"), "..", "onig.wasm")).buffer;
  await oniguruma.loadWASM(wasm);
  const onigLib = Promise.resolve({
    createOnigScanner: (patterns: string[]) => new oniguruma.OnigScanner(patterns),
    createOnigString: (s: string) => new oniguruma.OnigString(s),
  });

  const injection = JSON.parse(fs.readFileSync(INJECTION, "utf8"));
  const grammars: Record<string, string> = {
    "text.html.basic": htmlGrammar,
    "text.html.derivative": derivativeGrammar,
  };
  return new textmate.Registry({
    onigLib,
    loadGrammar: async (scopeName) => {
      if (scopeName === injection.scopeName) {
        return textmate.parseRawGrammar(JSON.stringify(injection), INJECTION);
      }
      const file = grammars[scopeName];
      if (file && fs.existsSync(file)) {
        return textmate.parseRawGrammar(fs.readFileSync(file, "utf8"), file);
      }
      return null; // embedded languages (js/css) are not needed for these assertions
    },
    getInjections: (scopeName) => (scopeName === "text.html.basic" || scopeName === "text.html.derivative" ? [injection.scopeName] : []),
  });
}

function findExtensionsDir(dir: string, depth: number): string | undefined {
  const candidate = path.join(dir, "resources", "app", "extensions");
  if (fs.existsSync(path.join(candidate, "html"))) {
    return candidate;
  }
  const mac = path.join(dir, "..", "Resources", "app", "extensions");
  if (fs.existsSync(path.join(mac, "html"))) {
    return path.resolve(mac);
  }
  if (depth === 0) {
    return undefined;
  }
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    if (entry.isDirectory()) {
      const found = findExtensionsDir(path.join(dir, entry.name), depth - 1);
      if (found) {
        return found;
      }
    }
  }
  return undefined;
}

function tokenize(grammar: textmate.IGrammar, text: string): Token[][] {
  const lines: Token[][] = [];
  let state = textmate.INITIAL;
  for (const line of text.split("\n")) {
    const result = grammar.tokenizeLine(line, state);
    lines.push(result.tokens.map((t) => ({ text: line.substring(t.startIndex, t.endIndex), scopes: t.scopes })));
    state = result.ruleStack;
  }
  return lines;
}

/** The token whose text is exactly `text` on the given line. */
function find(tokens: Token[][], line: number, text: string): Token {
  const token = tokens[line].find((t) => t.text === text);
  assert.ok(token, `token '${text}' not found on line ${line}: ${JSON.stringify(tokens[line].map((t) => t.text))}`);
  return token;
}

function hasScope(token: Token, scope: string): boolean {
  return token.scopes.some((s) => s === scope || s.startsWith(scope + "."));
}

async function main(): Promise<void> {
  const registry = await createRegistry();
  const grammar = await registry.loadGrammar("text.html.basic");
  assert.ok(grammar, "text.html.basic did not load");

  const fixture = [
    '<div class="card" d-if="{{item.visible}}" d-router>',
    '  <button d-on-click="UpdateSector(content,~/app/x.html);ShowWindow({{w.Name}})">Go</button>',
    "  <span>Hello {{item.name}}</span>",
    '  <a href="/x" title="plain">no drapo here</a>',
    "</div>",
  ].join("\n");
  const tokens = tokenize(grammar, fixture);

  // 1. d-* attribute names get the Drapo scopes; a plain attribute keeps the HTML scope only.
  const dIf = find(tokens, 0, "d-if");
  assert.ok(hasScope(dIf, "entity.other.attribute-name.drapo"), JSON.stringify(dIf.scopes));
  assert.ok(hasScope(dIf, "keyword.other.drapo"), JSON.stringify(dIf.scopes));
  const dRouter = find(tokens, 0, "d-router");
  assert.ok(hasScope(dRouter, "entity.other.attribute-name.drapo"), JSON.stringify(dRouter.scopes));
  const cls = find(tokens, 0, "class");
  assert.ok(hasScope(cls, "entity.other.attribute-name.html"), JSON.stringify(cls.scopes));
  assert.ok(!cls.scopes.some((s) => s.includes("drapo")), `plain attribute got a Drapo scope: ${JSON.stringify(cls.scopes)}`);

  // 2. Mustaches: in an attribute value, in text, and inside a handler argument.
  for (const [line, text] of [
    [0, "item.visible"],
    [2, "item.name"],
    [1, "w.Name"],
  ] as const) {
    const t = find(tokens, line, text);
    assert.ok(hasScope(t, "variable.other.mustache.drapo"), `${text}: ${JSON.stringify(t.scopes)}`);
  }
  const open = tokens[2].find((t) => t.text === "{{");
  assert.ok(open && hasScope(open, "punctuation.definition.template-expression.begin.drapo"), JSON.stringify(tokens[2]));

  // 3. Handler: attribute name, function names, parentheses, separators.
  const onClick = find(tokens, 1, "d-on-click");
  assert.ok(hasScope(onClick, "entity.other.attribute-name.drapo.handler"), JSON.stringify(onClick.scopes));
  for (const name of ["UpdateSector", "ShowWindow"]) {
    const t = find(tokens, 1, name);
    assert.ok(hasScope(t, "entity.name.function.drapo"), `${name}: ${JSON.stringify(t.scopes)}`);
  }
  const comma = tokens[1].find((t) => t.text === ",");
  assert.ok(comma && hasScope(comma, "punctuation.separator.arguments.drapo"), JSON.stringify(tokens[1]));
  const semicolon = tokens[1].find((t) => t.text === ";");
  assert.ok(semicolon && hasScope(semicolon, "punctuation.terminator.statement.drapo"), JSON.stringify(tokens[1]));

  // 4. FR-018: a line without Drapo markup carries no Drapo scope at all.
  for (const t of tokens[3]) {
    assert.ok(!t.scopes.some((s) => s.includes("drapo")), `unexpected Drapo scope on '${t.text}': ${JSON.stringify(t.scopes)}`);
  }
  // and the tag structure survives the injection (the closing tag is still a tag).
  const closing = find(tokens, 4, "div");
  assert.ok(hasScope(closing, "entity.name.tag"), JSON.stringify(closing.scopes));

  console.log("vscode-drapo grammar test: all assertions passed");
}

main().catch((err) => {
  console.error("Grammar test failed:", err);
  process.exit(1);
});
