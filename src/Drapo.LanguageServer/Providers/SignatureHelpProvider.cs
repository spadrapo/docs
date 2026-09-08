using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Drapo.LanguageServer.Documents;
using Drapo.Tooling.Helpers;
using Drapo.Tooling.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Drapo.LanguageServer.Providers
{
    /// <summary>Signature help for documented function calls inside d-on-* handler values.</summary>
    public sealed class SignatureHelpProvider
    {
        private readonly DrapoSymbolIndex _index;

        public SignatureHelpProvider(DrapoSymbolIndex index)
        {
            _index = index;
        }

        public SignatureHelp GetSignatureHelp(string text, Position position)
        {
            text ??= string.Empty;
            int offset = TextPosition.OffsetOf(text, position.Line, position.Character);
            if (!TextPosition.TryGetEnclosingAttributeValue(text, offset, out string attrName, out int valueStart, out _)
                || !attrName.StartsWith("d-on-", StringComparison.OrdinalIgnoreCase))
                return null;

            // Walk backwards from the cursor to the innermost unmatched '(' within the value.
            int depth = 0;
            int open = -1;
            for (int i = offset - 1; i >= valueStart; i--)
            {
                char c = text[i];
                if (c == ')') depth++;
                else if (c == '(')
                {
                    if (depth == 0) { open = i; break; }
                    depth--;
                }
            }
            if (open < 0)
                return null;

            // Callee: identifier immediately before '(' (allowing whitespace).
            int nameEnd = open;
            while (nameEnd > valueStart && char.IsWhiteSpace(text[nameEnd - 1]))
                nameEnd--;
            string callee = TextPosition.WordBefore(text, nameEnd);
            if (callee.Length == 0 || !_index.TryGetFunction(callee, out FunctionVM function))
                return null;

            string argsSoFar = text.Substring(open + 1, offset - open - 1);
            List<string> parts = DrapoHandlerSyntax.SplitArguments(argsSoFar);
            int active = Math.Max(0, parts.Count - 1);
            if (function.Parameters.Count > 0)
                active = Math.Min(active, function.Parameters.Count - 1);
            else
                active = 0;

            var parameters = function.Parameters.Select(p => new ParameterInformation
            {
                Label = new ParameterInformationLabel(p.Name),
                Documentation = new StringOrMarkupContent(ParameterDoc(p))
            }).ToList();

            return new SignatureHelp
            {
                Signatures = new Container<SignatureInformation>(new SignatureInformation
                {
                    Label = function.Signature ?? DrapoDocContent.BuildFunctionSignature(function.Name, function.Parameters),
                    Documentation = string.IsNullOrWhiteSpace(function.Description) ? null : new StringOrMarkupContent(new MarkupContent { Kind = MarkupKind.Markdown, Value = function.Description.Trim() }),
                    Parameters = new Container<ParameterInformation>(parameters)
                }),
                ActiveSignature = 0,
                ActiveParameter = active
            };
        }

        private static string ParameterDoc(FunctionParameterVM p)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(p.Description))
                sb.Append(p.Description.Trim());
            var meta = new List<string>();
            if (p.Types != null && p.Types.Count > 0)
                meta.Add("types: " + string.Join("|", p.Types));
            if (p.Optional)
                meta.Add("optional");
            if (!string.IsNullOrEmpty(p.DefaultValue))
                meta.Add("default: " + p.DefaultValue);
            if (meta.Count > 0)
                sb.Append(sb.Length > 0 ? " " : "").Append('(').Append(string.Join(", ", meta)).Append(')');
            return sb.ToString();
        }
    }
}
