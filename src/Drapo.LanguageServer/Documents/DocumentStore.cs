using System.Collections.Concurrent;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Drapo.LanguageServer.Documents
{
    /// <summary>One open document (full text; sync kind Full).</summary>
    public sealed class DocumentState
    {
        public DocumentUri Uri { get; init; }
        public string Text { get; init; }
        public int? Version { get; init; }
        public string LanguageId { get; init; }
    }

    /// <summary>Thread-safe map of open documents keyed by URI.</summary>
    public sealed class DocumentStore
    {
        private readonly ConcurrentDictionary<DocumentUri, DocumentState> _documents = new ConcurrentDictionary<DocumentUri, DocumentState>();

        public void Set(DocumentUri uri, string text, int? version, string languageId = null)
        {
            _documents.AddOrUpdate(uri,
                _ => new DocumentState { Uri = uri, Text = text ?? string.Empty, Version = version, LanguageId = languageId },
                (_, old) => new DocumentState { Uri = uri, Text = text ?? string.Empty, Version = version, LanguageId = languageId ?? old.LanguageId });
        }

        public bool TryGet(DocumentUri uri, out DocumentState state) => _documents.TryGetValue(uri, out state);

        public string GetText(DocumentUri uri) => _documents.TryGetValue(uri, out var s) ? s.Text : null;

        public void Remove(DocumentUri uri) => _documents.TryRemove(uri, out _);
    }
}
