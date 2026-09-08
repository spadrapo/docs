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
    public sealed class SignatureHelpHandler : SignatureHelpHandlerBase
    {
        private readonly DocumentStore _documents;
        private readonly DrapoSymbolIndex _index;
        private readonly ILogger<SignatureHelpHandler> _logger;

        public SignatureHelpHandler(DocumentStore documents, DrapoSymbolIndex index, ILogger<SignatureHelpHandler> logger)
        {
            _documents = documents;
            _index = index;
            _logger = logger;
        }

        protected override SignatureHelpRegistrationOptions CreateRegistrationOptions(SignatureHelpCapability capability, ClientCapabilities clientCapabilities)
        {
            return new SignatureHelpRegistrationOptions
            {
                DocumentSelector = DrapoDocuments.Selector,
                TriggerCharacters = new Container<string>("(", ","),
                RetriggerCharacters = new Container<string>(",")
            };
        }

        public override Task<SignatureHelp> Handle(SignatureHelpParams request, CancellationToken cancellationToken)
        {
            try
            {
                string text = _documents.GetText(request.TextDocument.Uri);
                if (text == null)
                    return Task.FromResult<SignatureHelp>(null);
                return Task.FromResult(new SignatureHelpProvider(_index).GetSignatureHelp(text, request.Position));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Signature help failed for {Uri}", request.TextDocument.Uri);
                return Task.FromResult<SignatureHelp>(null);
            }
        }
    }
}
