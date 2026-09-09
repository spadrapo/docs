using Drapo.LanguageServer.Handlers;
using OmniSharp.Extensions.LanguageServer.Protocol;
using Xunit;

namespace Drapo.Tests.LanguageServer
{
    /// <summary>Clients other than VS Code send their own language ids (Visual Studio sends its content type).</summary>
    public class LanguageIdTests
    {
        [Theory]
        [InlineData("file:///c:/site/page.html", "html", "html")]
        [InlineData("file:///c:/site/page.html", "HTML", "html")]
        [InlineData("file:///c:/site/page.htm", "HTML", "html")]
        [InlineData("file:///c:/site/page.html", null, "html")]
        [InlineData("file:///c:/site/page.html", "anything-else", "html")]
        [InlineData("file:///c:/site/Views/Index.cshtml", "LegacyRazorCSharp", "aspnetcorerazor")]
        [InlineData("file:///c:/site/Views/Index.cshtml", "razor", "razor")]
        [InlineData("file:///c:/site/Component.razor", "Razor", "razor")]
        [InlineData("file:///c:/site/Component.razor", "aspnetcorerazor", "aspnetcorerazor")]
        public void UnknownIdsAreMappedByExtensionKnownIdsAreKept(string uri, string sent, string expected)
        {
            Assert.Equal(expected, TextDocumentSyncHandler.NormalizeLanguageId(DocumentUri.Parse(uri), sent));
        }
    }
}
