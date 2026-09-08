using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Drapo.Tooling.Helpers
{
    /// <summary>
    /// Parsing of <c>d-on-*</c> handler expressions shared by the template validator and the
    /// language server, so both interpret function calls and argument lists identically.
    /// </summary>
    public static class DrapoHandlerSyntax
    {
        private static readonly Regex FunctionCallRegex = new Regex(@"([A-Za-z_]\w*)\s*\(", RegexOptions.Compiled);

        /// <summary>A function call found in a handler expression.</summary>
        public sealed class FunctionCall
        {
            /// <summary>Function name as written.</summary>
            public string Name { get; set; }
            /// <summary>Index of the name within the handler expression.</summary>
            public int NameIndex { get; set; }
            /// <summary>Raw text between the call's parentheses (may be unterminated).</summary>
            public string Arguments { get; set; }
        }

        /// <summary>
        /// Scans a handler expression for function calls, returning each name, its position, and
        /// its raw argument text. Nested calls are returned too (each is validated independently).
        /// </summary>
        public static IEnumerable<FunctionCall> ScanFunctionCalls(string value)
        {
            if (string.IsNullOrEmpty(value))
                yield break;
            foreach (Match m in FunctionCallRegex.Matches(value))
            {
                int parenIndex = m.Index + m.Length - 1;
                int depth = 0;
                int close = -1;
                for (int i = parenIndex; i < value.Length; i++)
                {
                    if (value[i] == '(') depth++;
                    else if (value[i] == ')') { depth--; if (depth == 0) { close = i; break; } }
                }
                string args = close > parenIndex ? value.Substring(parenIndex + 1, close - parenIndex - 1) : value.Substring(parenIndex + 1);
                yield return new FunctionCall
                {
                    Name = m.Groups[1].Value,
                    NameIndex = m.Groups[1].Index,
                    Arguments = args
                };
            }
        }

        /// <summary>
        /// Splits a function argument string at top-level commas, ignoring commas nested inside
        /// parentheses or mustache expressions. Empty/whitespace input yields an empty list.
        /// </summary>
        public static List<string> SplitArguments(string args)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(args))
                return result;
            int depth = 0;
            int mustache = 0;
            int start = 0;
            for (int i = 0; i < args.Length; i++)
            {
                if (i + 1 < args.Length && args[i] == '{' && args[i + 1] == '{') { mustache++; i++; continue; }
                if (i + 1 < args.Length && args[i] == '}' && args[i + 1] == '}') { if (mustache > 0) mustache--; i++; continue; }
                if (mustache > 0) continue;
                char c = args[i];
                if (c == '(') depth++;
                else if (c == ')') { if (depth > 0) depth--; }
                else if (c == ',' && depth == 0)
                {
                    result.Add(args.Substring(start, i - start));
                    start = i + 1;
                }
            }
            result.Add(args.Substring(start));
            return result;
        }
    }
}
