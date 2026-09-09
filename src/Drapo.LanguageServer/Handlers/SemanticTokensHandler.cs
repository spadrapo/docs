using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Drapo.LanguageServer.Documents;
using Drapo.LanguageServer.Providers;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Drapo.LanguageServer.Handlers
{
    /// <summary>
    /// textDocument/semanticTokens (full, full/delta and range): catalog-aware colouring of d-*
    /// attributes, handler function names and mustaches. See <see cref="SemanticTokensProvider"/>.
    /// Delta matters for Visual Studio, whose client re-polls a full-only provider every two
    /// seconds while the document is visible; with delta the poll is a tiny edit list.
    /// </summary>
    public sealed class SemanticTokensHandler : SemanticTokensHandlerBase
    {
        private readonly ConcurrentDictionary<DocumentUri, SemanticTokensDocument> _tokenDocuments = new ConcurrentDictionary<DocumentUri, SemanticTokensDocument>();
        private readonly DocumentStore _documents;
        private readonly DrapoSymbolIndex _index;
        private readonly ILogger<SemanticTokensHandler> _logger;

        public SemanticTokensHandler(DocumentStore documents, DrapoSymbolIndex index, ILogger<SemanticTokensHandler> logger)
        {
            _documents = documents;
            _index = index;
            _logger = logger;
        }

        protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(SemanticTokensCapability capability, ClientCapabilities clientCapabilities)
        {
            return new SemanticTokensRegistrationOptions
            {
                DocumentSelector = DrapoDocuments.Selector,
                Legend = DrapoSemanticTokens.Legend,
                Full = new SemanticTokensCapabilityRequestFull { Delta = true },
                Range = true
            };
        }

        protected override Task Tokenize(SemanticTokensBuilder builder, ITextDocumentIdentifierParams identifier, CancellationToken cancellationToken)
        {
            try
            {
                string text = _documents.GetText(identifier.TextDocument.Uri);
                if (text == null)
                {
                    ProtocolTrace.Write($"semanticTokens: no open document for {identifier.TextDocument.Uri}");
                    return Task.CompletedTask;
                }
                int count = 0;
                foreach (DrapoToken token in new SemanticTokensProvider(_index).GetTokens(text))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    builder.Push(token.Line, token.Character, token.Length, token.Type, token.Modifiers);
                    count++;
                }
                ProtocolTrace.Write($"semanticTokens: {count} tokens for {identifier.TextDocument.Uri}");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Never fail the request: a colouring glitch must not break the editor.
                _logger.LogError(ex, "Semantic tokens failed for {Uri}", identifier.TextDocument.Uri);
            }
            return Task.CompletedTask;
        }

        protected override Task<SemanticTokensDocument> GetSemanticTokensDocument(ITextDocumentIdentifierParams @params, CancellationToken cancellationToken)
        {
            // One tokens document per open text document: OmniSharp diffs against it for full/delta.
            // Entries for documents that were closed meanwhile are dropped on the way.
            foreach (DocumentUri uri in _tokenDocuments.Keys)
            {
                if (!_documents.TryGet(uri, out _))
                    _tokenDocuments.TryRemove(uri, out _);
            }
            return Task.FromResult(_tokenDocuments.GetOrAdd(@params.TextDocument.Uri, _ => new SemanticTokensDocument(DrapoSemanticTokens.Legend)));
        }
    }
}
