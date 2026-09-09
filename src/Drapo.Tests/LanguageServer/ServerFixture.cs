using System;
using Drapo.LanguageServer;
using Drapo.LanguageServer.Providers;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Drapo.Tests.LanguageServer
{
    /// <summary>Shared symbol index and providers over the repository's real content.</summary>
    public static class ServerFixture
    {
        private static readonly Lazy<DrapoSymbolIndex> IndexLazy = new Lazy<DrapoSymbolIndex>(() =>
            new DrapoSymbolIndex(TestContentRoot.Catalog, TestContentRoot.Attributes(), TestContentRoot.Functions()));

        public static DrapoSymbolIndex Index => IndexLazy.Value;

        public static CompletionProvider Completion => new CompletionProvider(Index);

        public static HoverProvider Hover => new HoverProvider(Index);

        public static SignatureHelpProvider SignatureHelp => new SignatureHelpProvider(Index);

        public static SemanticTokensProvider SemanticTokens => new SemanticTokensProvider(Index);

        /// <summary>Position of the first occurrence of <paramref name="marker"/> plus <paramref name="delta"/> characters.</summary>
        public static Position At(string text, string marker, int delta = 0)
        {
            int idx = text.IndexOf(marker, StringComparison.Ordinal);
            if (idx < 0)
                throw new ArgumentException($"Marker '{marker}' not found.");
            int offset = idx + delta;
            int line = 0, character = 0;
            for (int i = 0; i < offset; i++)
            {
                if (text[i] == '\n') { line++; character = 0; }
                else character++;
            }
            return new Position(line, character);
        }

        /// <summary>Position right after the first occurrence of <paramref name="marker"/>.</summary>
        public static Position After(string text, string marker) => At(text, marker, marker.Length);
    }
}
