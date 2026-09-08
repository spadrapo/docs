using System;
using System.Collections.Generic;
using System.Linq;
using Drapo.LanguageServer.Documents;
using Drapo.Tooling.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Drapo.LanguageServer.Providers
{
    /// <summary>
    /// Attribute-name completion inside a tag. Items are exactly the engine's attributes (fixed
    /// names + prefix families) plus documented family members the engine recognises.
    /// </summary>
    public sealed class CompletionProvider
    {
        private readonly DrapoSymbolIndex _index;

        public CompletionProvider(DrapoSymbolIndex index)
        {
            _index = index;
        }

        public CompletionList GetCompletions(string text, Position position)
        {
            text ??= string.Empty;
            int offset = TextPosition.OffsetOf(text, position.Line, position.Character);
            if (!TextPosition.IsInsideTag(text, offset))
                return new CompletionList(isIncomplete: false);

            string word = TextPosition.WordBefore(text, offset);
            if (word.Length > 0 && !word.StartsWith("d", StringComparison.OrdinalIgnoreCase))
                return new CompletionList(isIncomplete: false);
            if (word.Length > 1 && !word.StartsWith("d-", StringComparison.OrdinalIgnoreCase))
                return new CompletionList(isIncomplete: false);

            var replace = new Range(
                new Position(position.Line, position.Character - word.Length),
                new Position(position.Line, position.Character));

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var items = new List<CompletionItem>();

            void Add(string label, CompletionItemKind kind, string documentation, string sortPrefix)
            {
                if (!seen.Add(label))
                    return;
                items.Add(new CompletionItem
                {
                    Label = label,
                    Kind = kind,
                    InsertText = label,
                    FilterText = label,
                    SortText = sortPrefix + label,
                    TextEdit = new TextEdit { Range = replace, NewText = label },
                    Documentation = documentation == null ? null : new StringOrMarkupContent(new MarkupContent { Kind = MarkupKind.Markdown, Value = documentation })
                });
            }

            // (a) fixed engine attributes, with documentation when a page exists
            foreach (string name in _index.EngineAttributes)
            {
                _index.TryGetAttribute(name, out AttributeVM doc);
                Add(doc?.Name ?? name, CompletionItemKind.Property, doc?.Description, "0-");
            }
            // (b) documented members of prefix families (e.g. d-on-model-change)
            foreach (AttributeVM doc in _index.Attributes.Values)
                Add(doc.Name, CompletionItemKind.Property, doc.Description, "0-");
            // (c) prefix families themselves
            foreach (string prefix in _index.Prefixes)
                Add(prefix, CompletionItemKind.Keyword, $"Prefix family: complete with the suffix (e.g. `{prefix}...`).", "1-");

            items.Sort((a, b) => string.CompareOrdinal(a.SortText, b.SortText));
            return new CompletionList(items, isIncomplete: false);
        }
    }
}
