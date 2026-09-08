using System;
using System.Text;
using Drapo.LanguageServer.Documents;
using Drapo.Tooling.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Drapo.LanguageServer.Providers
{
    /// <summary>Hover documentation for d-* attributes and for functions inside d-on-* handlers.</summary>
    public sealed class HoverProvider
    {
        private readonly DrapoSymbolIndex _index;

        public HoverProvider(DrapoSymbolIndex index)
        {
            _index = index;
        }

        public Hover GetHover(string text, Position position)
        {
            text ??= string.Empty;
            int offset = TextPosition.OffsetOf(text, position.Line, position.Character);
            if (!TextPosition.TryIdentifierAt(text, offset, out int start, out int end))
                return null;
            string token = text.Substring(start, end - start);
            var range = new Range(ToPosition(text, start), ToPosition(text, end));

            // Function inside a d-on-* handler value?
            if (TextPosition.TryGetEnclosingAttributeValue(text, offset, out string attrName, out _, out _)
                && attrName.StartsWith("d-on-", StringComparison.OrdinalIgnoreCase))
            {
                if (_index.TryGetFunction(token, out FunctionVM function))
                    return new Hover { Range = range, Contents = Markdown(FunctionMarkdown(function)) };
                return null;
            }

            // Attribute name inside a tag?
            if (token.StartsWith("d-", StringComparison.OrdinalIgnoreCase) && TextPosition.IsInsideTag(text, offset))
            {
                if (_index.TryGetAttribute(token, out AttributeVM attribute))
                    return new Hover { Range = range, Contents = Markdown($"**{attribute.Name}**\n\n{attribute.Description}") };
                if (_index.Catalog.IsValidAttribute(token))
                    return new Hover { Range = range, Contents = Markdown($"**{token}**\n\n_Recognised by the Drapo engine; no documentation page._") };
            }
            return null;
        }

        public static string FunctionMarkdown(FunctionVM function)
        {
            var sb = new StringBuilder();
            sb.Append('`').Append(function.Signature ?? function.Name + "()").Append('`').Append("\n\n");
            if (!string.IsNullOrWhiteSpace(function.Description))
                sb.Append(function.Description.Trim()).Append("\n\n");
            if (function.Parameters != null && function.Parameters.Count > 0)
            {
                sb.Append("| Parameter | Types | Optional | Default | Description |\n");
                sb.Append("|---|---|---|---|---|\n");
                foreach (FunctionParameterVM p in function.Parameters)
                {
                    string types = p.Types != null && p.Types.Count > 0 ? string.Join(", ", p.Types) : "any";
                    sb.Append("| ").Append(Cell(p.Name))
                      .Append(" | ").Append(Cell(types))
                      .Append(" | ").Append(p.Optional ? "yes" : "no")
                      .Append(" | ").Append(Cell(p.DefaultValue))
                      .Append(" | ").Append(Cell(p.Description))
                      .Append(" |\n");
                }
            }
            return sb.ToString().TrimEnd();
        }

        private static string Cell(string value) => string.IsNullOrEmpty(value) ? "" : value.Replace("|", "\\|").Replace("\n", " ");

        private static MarkedStringsOrMarkupContent Markdown(string value) =>
            new MarkedStringsOrMarkupContent(new MarkupContent { Kind = MarkupKind.Markdown, Value = value });

        private static Position ToPosition(string text, int offset)
        {
            (int line, int character) = TextPosition.PositionOf(text, offset);
            return new Position(line, character);
        }
    }
}
