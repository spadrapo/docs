using System;
using System.Text.RegularExpressions;

namespace Drapo.LanguageServer.Documents
{
    /// <summary>
    /// Offset/position arithmetic and lightweight context detection over raw document text.
    /// Deliberately regex/scan based (no HTML parser) so that it interprets documents the same
    /// way the validator does. Lines are separated by '\n' (a preceding '\r' is ordinary text).
    /// </summary>
    public static class TextPosition
    {
        private const char DoubleQuote = '"';
        private const char SingleQuote = (char)39;

        /// <summary>Same attribute pattern the validator uses: a valued d-* attribute inside a tag.</summary>
        public static readonly Regex AttributeRegex = new Regex(
            @"(?<=\s)(d-[A-Za-z][\w-]*)\s*=\s*(?:""([^""]*)""|'([^']*)')",
            RegexOptions.Compiled);

        public static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c == '_' || c == '-';

        /// <summary>0-based (line, character) → offset. Clamps to the line and text length.</summary>
        public static int OffsetOf(string text, int line, int character)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
            int offset = 0;
            for (int l = 0; l < line; l++)
            {
                int nl = text.IndexOf('\n', offset);
                if (nl < 0)
                    return text.Length;
                offset = nl + 1;
            }
            int lineEnd = LineEnd(text, offset);
            return Math.Min(offset + Math.Max(0, character), lineEnd);
        }

        /// <summary>Offset → 0-based (line, character).</summary>
        public static (int line, int character) PositionOf(string text, int offset)
        {
            int line = 0, character = 0;
            int max = Math.Min(Math.Max(0, offset), text?.Length ?? 0);
            for (int i = 0; i < max; i++)
            {
                if (text[i] == '\n') { line++; character = 0; }
                else character++;
            }
            return (line, character);
        }

        /// <summary>Offset of the end of the line containing <paramref name="offset"/> (the '\n' or text end).</summary>
        public static int LineEnd(string text, int offset)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
            int nl = text.IndexOf('\n', Math.Min(Math.Max(0, offset), text.Length));
            return nl < 0 ? text.Length : nl;
        }

        /// <summary>End offset (exclusive) of the identifier run starting at <paramref name="offset"/>.</summary>
        public static int IdentifierRunEnd(string text, int offset)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
            int i = Math.Max(0, offset);
            while (i < text.Length && IsIdentifierChar(text[i]))
                i++;
            return i;
        }

        /// <summary>The identifier run ending at <paramref name="offset"/> (may be empty).</summary>
        public static string WordBefore(string text, int offset)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            int end = Math.Min(Math.Max(0, offset), text.Length);
            int start = end;
            while (start > 0 && IsIdentifierChar(text[start - 1]))
                start--;
            return text.Substring(start, end - start);
        }

        /// <summary>The maximal identifier run around <paramref name="offset"/>; false if none.</summary>
        public static bool TryIdentifierAt(string text, int offset, out int start, out int end)
        {
            start = end = 0;
            if (string.IsNullOrEmpty(text))
                return false;
            int o = Math.Min(Math.Max(0, offset), text.Length);
            int s = o;
            while (s > 0 && IsIdentifierChar(text[s - 1]))
                s--;
            int e = o;
            while (e < text.Length && IsIdentifierChar(text[e]))
                e++;
            if (e == s)
                return false;
            start = s; end = e;
            return true;
        }

        /// <summary>
        /// True when the cursor is inside a tag (after the nearest '&lt;' with no '&gt;' in between)
        /// and not inside a quoted attribute value.
        /// </summary>
        public static bool IsInsideTag(string text, int offset)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            int o = Math.Min(Math.Max(0, offset), text.Length);
            if (o == 0)
                return false;
            int lt = text.LastIndexOf('<', o - 1);
            if (lt < 0)
                return false;
            int gt = text.LastIndexOf('>', o - 1);
            if (gt > lt)
                return false;
            // Not inside a quoted value: track quotes between '<' and the cursor.
            char quote = '\0';
            for (int i = lt; i < o; i++)
            {
                char c = text[i];
                if (quote == '\0' && (c == DoubleQuote || c == SingleQuote))
                    quote = c;
                else if (c == quote)
                    quote = '\0';
            }
            return quote == '\0';
        }

        /// <summary>
        /// If <paramref name="offset"/> lies within the quoted value of a d-* attribute, returns the
        /// attribute name and the value's [start, end) offsets.
        /// </summary>
        public static bool TryGetEnclosingAttributeValue(string text, int offset, out string name, out int valueStart, out int valueEnd)
        {
            name = null; valueStart = valueEnd = 0;
            if (string.IsNullOrEmpty(text))
                return false;
            int o = Math.Min(Math.Max(0, offset), text.Length);
            // Only scan the current tag: start from the nearest '<' before the cursor.
            int lt = o == 0 ? -1 : text.LastIndexOf('<', o - 1);
            int scanStart = lt < 0 ? 0 : lt;
            foreach (Match m in AttributeRegex.Matches(text, scanStart))
            {
                if (m.Index > o)
                    break;
                Group g = m.Groups[2].Success ? m.Groups[2] : m.Groups[3];
                if (o >= g.Index && o <= g.Index + g.Length)
                {
                    name = m.Groups[1].Value;
                    valueStart = g.Index;
                    valueEnd = g.Index + g.Length;
                    return true;
                }
            }
            return false;
        }
    }
}
