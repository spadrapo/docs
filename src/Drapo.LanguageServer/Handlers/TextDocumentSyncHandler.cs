using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Drapo.LanguageServer.Documents;
using Drapo.LanguageServer.Providers;
using Drapo.Tooling.Models;
using Drapo.Tooling.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace Drapo.LanguageServer.Handlers
{
    /// <summary>
    /// Full-text document synchronisation. Every open/change/save (re)schedules validation after a
    /// short quiet period; close clears the document's diagnostics.
    /// </summary>
    public sealed class TextDocumentSyncHandler : TextDocumentSyncHandlerBase
    {
        public static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(250);

        private readonly ILanguageServerFacade _server;
        private readonly DocumentStore _documents;
        private readonly IDrapoValidatorService _validator;
        private readonly ILogger<TextDocumentSyncHandler> _logger;
        private readonly ConcurrentDictionary<DocumentUri, CancellationTokenSource> _pending = new ConcurrentDictionary<DocumentUri, CancellationTokenSource>();

        public TextDocumentSyncHandler(ILanguageServerFacade server, DocumentStore documents, IDrapoValidatorService validator, ILogger<TextDocumentSyncHandler> logger)
        {
            _server = server;
            _documents = documents;
            _validator = validator;
            _logger = logger;
        }

        public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
        {
            string languageId = _documents.TryGet(uri, out var state) && state.LanguageId != null ? state.LanguageId : "html";
            return new TextDocumentAttributes(uri, languageId);
        }

        /// <summary>
        /// The other handlers are registered for the language ids VS Code uses (see
        /// <see cref="DrapoDocuments"/>) and OmniSharp routes requests by the id stored at didOpen.
        /// Other clients derive the id from their own content type names (Visual Studio sends the
        /// content type, e.g. "HTML"), so anything unknown is mapped by file extension.
        /// </summary>
        public static string NormalizeLanguageId(DocumentUri uri, string languageId)
        {
            string id = languageId?.ToLowerInvariant();
            if (id != null && DrapoDocuments.LanguageIds.Contains(id))
                return id;
            string path = uri?.Path ?? string.Empty;
            if (path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
                return "aspnetcorerazor";
            if (path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
                return "razor";
            return "html"; // .html/.htm and anything else the client chose to open with us
        }

        protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
        {
            return new TextDocumentSyncRegistrationOptions
            {
                DocumentSelector = DrapoDocuments.Selector,
                Change = TextDocumentSyncKind.Full,
                Save = new SaveOptions { IncludeText = false }
            };
        }

        public override Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
        {
            ProtocolTrace.Write($"didOpen {request.TextDocument.Uri} languageId={request.TextDocument.LanguageId}");
            _documents.Set(request.TextDocument.Uri, request.TextDocument.Text, request.TextDocument.Version, NormalizeLanguageId(request.TextDocument.Uri, request.TextDocument.LanguageId));
            ScheduleValidation(request.TextDocument.Uri);
            return Unit.Task;
        }

        public override Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
        {
            string text = null;
            foreach (var change in request.ContentChanges)
                text = change.Text; // Full sync: the last full-content change wins.
            if (text != null)
                _documents.Set(request.TextDocument.Uri, text, request.TextDocument.Version);
            ScheduleValidation(request.TextDocument.Uri);
            return Unit.Task;
        }

        public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
        {
            if (request.Text != null)
                _documents.Set(request.TextDocument.Uri, request.Text, null);
            ScheduleValidation(request.TextDocument.Uri);
            return Unit.Task;
        }

        public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
        {
            if (_pending.TryRemove(request.TextDocument.Uri, out var cts))
                cts.Cancel();
            _documents.Remove(request.TextDocument.Uri);
            Publish(request.TextDocument.Uri, null, new Container<Diagnostic>());
            return Unit.Task;
        }

        private void ScheduleValidation(DocumentUri uri)
        {
            var cts = new CancellationTokenSource();
            var previous = _pending.AddOrUpdate(uri, cts, (_, old) => { old.Cancel(); return cts; });
            _ = RunValidationAsync(uri, cts.Token);
        }

        private async Task RunValidationAsync(DocumentUri uri, CancellationToken token)
        {
            try
            {
                await Task.Delay(Debounce, token);
                if (!_documents.TryGet(uri, out var state))
                    return;
                DrapoValidationResultVM result = await _validator.Validate(state.Text);
                if (token.IsCancellationRequested)
                    return;
                var mapped = DiagnosticMapper.Map(result.Diagnostics, state.Text);
                Publish(uri, state.Version, new Container<Diagnostic>(mapped));
            }
            catch (OperationCanceledException)
            {
                // superseded by a newer edit
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Validation failed for {Uri}", uri);
            }
        }

        private void Publish(DocumentUri uri, int? version, Container<Diagnostic> diagnostics)
        {
            _server.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
            {
                Uri = uri,
                Version = version,
                Diagnostics = diagnostics
            });
        }
    }

    /// <summary>Document selector shared by all handlers.</summary>
    public static class DrapoDocuments
    {
        public static readonly string[] LanguageIds = { "html", "razor", "aspnetcorerazor" };
        public static readonly TextDocumentSelector Selector = TextDocumentSelector.ForLanguage(LanguageIds);
    }
}
