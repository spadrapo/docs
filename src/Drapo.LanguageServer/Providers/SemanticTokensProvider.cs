using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Drapo.LanguageServer.Documents;
using Drapo.Tooling.Helpers;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Drapo.LanguageServer.Providers
{
    /// <summary>
    /// Classification of one span of a document. Token types are the standard LSP names, so any
    /// client (VS Code, Visual Studio, others) colours them with its built-in theme; the
    /// <c>unknown</c> modifier lets a client that knows it (the VS Code extension) paint what the
    /// engine does not recognise differently, and is harmless everywhere else.
    /// </summary>
    public sealed class DrapoToken
    {
        public int Line { get; init; }
        public int Character { get; init; }
        public int Length { get; init; }
        public int Offset { get; init; }
        public SemanticTokenType Type { get; init; }
        public IReadOnlyList<SemanticTokenModifier> Modifiers { get; init; }

        public override string ToString() =>
            $"{Line}:{Character}+{Length} {Type}{(Modifiers.Count > 0 ? " [" + string.Join(",", Modifiers.Select(m => (string)m)) + "]" : string.Empty)}";
    }

    /// <summary>
    /// The legend the server advertises. Fixed and public so the client extensions and the tests
    /// can rely on it (FR-019/FR-020: known/unknown comes from the same catalog the diagnostics use).
    /// </summary>
    public static class DrapoSemanticTokens
    {
        /// <summary>A d-* attribute name (known: <see cref="LibraryModifier"/>; unknown: <see cref="UnknownModifier"/>).</summary>
        public static readonly SemanticTokenType AttributeType = SemanticTokenType.Keyword;
        /// <summary>A function name inside a d-on-* handler value.</summary>
        public static readonly SemanticTokenType FunctionType = SemanticTokenType.Function;
        /// <summary>A whole {{ mustache }} expression (delimiters included).</summary>
        public static readonly SemanticTokenType MustacheType = SemanticTokenType.Variable;

        /// <summary>Standard modifier: the symbol is part of the framework (the engine dispatches it).</summary>
        public static readonly SemanticTokenModifier LibraryModifier = SemanticTokenModifier.DefaultLibrary;
        /// <summary>Custom modifier: the engine does not recognise the symbol (a diagnostic accompanies it).</summary>
        public static readonly SemanticTokenModifier UnknownModifier = new SemanticTokenModifier("unknown");

        public static readonly IReadOnlyList<SemanticTokenType> TokenTypes = new[] { AttributeType, FunctionType, MustacheType };
        public static readonly IReadOnlyList<SemanticTokenModifier> TokenModifiers = new[] { LibraryModifier, UnknownModifier };

        public static SemanticTokensLegend Legend => new SemanticTokensLegend
        {
            TokenTypes = new Container<SemanticTokenType>(TokenTypes),
            TokenModifiers = new Container<SemanticTokenModifier>(TokenModifiers)
        };
    }

    /// <summary>
    /// Scans a document for Drapo markup and classifies it. Deliberately the same text-based
    /// interpretation the validator uses (<see cref="TextPosition.AttributeRegex"/> and
    /// <see cref="DrapoHandlerSyntax"/>), so a symbol the diagnostics call unknown is never painted
    /// as known here.
    /// </summary>
    public sealed class SemanticTokensProvider
    {
        // A d-* attribute without a value, inside a tag (checked separately): `<div d-router>`.
        private static readonly Regex BareAttributeRegex = new Regex(@"(?<=\s)(d-[A-Za-z][\w-]*)(?=[\s/>])", RegexOptions.Compiled);

        private readonly DrapoSymbolIndex _index;

        public SemanticTokensProvider(DrapoSymbolIndex index)
        {
            _index = index;
        }

        /// <summary>Tokens for the whole document, ordered by position, never overlapping, never spanning lines.</summary>
        public List<DrapoToken> GetTokens(string text)
        {
            text ??= string.Empty;
            var spans = new List<(int start, int length, SemanticTokenType type, SemanticTokenModifier[] modifiers)>();

            foreach (Match m in TextPosition.AttributeRegex.Matches(text))
            {
                Group name = m.Groups[1];
                spans.Add((name.Index, name.Length, DrapoSemanticTokens.AttributeType, AttributeModifiers(name.Value)));

                if (!name.Value.StartsWith("d-on-", StringComparison.OrdinalIgnoreCase))
                    continue;
                Group value = m.Groups[2].Success ? m.Groups[2] : m.Groups[3];
                foreach (DrapoHandlerSyntax.FunctionCall call in DrapoHandlerSyntax.ScanFunctionCalls(value.Value))
                {
                    var modifiers = _index.Catalog.IsValidFunction(call.Name)
                        ? new[] { DrapoSemanticTokens.LibraryModifier }
                        : new[] { DrapoSemanticTokens.UnknownModifier };
                    spans.Add((value.Index + call.NameIndex, call.Name.Length, DrapoSemanticTokens.FunctionType, modifiers));
                }
            }

            foreach (Match m in BareAttributeRegex.Matches(text))
            {
                // Skip names that are the start of a valued attribute (already covered) and prose.
                if (!TextPosition.IsInsideTag(text, m.Index) || IsFollowedByEquals(text, m.Index + m.Length))
                    continue;
                spans.Add((m.Index, m.Length, DrapoSemanticTokens.AttributeType, AttributeModifiers(m.Value)));
            }

            foreach ((int start, int end) in Mustaches(text))
                spans.Add((start, end - start, DrapoSemanticTokens.MustacheType, Array.Empty<SemanticTokenModifier>()));

            // Order, drop overlaps (first wins) and split anything that crosses a line break.
            var result = new List<DrapoToken>();
            int[] lineStarts = LineStarts(text);
            int lastEnd = -1;
            foreach (var span in spans.OrderBy(s => s.start).ThenByDescending(s => s.length))
            {
                if (span.start < lastEnd)
                    continue;
                lastEnd = span.start + span.length;
                foreach (DrapoToken token in SplitLines(text, lineStarts, span.start, span.length, span.type, span.modifiers))
                    result.Add(token);
            }
            return result;
        }

        private SemanticTokenModifier[] AttributeModifiers(string name) =>
            _index.Catalog.IsValidAttribute(name)
                ? new[] { DrapoSemanticTokens.LibraryModifier }
                : new[] { DrapoSemanticTokens.UnknownModifier };

        private static bool IsFollowedByEquals(string text, int offset)
        {
            int i = offset;
            while (i < text.Length && char.IsWhiteSpace(text[i]))
                i++;
            return i < text.Length && text[i] == '=';
        }

        /// <summary>Balanced outermost {{ ... }} pairs as [start, end) offsets; unclosed openers are ignored.</summary>
        private static IEnumerable<(int start, int end)> Mustaches(string text)
        {
            var stack = new Stack<int>();
            for (int i = 0; i + 1 < text.Length; i++)
            {
                if (text[i] == '{' && text[i + 1] == '{')
                {
                    stack.Push(i);
                    i++;
                }
                else if (text[i] == '}' && text[i + 1] == '}')
                {
                    if (stack.Count > 0)
                    {
                        int start = stack.Pop();
                        if (stack.Count == 0)
                            yield return (start, i + 2);
                    }
                    i++;
                }
            }
        }

        private static int[] LineStarts(string text)
        {
            var starts = new List<int> { 0 };
            for (int i = 0; i < text.Length; i++)
                if (text[i] == '\n')
                    starts.Add(i + 1);
            return starts.ToArray();
        }

        /// <summary>Offset → 0-based (line, character) in O(log n) using the precomputed line starts.</summary>
        private static (int line, int character) Position(int[] lineStarts, int offset)
        {
            int lo = 0, hi = lineStarts.Length - 1;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (lineStarts[mid] <= offset) lo = mid; else hi = mid - 1;
            }
            return (lo, offset - lineStarts[lo]);
        }

        private static IEnumerable<DrapoToken> SplitLines(string text, int[] lineStarts, int start, int length, SemanticTokenType type, SemanticTokenModifier[] modifiers)
        {
            int end = Math.Min(text.Length, start + length);
            int segmentStart = start;
            while (segmentStart < end)
            {
                int nl = text.IndexOf('\n', segmentStart, end - segmentStart);
                int segmentEnd = nl < 0 ? end : nl;
                int segmentLength = segmentEnd - segmentStart;
                if (segmentLength > 0)
                {
                    (int line, int character) = Position(lineStarts, segmentStart);
                    yield return new DrapoToken
                    {
                        Line = line,
                        Character = character,
                        Length = segmentLength,
                        Offset = segmentStart,
                        Type = type,
                        Modifiers = modifiers
                    };
                }
                segmentStart = segmentEnd + 1;
            }
        }
    }
}
