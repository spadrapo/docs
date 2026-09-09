using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace Drapo.VisualStudio
{
    /// <summary>
    /// Hosts the bundled Drapo.LanguageServer (self-contained win-x64, under <c>server\</c> next to
    /// this assembly) through Visual Studio's built-in LSP client, for the <c>HTML</c> content type
    /// only (.html/.htm). Razor documents use the <c>Razor</c> content type, which does not derive
    /// from <c>HTML</c>, so they are excluded by construction.
    /// </summary>
    [Export(typeof(ILanguageClient))]
    [ContentType(HtmlContentType.Name)]
    [RunOnContext(RunningContext.RunOnHost)]
    public sealed class DrapoLanguageClient : ILanguageClient
    {
        public const string ServerFileName = "Drapo.LanguageServer.exe";

        public string Name => "Drapo Language Server";

        public IEnumerable<string>? ConfigurationSections => null;

        public object? InitializationOptions => null;

        public IEnumerable<string>? FilesToWatch => null;

        public bool ShowNotificationOnInitializeFailed => true;

        public event AsyncEventHandler<EventArgs>? StartAsync;

#pragma warning disable CS0067 // raised by the host when it wants the server stopped; unused here
        public event AsyncEventHandler<EventArgs>? StopAsync;
#pragma warning restore CS0067

        /// <summary>Folder containing the bundled server: <c>&lt;extension dir&gt;\server</c>.</summary>
        public static string ServerDirectory =>
            Path.Combine(Path.GetDirectoryName(typeof(DrapoLanguageClient).Assembly.Location)!, "server");

        public Task<Connection?> ActivateAsync(CancellationToken token)
        {
            string directory = ServerDirectory;
            string exe = Path.Combine(directory, ServerFileName);
            if (!File.Exists(exe))
                throw new FileNotFoundException($"The Drapo language server was not found at '{exe}'. Reinstall the extension.", exe);

            var info = new ProcessStartInfo
            {
                FileName = exe,
                // The documentation content the server reads by convention (content\app\functions, ...).
                Arguments = "--content \"" + Path.Combine(directory, "content", "app") + "\"",
                WorkingDirectory = directory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                // stderr is not redirected on purpose: nothing drains it, and an undrained pipe blocks the server.
                RedirectStandardError = false
            };

            var process = new Process { StartInfo = info };
            if (!process.Start())
                return Task.FromResult<Connection?>(null);

            // reader = what the server writes (its stdout); writer = what the server reads (its stdin).
            return Task.FromResult<Connection?>(new Connection(process.StandardOutput.BaseStream, process.StandardInput.BaseStream));
        }

        public async Task OnLoadedAsync()
        {
            if (StartAsync != null)
                await StartAsync.InvokeAsync(this, EventArgs.Empty);
        }

        public Task OnServerInitializedAsync() => Task.CompletedTask;

        public Task<InitializationFailureContext?> OnServerInitializeFailedAsync(ILanguageClientInitializationInfo initializationState)
        {
            string reason = initializationState.InitializationException?.Message ?? initializationState.StatusMessage ?? "unknown error";
            return Task.FromResult<InitializationFailureContext?>(new InitializationFailureContext
            {
                FailureMessage = "The Drapo language server failed to start: " + reason
            });
        }
    }
}
