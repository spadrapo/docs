using System;
using System.Collections.Generic;
using Drapo.LanguageServer.Documents;
using Drapo.Tooling.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Drapo.LanguageServer.Providers
{
    /// <summary>
    /// Projects validator diagnostics (1-based line/column, no length) onto LSP diagnostics.
    /// The VM is never extended: the range end is derived here from the document text so that the
    /// MCP validate_drapo payload stays byte-identical.
    /// </summary>
    public static class DiagnosticMapper
    {
        public const string Source = "drapo";

        public static List<Diagnostic> Map(IEnumerable<DrapoDiagnosticVM> diagnostics, string text)
        {
            var result = new List<Diagnostic>();
            if (diagnostics == null)
                return result;
            text ??= string.Empty;
            foreach (DrapoDiagnosticVM d in diagnostics)
            {
                int line = Math.Max(0, d.Line - 1);
                int character = Math.Max(0, d.Column - 1);
                int startOffset = TextPosition.OffsetOf(text, line, character);
                int lineEnd = TextPosition.LineEnd(text, startOffset);
                int endOffset = TextPosition.IdentifierRunEnd(text, startOffset);
                if (endOffset < startOffset + 2)
                    endOffset = startOffset + 2;
                // Clamp to the line end but always keep at least one character of width.
                endOffset = Math.Min(endOffset, Math.Max(lineEnd, startOffset + 1));
                int endCharacter = character + (endOffset - startOffset);

                result.Add(new Diagnostic
                {
                    Range = new Range(new Position(line, character), new Position(line, endCharacter)),
                    Severity = string.Equals(d.Level, "warning", StringComparison.OrdinalIgnoreCase) ? DiagnosticSeverity.Warning : DiagnosticSeverity.Error,
                    Code = d.Rule,
                    Source = Source,
                    Message = d.Message
                });
            }
            return result;
        }
    }
}
