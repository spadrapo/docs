import * as fs from "fs";
import * as path from "path";
import * as vscode from "vscode";
import {
  LanguageClient,
  LanguageClientOptions,
  ServerOptions,
  TransportKind,
} from "vscode-languageclient/node";

const OUTPUT_CHANNEL_NAME = "Drapo Language Server";
const SERVER_BASENAME = "Drapo.LanguageServer";

let client: LanguageClient | undefined;
let output: vscode.LogOutputChannel | undefined;

/** Maps the Node platform/arch pair onto the .NET runtime identifier the server was published for. */
function runtimeIdentifier(): string | undefined {
  const key = `${process.platform}-${process.arch}`;
  switch (key) {
    case "win32-x64":
      return "win-x64";
    case "linux-x64":
      return "linux-x64";
    case "darwin-x64":
      return "osx-x64";
    case "darwin-arm64":
      return "osx-arm64";
    default:
      return undefined;
  }
}

function serverFileName(): string {
  return process.platform === "win32" ? `${SERVER_BASENAME}.exe` : SERVER_BASENAME;
}

/**
 * Resolves the server executable: the `drapo.server.path` setting wins, otherwise the
 * platform-specific binary bundled under `server/<rid>/`.
 */
function resolveServerPath(context: vscode.ExtensionContext): { path?: string; error?: string } {
  const configured = vscode.workspace.getConfiguration("drapo").get<string>("server.path", "").trim();
  if (configured.length > 0) {
    if (fs.existsSync(configured)) {
      return { path: configured };
    }
    return { error: `Drapo language server not found at the configured path: ${configured}` };
  }

  const rid = runtimeIdentifier();
  if (!rid) {
    return {
      error:
        `Drapo language server is not bundled for ${process.platform}-${process.arch}. ` +
        "Set drapo.server.path to a Drapo.LanguageServer executable built for this platform.",
    };
  }

  const bundled = path.join(context.extensionPath, "server", rid, serverFileName());
  if (!fs.existsSync(bundled)) {
    return {
      error:
        `Drapo language server not found for ${rid} (expected ${bundled}). ` +
        "Install the platform-specific package of this extension or set drapo.server.path.",
    };
  }
  return { path: bundled };
}

function ensureExecutable(file: string): void {
  if (process.platform === "win32") {
    return;
  }
  try {
    fs.accessSync(file, fs.constants.X_OK);
  } catch {
    try {
      fs.chmodSync(file, 0o755);
    } catch (err) {
      output?.appendLine(`Could not mark ${file} executable: ${String(err)}`);
    }
  }
}

async function startClient(context: vscode.ExtensionContext): Promise<void> {
  const resolved = resolveServerPath(context);
  if (!resolved.path) {
    output?.appendLine(resolved.error ?? "Unknown error resolving the Drapo language server.");
    void vscode.window.showErrorMessage(resolved.error ?? "Drapo language server could not be located.");
    return;
  }
  ensureExecutable(resolved.path);
  output?.appendLine(`Starting ${resolved.path}`);

  const serverOptions: ServerOptions = {
    run: { command: resolved.path, transport: TransportKind.stdio },
    debug: { command: resolved.path, transport: TransportKind.stdio },
  };

  const clientOptions: LanguageClientOptions = {
    documentSelector: [
      { scheme: "file", language: "html" },
      { scheme: "untitled", language: "html" },
      { scheme: "file", language: "razor" },
      { scheme: "untitled", language: "razor" },
      { scheme: "file", language: "aspnetcorerazor" },
      { scheme: "untitled", language: "aspnetcorerazor" },
    ],
    outputChannel: output,
    outputChannelName: OUTPUT_CHANNEL_NAME,
    // Protocol traces (drapo.trace.server) go to the same persisted log channel.
    traceOutputChannel: output,
  };

  client = new LanguageClient("drapo", OUTPUT_CHANNEL_NAME, serverOptions, clientOptions);
  try {
    await client.start();
    output?.appendLine("Drapo language server started.");
  } catch (err) {
    const message = err instanceof Error ? err.message : String(err);
    output?.appendLine(`Failed to start the Drapo language server: ${message}`);
    void vscode.window.showErrorMessage(`Drapo language server failed to start: ${message}`);
    client = undefined;
  }
}

async function stopClient(): Promise<void> {
  if (!client) {
    return;
  }
  const current = client;
  client = undefined;
  try {
    await current.stop();
  } catch (err) {
    output?.appendLine(`Error stopping the Drapo language server: ${String(err)}`);
  }
}

export async function activate(context: vscode.ExtensionContext): Promise<void> {
  output = vscode.window.createOutputChannel(OUTPUT_CHANNEL_NAME, { log: true });
  context.subscriptions.push(output);

  context.subscriptions.push(
    vscode.commands.registerCommand("drapo.restartServer", async () => {
      await stopClient();
      await startClient(context);
    })
  );

  context.subscriptions.push(
    vscode.workspace.onDidChangeConfiguration(async (e) => {
      if (e.affectsConfiguration("drapo.server.path")) {
        await stopClient();
        await startClient(context);
      }
    })
  );

  await startClient(context);
}

export async function deactivate(): Promise<void> {
  await stopClient();
}
