using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
                            ["publishDiagnostics"] = new JObject()
                        }
                    }
                }, timeout.Token);

                JObject caps = (JObject)init["result"]["capabilities"];
                Assert.NotNull(caps["textDocumentSync"]);
                Assert.NotNull(caps["completionProvider"]);
                Assert.NotNull(caps["hoverProvider"]);
                Assert.NotNull(caps["signatureHelpProvider"]);
                Assert.Equal("drapo-language-server", (string)init["result"]["serverInfo"]["name"]);

                await client.Notify("initialized", new JObject(), timeout.Token);

                const string uri = "file:///c:/tmp/smoke.html";
                await client.Notify("textDocument/didOpen", new JObject
                {
                    ["textDocument"] = new JObject
                    {
                        ["uri"] = uri,
                        ["languageId"] = "html",
                        ["version"] = 1,
                        ["text"] = "<div d-nope=\"x\"></div>"
                    }
                }, timeout.Token);

                JObject published = await client.WaitForNotification("textDocument/publishDiagnostics", timeout.Token);
                Assert.Equal(uri, (string)published["params"]["uri"]);
                JArray diagnostics = (JArray)published["params"]["diagnostics"];
                JObject d = Assert.Single(diagnostics).Value<JObject>();
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
