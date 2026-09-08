using System;
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
    public sealed class HoverHandler : HoverHandlerBase
    {
        private readonly DocumentStore _documents;
        private readonly DrapoSymbolIndex _index;
        private readonly ILogger<HoverHandler> _logger;

        public HoverHandler(DocumentStore documents, DrapoSymbolIndex index, ILogger<HoverHandler> logger)
        {
            _documents = documents;
            _index = index;
            _logger = logger;
        }

        protected override HoverRegistrationOptions CreateRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
        {
            return new HoverRegistrationOptions { DocumentSelector = DrapoDocuments.Selector };
        }

        public override Task<Hover> Handle(HoverParams request, CancellationToken cancellationToken)
        {
            try
            {
                string text = _documents.GetText(request.TextDocument.Uri);
                if (text == null)
                    return Task.FromResult<Hover>(null);
                return Task.FromResult(new HoverProvider(_index).GetHover(text, request.Position));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hover failed for {Uri}", request.TextDocument.Uri);
                return Task.FromResult<Hover>(null);
            }
        }
    }
}
