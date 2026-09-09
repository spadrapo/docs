using System.ComponentModel.Composition;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;

namespace Drapo.VisualStudio
{
    /// <summary>
    /// Makes the built-in <c>HTML</c> content type (what Visual Studio assigns to .html/.htm files)
    /// also derive from <c>languageserver-base</c>.
    /// </summary>
    /// <remarks>
    /// Every LSP client part inside Visual Studio (completion, hover, signature help, diagnostics,
    /// semantic tokens, the document listener that sends <c>didOpen</c>) is exported for the
    /// <c>languageserver-base</c> content type, and <c>HTML</c> only derives from <c>text</c>.
    /// The content type registry merges same-name definitions, so exporting a second definition
    /// named <c>HTML</c> with this extra base adds the base without redefining HTML. Without it an
    /// <see cref="ILanguageClient"/> for <c>HTML</c> is never activated. Verified against
    /// VS 2022 17.14 and VS 2026 18.7 (specs/004-lsp-improvements/research.md, R3).
    /// </remarks>
    internal static class HtmlContentType
    {
        public const string Name = "HTML";

        [Export]
        [Name(Name)]
        [BaseDefinition(CodeRemoteContentDefinition.RemoteBaseTypeName)]
        internal static ContentTypeDefinition? HtmlAsLanguageServerBase = null; // MEF reads the attributes, never the value
    }
}
