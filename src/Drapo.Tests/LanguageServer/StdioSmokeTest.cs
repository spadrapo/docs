using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Drapo.Tests.LanguageServer
{
    /// <summary>
    /// End-to-end over a real process and real stdio framing: initialize → didOpen (bad attribute)
    /// → publishDiagnostics → shutdown → exit 0.
    /// </summary>
    public class StdioSmokeTest
    {
        private static string ServerPath
        {
            get
            {
                // The server is a project reference, so its build output sits next to the tests.
                string exe = Path.Combine(AppContext.BaseDirectory, OperatingSystem.IsWindows() ? "Drapo.LanguageServer.exe" : "Drapo.LanguageServer");
                if (File.Exists(exe))
                    return exe;
                return Path.Combine(AppContext.BaseDirectory, "Drapo.LanguageServer.dll");
            }
        }

        [Fact]
        public async Task InitializeOpenDiagnoseShutdown()
        {
            string path = ServerPath;
            var psi = new ProcessStartInfo
            {
                FileName = path.EndsWith(".dll") ? "dotnet" : path,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            if (path.EndsWith(".dll"))
                psi.ArgumentList.Add(path);
            psi.ArgumentList.Add("--content");
            psi.ArgumentList.Add(TestContentRoot.AppPath);

            using var process = Process.Start(psi);
            Assert.NotNull(process);
            var stderr = new StringBuilder();
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };
            process.BeginErrorReadLine();

            var client = new LspPipe(process.StandardInput.BaseStream, process.StandardOutput.BaseStream);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            try
            {
                JObject init = await client.Request(1, "initialize", new JObject
                {
                    ["processId"] = Environment.ProcessId,
                    ["rootUri"] = null,
                    ["capabilities"] = new JObject
                    {
                        ["textDocument"] = new JObject
                        {
                            ["synchronization"] = new JObject { ["dynamicRegistration"] = false },
                            ["completion"] = new JObject { ["dynamicRegistration"] = false },
                            ["hover"] = new JObject { ["dynamicRegistration"] = false },
                            ["signatureHelp"] = new JObject { ["dynamicRegistration"] = false },
                            ["publishDiagnostics"] = new JObject(),
                            // What Visual Studio's client advertises (research.md R3): dynamic registration,
                            // standard types, no multiline. The server must still answer statically.
                            ["semanticTokens"] = new JObject
                            {
                                ["dynamicRegistration"] = true,
                                ["requests"] = new JObject { ["range"] = true, ["full"] = new JObject { ["delta"] = true } },
                                ["tokenTypes"] = new JArray("keyword", "function", "variable", "string"),
                                ["tokenModifiers"] = new JArray("defaultLibrary", "deprecated"),
                                ["formats"] = new JArray("relative"),
                                ["multilineTokenSupport"] = false,
                                ["overlappingTokenSupport"] = false
                            }
                        }
                    }
                }, timeout.Token);

                JObject caps = (JObject)init["result"]["capabilities"];
                Assert.NotNull(caps["textDocumentSync"]);
                Assert.NotNull(caps["completionProvider"]);
                Assert.NotNull(caps["hoverProvider"]);
                Assert.NotNull(caps["signatureHelpProvider"]);
                Assert.Equal("drapo-language-server", (string)init["result"]["serverInfo"]["name"]);
                // Visual Studio ignores semantic tokens unless the legend is present with at least one type.
                JObject semantic = (JObject)caps["semanticTokensProvider"];
                Assert.NotNull(semantic);
                Assert.Equal(new[] { "keyword", "function", "variable" }, semantic["legend"]["tokenTypes"].Values<string>());
                Assert.Equal(new[] { "defaultLibrary", "unknown" }, semantic["legend"]["tokenModifiers"].Values<string>());
                Assert.True((bool)semantic["range"]);
                Assert.NotNull(semantic["full"]);

                await client.Notify("initialized", new JObject(), timeout.Token);

                const string uri = "file:///c:/tmp/smoke.html";
                await client.Notify("textDocument/didOpen", new JObject
                {
                    ["textDocument"] = new JObject
                    {
                        ["uri"] = uri,
                        ["languageId"] = "html",
                        ["version"] = 1,
                        ["text"] = "<div d-nope=\"x\" d-if=\"{{show}}\"></div>"
                    }
                }, timeout.Token);

                JObject published = await client.WaitForNotification("textDocument/publishDiagnostics", timeout.Token);
                Assert.Equal(uri, (string)published["params"]["uri"]);
                JArray diagnostics = (JArray)published["params"]["diagnostics"];
                JObject d = Assert.Single(diagnostics).Value<JObject>();
                Assert.Equal(1, diagnostics.Count); // d-if is known, only d-nope is flagged
                Assert.Equal("unknown-attribute", (string)d["code"]);
                Assert.Equal(1, (int)d["severity"]);
                Assert.Equal("drapo", (string)d["source"]);
                Assert.Equal(0, (int)d["range"]["start"]["line"]);
                Assert.Equal(5, (int)d["range"]["start"]["character"]);

                // Completion over the wire too.
                JObject completion = await client.Request(2, "textDocument/completion", new JObject
                {
                    ["textDocument"] = new JObject { ["uri"] = uri },
                    ["position"] = new JObject { ["line"] = 0, ["character"] = 7 }
                }, timeout.Token);
                JToken result = completion["result"];
                JToken items = result.Type == JTokenType.Array ? result : result["items"];
                Assert.True(items.HasValues);

                // Semantic tokens over the wire: d-nope (keyword.unknown), d-if (keyword.defaultLibrary), {{show}} (variable).
                JObject tokens = await client.Request(4, "textDocument/semanticTokens/full", new JObject
                {
                    ["textDocument"] = new JObject { ["uri"] = uri }
                }, timeout.Token);
                Assert.Null(tokens["error"]);
                int[] data = tokens["result"]["data"].Values<int>().ToArray();
                // Relative encoding: [deltaLine, deltaStart, length, type, modifiers] * 3
                Assert.Equal(new[] { 0, 5, 6, 0, 2, 0, 11, 4, 0, 1, 0, 6, 8, 2, 0 }, data);
                string resultId = (string)tokens["result"]["resultId"];
                Assert.False(string.IsNullOrEmpty(resultId), "full response must carry a resultId for deltas");

                // Delta with no change: an empty edit list. Then rename d-nope -> d-if and expect an edit.
                JObject unchanged = await client.Request(5, "textDocument/semanticTokens/full/delta", new JObject
                {
                    ["textDocument"] = new JObject { ["uri"] = uri },
                    ["previousResultId"] = resultId
                }, timeout.Token);
                Assert.Null(unchanged["error"]);
                Assert.Empty((JArray)unchanged["result"]["edits"]);

                await client.Notify("textDocument/didChange", new JObject
                {
                    ["textDocument"] = new JObject { ["uri"] = uri, ["version"] = 2 },
                    ["contentChanges"] = new JArray(new JObject { ["text"] = "<div d-if=\"x\" d-if=\"{{show}}\"></div>" })
                }, timeout.Token);
                JObject changed = await client.Request(6, "textDocument/semanticTokens/full/delta", new JObject
                {
                    ["textDocument"] = new JObject { ["uri"] = uri },
                    ["previousResultId"] = (string)unchanged["result"]["resultId"]
                }, timeout.Token);
                Assert.Null(changed["error"]);
                JArray edits = (JArray)changed["result"]["edits"];
                Assert.NotEmpty(edits);
                // The rewritten data must equal a fresh full request.
                JObject full2 = await client.Request(7, "textDocument/semanticTokens/full", new JObject
                {
                    ["textDocument"] = new JObject { ["uri"] = uri }
                }, timeout.Token);
                Assert.Equal(new[] { 0, 5, 4, 0, 1, 0, 9, 4, 0, 1, 0, 6, 8, 2, 0 }, full2["result"]["data"].Values<int>().ToArray());

                JObject shutdown = await client.Request(3, "shutdown", null, timeout.Token);
                Assert.Null(shutdown["error"]);
                await client.Notify("exit", null, timeout.Token);

                Assert.True(process.WaitForExit(10000), "server did not exit after 'exit'");
                Assert.Equal(0, process.ExitCode);
            }
            finally
            {
                if (!process.HasExited)
                    process.Kill(true);
                if (stderr.Length > 0)
                    Console.Error.WriteLine("server stderr:\n" + stderr);
            }
        }

        /// <summary>Tiny Content-Length framed JSON-RPC client.</summary>
        private sealed class LspPipe
        {
            private readonly Stream _in;
            private readonly Stream _out;
            private readonly Queue<JObject> _buffered = new Queue<JObject>();

            public LspPipe(Stream toServer, Stream fromServer)
            {
                _in = toServer;
                _out = fromServer;
            }

            public async Task Notify(string method, JObject @params, CancellationToken ct)
            {
                var msg = new JObject { ["jsonrpc"] = "2.0", ["method"] = method };
                if (@params != null) msg["params"] = @params;
                await Send(msg, ct);
            }

            public async Task<JObject> Request(int id, string method, JObject @params, CancellationToken ct)
            {
                var msg = new JObject { ["jsonrpc"] = "2.0", ["id"] = id, ["method"] = method };
                if (@params != null) msg["params"] = @params;
                await Send(msg, ct);
                while (true)
                {
                    JObject m = await Read(ct);
                    if (m["id"] != null && m["id"].Type == JTokenType.Integer && (int)m["id"] == id && m["method"] == null)
                        return m;
                    if (m["method"] != null && m["id"] != null)
                        await Send(new JObject { ["jsonrpc"] = "2.0", ["id"] = m["id"], ["result"] = null }, ct); // server->client request: answer null
                    else
                        _buffered.Enqueue(m);
                }
            }

            public async Task<JObject> WaitForNotification(string method, CancellationToken ct)
            {
                foreach (JObject b in _buffered)
                    if ((string)b["method"] == method) return b;
                while (true)
                {
                    JObject m = await Read(ct);
                    if ((string)m["method"] == method)
                        return m;
                    if (m["method"] != null && m["id"] != null)
                        await Send(new JObject { ["jsonrpc"] = "2.0", ["id"] = m["id"], ["result"] = null }, ct);
                }
            }

            private async Task Send(JObject msg, CancellationToken ct)
            {
                byte[] body = Encoding.UTF8.GetBytes(msg.ToString(Newtonsoft.Json.Formatting.None));
                byte[] header = Encoding.ASCII.GetBytes($"Content-Length: {body.Length}\r\n\r\n");
                await _in.WriteAsync(header, ct);
                await _in.WriteAsync(body, ct);
                await _in.FlushAsync(ct);
            }

            private async Task<JObject> Read(CancellationToken ct)
            {
                int length = -1;
                while (true)
                {
                    string line = await ReadLine(ct);
                    if (line.Length == 0)
                        break;
                    if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                        length = int.Parse(line.Substring("Content-Length:".Length).Trim());
                }
                if (length < 0)
                    throw new InvalidOperationException("Missing Content-Length header");
                byte[] buffer = new byte[length];
                int read = 0;
                while (read < length)
                {
                    int n = await _out.ReadAsync(buffer.AsMemory(read, length - read), ct);
                    if (n == 0) throw new EndOfStreamException("server closed stdout");
                    read += n;
                }
                return JObject.Parse(Encoding.UTF8.GetString(buffer));
            }

            private async Task<string> ReadLine(CancellationToken ct)
            {
                var sb = new StringBuilder();
                byte[] one = new byte[1];
                while (true)
                {
                    int n = await _out.ReadAsync(one.AsMemory(0, 1), ct);
                    if (n == 0) throw new EndOfStreamException("server closed stdout");
                    char c = (char)one[0];
                    if (c == '\n') break;
                    if (c != '\r') sb.Append(c);
                }
                return sb.ToString();
            }
        }
    }
}
