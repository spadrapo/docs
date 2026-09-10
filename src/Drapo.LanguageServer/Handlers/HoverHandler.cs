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
        private MarkupKind _kind = MarkupKind.Markdown;

        public HoverHandler(DocumentStore documents, DrapoSymbolIndex index, ILogger<HoverHandler> logger)
        {
            _documents = documents;
            _index = index;
            _logger = logger;
        }

        protected override HoverRegistrationOptions CreateRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
        {
            _kind = PreferredKind(capability?.ContentFormat);
            return new HoverRegistrationOptions { DocumentSelector = DrapoDocuments.Selector };
        }

        /// <summary>Markdown when the client lists it (VS Code); plain text otherwise (Visual Studio sends only "plaintext").</summary>
        public static MarkupKind PreferredKind(Container<MarkupKind> formats)
        {
            if (formats == null)
                return MarkupKind.Markdown;
            foreach (MarkupKind format in formats)
            {
                if (format == MarkupKind.Markdown)
                    return MarkupKind.Markdown;
                if (format == MarkupKind.PlainText)
                    return MarkupKind.PlainText;
            }
            return MarkupKind.Markdown;
        }

        public override Task<Hover> Handle(HoverParams request, CancellationToken cancellationToken)
        {
            try
            {
                string text = _documents.GetText(request.TextDocument.Uri);
                if (text == null)
                    return Task.FromResult<Hover>(null);
                Hover hover = new HoverProvider(_index).GetHover(text, request.Position, _kind);
                ProtocolTrace.Write($"hover {request.TextDocument.Uri} at {request.Position.Line}:{request.Position.Character} -> {(hover == null ? "null" : hover.Contents.MarkupContent?.Value?.Length + " chars")}");
                return Task.FromResult(hover);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hover failed for {Uri}", request.TextDocument.Uri);
                return Task.FromResult<Hover>(null);
            }
        }
    }
}
