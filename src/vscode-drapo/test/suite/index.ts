import * as assert from "assert";
import * as path from "path";
import * as vscode from "vscode";

/**
 * Runs inside the VS Code extension host. Proves the user-visible behaviour of the extension end
 * to end: the bundled/configured server starts, diagnostics reach the editor, completion and
 * hover answer, and fixing the problem clears the diagnostic. No test framework needed.
 */
export async function run(): Promise<void> {
  try {
    await runChecks();
  } catch (err) {
    const message = err instanceof Error ? `${err.message}
${err.stack ?? ""}` : String(err);
    console.log("vscode-drapo integration test FAILED: " + message);
    throw err;
  }
}

async function runChecks(): Promise<void> {
  const folder = vscode.workspace.workspaceFolders?.[0];
  assert.ok(folder, "fixture workspace not opened");
  const uri = vscode.Uri.file(path.join(folder.uri.fsPath, "smoke.html"));

  const document = await vscode.workspace.openTextDocument(uri);
  await vscode.window.showTextDocument(document);

  // 1. Diagnostics arrive (server start + didOpen + publishDiagnostics).
  const diagnostics = await waitFor(
    () => vscode.languages.getDiagnostics(uri).filter((d) => d.source === "drapo"),
    (list) => list.length >= 2,
    60_000, // first launch of a self-contained server can be slow on cold machines
    "drapo diagnostics"
  );
  const codes = diagnostics.map((d) => String(typeof d.code === "object" ? d.code?.value : d.code)).sort();
  assert.deepStrictEqual(codes, ["unknown-attribute", "wrong-arity"]);
  const unknown = diagnostics.find((d) => String(d.code) === "unknown-attribute")!;
  assert.strictEqual(unknown.severity, vscode.DiagnosticSeverity.Error);
  assert.strictEqual(unknown.range.start.line, 0);
  assert.strictEqual(unknown.range.start.character, 5);
  assert.strictEqual(unknown.range.end.character, 11);
  assert.ok(unknown.message.includes("d-nope"), unknown.message);
  const arity = diagnostics.find((d) => String(d.code) === "wrong-arity")!;
  assert.strictEqual(arity.severity, vscode.DiagnosticSeverity.Warning);
  assert.strictEqual(arity.range.start.line, 1);

  // 2. Completion inside a tag lists engine attributes.
  const completions = await vscode.commands.executeCommand<vscode.CompletionList>(
    "vscode.executeCompletionItemProvider",
    uri,
    new vscode.Position(0, 7), // inside "d-nope"
    "-"
  );
  const labels = completions.items.map((i) => (typeof i.label === "string" ? i.label : i.label.label));
  for (const expected of ["d-for", "d-if", "d-model", "d-on-"]) {
    assert.ok(labels.includes(expected), `completion missing ${expected}`);
  }

  // 3. Hover on d-for shows documentation.
  const hovers = await vscode.commands.executeCommand<vscode.Hover[]>(
    "vscode.executeHoverProvider",
    uri,
    new vscode.Position(2, 6) // inside "d-for"
  );
  const hoverText = hovers
    .flatMap((h) => h.contents)
    .map((c) => (typeof c === "string" ? c : c.value))
    .join("\n");
  assert.ok(hoverText.includes("**d-for**"), `unexpected hover: ${hoverText}`);

  // 4. Signature help inside the UpdateSector call.
  const help = await vscode.commands.executeCommand<vscode.SignatureHelp>(
    "vscode.executeSignatureHelpProvider",
    uri,
    new vscode.Position(1, 33) // right after "UpdateSector("
  );
  assert.ok(help && help.signatures.length === 1, "signature help missing");
  assert.ok(help.signatures[0].label.startsWith("UpdateSector("), help.signatures[0].label);
  assert.strictEqual(help.activeParameter, 0);

  // 5. Semantic tokens: the server's legend and one token per Drapo symbol (FR-019/FR-023).
  const legend = await vscode.commands.executeCommand<vscode.SemanticTokensLegend>(
    "vscode.provideDocumentSemanticTokensLegend",
    uri
  );
  assert.ok(legend, "semantic tokens legend missing (server did not register the provider)");
  assert.deepStrictEqual(legend.tokenTypes, ["keyword", "function", "variable"]);
  assert.deepStrictEqual(legend.tokenModifiers, ["defaultLibrary", "unknown"]);
  const semantic = await waitFor(
    () => vscode.commands.executeCommand<vscode.SemanticTokens>("vscode.provideDocumentSemanticTokens", uri),
    (t) => !!t,
    10_000,
    "semantic tokens"
  );
  const data = Array.from(semantic!.data);
  // [deltaLine, deltaStart, length, type, modifiers] per token; d-nope is keyword.unknown (type 0, bit 2).
  assert.deepStrictEqual(data.slice(0, 5), [0, 5, 6, 0, 2], JSON.stringify(data));
  // UpdateSector (line 1) is function.defaultLibrary; {{items}} (line 2) is a variable.
  assert.ok(chunks(data).some((t) => t[2] === 12 && t[3] === 1 && t[4] === 1), "UpdateSector token missing: " + JSON.stringify(data));
  assert.ok(chunks(data).some((t) => t[2] === 9 && t[3] === 2), "{{items}} token missing: " + JSON.stringify(data));

  // 6. Fixing the attribute clears its diagnostic without reopening.
  const editor = vscode.window.activeTextEditor!;
  await editor.edit((b) => b.replace(new vscode.Range(0, 5, 0, 11), "d-if"));
  await waitFor(
    () => vscode.languages.getDiagnostics(uri).filter((d) => d.source === "drapo"),
    (list) => list.length === 1 && String(list[0].code) === "wrong-arity",
    10_000,
    "diagnostic cleared after fix"
  );

  console.log("vscode-drapo integration test: all assertions passed");
}

function chunks(data: number[]): number[][] {
  const out: number[][] = [];
  for (let i = 0; i + 4 < data.length; i += 5) {
    out.push(data.slice(i, i + 5));
  }
  return out;
}

async function waitFor<T>(
  probe: () => T | Thenable<T>,
  done: (value: T) => boolean,
  timeoutMs: number,
  what: string
): Promise<T> {
  const start = Date.now();
  let last: T = await probe();
  while (!done(last)) {
    if (Date.now() - start > timeoutMs) {
      throw new Error(`Timed out waiting for ${what}: ${JSON.stringify(last)}`);
    }
    await new Promise((r) => setTimeout(r, 200));
    last = await probe();
  }
  return last;
}
