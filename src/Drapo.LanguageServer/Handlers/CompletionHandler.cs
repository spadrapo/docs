using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Drapo.LanguageServer.Documents;
using Drapo.LanguageServer.Providers;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Drapo.LanguageServer.Handlers
{
    public sealed class CompletionHandler : CompletionHandlerBase
    {
        private readonly DocumentStore _documents;
        private readonly DrapoSymbolIndex _index;
        private readonly ILogger<CompletionHandler> _logger;

        public CompletionHandler(DocumentStore documents, DrapoSymbolIndex index, ILogger<CompletionHandler> logger)
        {
            _documents = documents;
            _index = index;
            _logger = logger;
        }

        protected override CompletionRegistrationOptions CreateRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities)
        {
            return new CompletionRegistrationOptions
            {
                DocumentSelector = DrapoDocuments.Selector,
                TriggerCharacters = new Container<string>("-"),
                ResolveProvider = false
            };
        }

        public override Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
        {
            try
            {
                string text = _documents.GetText(request.TextDocument.Uri);
                if (text == null)
                    return Task.FromResult(new CompletionList(isIncomplete: false));
                CompletionList list = new CompletionProvider(_index).GetCompletions(text, request.Position);
                ProtocolTrace.Write($"completion {request.TextDocument.Uri} at {request.Position.Line}:{request.Position.Character} trigger={request.Context?.TriggerKind}/{request.Context?.TriggerCharacter} -> {list.Items.Count()} items");
                return Task.FromResult(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Completion failed for {Uri}", request.TextDocument.Uri);
                return Task.FromResult(new CompletionList(isIncomplete: false));
            }
        }

        public override Task<CompletionItem> Handle(CompletionItem request, CancellationToken cancellationToken) => Task.FromResult(request);
    }
}
