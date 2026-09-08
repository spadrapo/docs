import * as fs from "fs";
import * as os from "os";
import * as path from "path";
import { runTests } from "@vscode/test-electron";

/**
 * Launches a real VS Code (downloaded once into .vscode-test/) with this extension loaded from
 * source, opens a fixture workspace and runs test/suite/index.ts inside the extension host.
 *
 * The server binary comes from DRAPO_SERVER_PATH or, by default, the Debug build of
 * ../Drapo.LanguageServer (run `dotnet build ../Drapo.LanguageServer` first).
 */
async function main(): Promise<void> {
  const extensionDevelopmentPath = path.resolve(__dirname, "..", "..");
  const extensionTestsPath = path.resolve(__dirname, "suite", "index.js");

  const exe = process.platform === "win32" ? "Drapo.LanguageServer.exe" : "Drapo.LanguageServer";
  // DRAPO_TEST_BUNDLED=1 exercises the bundled server under server/<rid>/ (as a packaged VSIX
  // would) instead of pinning drapo.server.path to the Debug build.
  const useBundled = process.env.DRAPO_TEST_BUNDLED === "1";
  const serverPath =
    process.env.DRAPO_SERVER_PATH ??
    path.resolve(extensionDevelopmentPath, "..", "Drapo.LanguageServer", "bin", "Debug", "net8.0", exe);
  if (!useBundled && !fs.existsSync(serverPath)) {
    throw new Error(`Language server not found at ${serverPath}. Build it or set DRAPO_SERVER_PATH.`);
  }

  // Fixture workspace: one Drapo file with two known problems, and the server path pinned.
  const workspace = fs.mkdtempSync(path.join(os.tmpdir(), "vscode-drapo-test-"));
  fs.mkdirSync(path.join(workspace, ".vscode"));
  fs.writeFileSync(
    path.join(workspace, ".vscode", "settings.json"),
    JSON.stringify(useBundled ? {} : { "drapo.server.path": serverPath }, null, 2)
  );
  fs.writeFileSync(
    path.join(workspace, "smoke.html"),
    '<div d-nope="x"></div>\n<button d-on-click="UpdateSector()">Go</button>\n<ul d-for="item in {{items}}"></ul>\n'
  );

  await runTests({
    extensionDevelopmentPath,
    extensionTestsPath,
    launchArgs: [workspace, "--disable-extensions", "--disable-workspace-trust"],
  });
}

main().catch((err) => {
  console.error("Extension tests failed:", err);
  process.exit(1);
});
