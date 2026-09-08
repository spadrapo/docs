using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Drapo.LanguageServer.Documents;
using Drapo.LanguageServer.Handlers;
using Drapo.Tooling.Content;
using Drapo.Tooling.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Server;

namespace Drapo.LanguageServer
{
    public static class Program
    {
        public const string ServerName = "drapo-language-server";

        public static string ServerVersion =>
            typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(Program).Assembly.GetName().Version?.ToString()
            ?? "0.0.0";

        public static string DefaultContentPath => Path.Combine(AppContext.BaseDirectory, "content", "app");

        public static async Task<int> Main(string[] args)
        {
            string contentPath = DefaultContentPath;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--content" && i + 1 < args.Length)
                    contentPath = Path.GetFullPath(args[++i]);
                else if (args[i] == "--version")
                {
                    Console.WriteLine($"{ServerName} {ServerVersion} (engine {new DrapoEngineCatalog().EngineVersion})");
                    return 0;
                }
                else if (args[i] == "--help" || args[i] == "-h")
                {
                    Console.WriteLine("Usage: Drapo.LanguageServer [--content <dir>] [--version]");
                    Console.WriteLine("Speaks the Language Server Protocol over stdin/stdout.");
                    return 0;
                }
            }

            if (!Directory.Exists(Path.Combine(contentPath, "functions")))
            {
                await Console.Error.WriteLineAsync($"{ServerName}: documentation content not found at '{contentPath}' (expected a 'functions' folder). Use --content <dir>.");
                return 1;
            }

            try
            {
                var server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(options => options
                    .WithInput(Console.OpenStandardInput())
                    .WithOutput(Console.OpenStandardOutput())
                    .ConfigureLogging(logging => logging
                        .ClearProviders()
                        .AddProvider(new StderrLoggerProvider())
                        .SetMinimumLevel(LogLevel.Warning))
                    .WithServerInfo(new ServerInfo { Name = ServerName, Version = ServerVersion })
                    .WithServices(services => ConfigureServices(services, contentPath))
                    .WithHandler<TextDocumentSyncHandler>()
                    .WithHandler<CompletionHandler>()
                    .WithHandler<HoverHandler>()
                    .WithHandler<SignatureHelpHandler>());

                await server.WaitForExit;
                return 0;
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"{ServerName}: fatal: {ex}");
                return 1;
            }
        }

        /// <summary>Registers the tooling services (same implementations the docs site uses) and server state.</summary>
        public static void ConfigureServices(IServiceCollection services, string contentPath)
        {
            services.AddSingleton<IDrapoContentRoot>(new DrapoContentRoot(contentPath));
            services.AddSingleton<IDrapoEngineCatalog, DrapoEngineCatalog>();
            services.AddSingleton<IFunctionService, FunctionService>();
            services.AddSingleton<IAttributeService, AttributeService>();
            services.AddSingleton<IDrapoValidatorService, DrapoValidatorService>();
            services.AddSingleton<DocumentStore>();
            services.AddSingleton<DrapoSymbolIndex>();
        }
    }

    internal sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new NullScope();
        public void Dispose() { }
    }

    /// <summary>Minimal logger that writes to stderr (stdout is reserved for the protocol).</summary>
    internal sealed class StderrLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new StderrLogger(categoryName);
        public void Dispose() { }

        private sealed class StderrLogger : ILogger
        {
            private readonly string _category;
            public StderrLogger(string category) { _category = category; }
            public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
            // OmniSharp warns once that no configuration sections are registered; this server has
            // no settings, so that warning is expected and only noise for the user.
            public bool IsEnabled(LogLevel logLevel) =>
                logLevel >= LogLevel.Warning && !_category.EndsWith(".DidChangeConfigurationProvider", StringComparison.Ordinal);
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (!IsEnabled(logLevel))
                    return;
                Console.Error.WriteLine($"[{logLevel}] {_category}: {formatter(state, exception)}{(exception != null ? Environment.NewLine + exception : string.Empty)}");
            }
        }
    }
}
