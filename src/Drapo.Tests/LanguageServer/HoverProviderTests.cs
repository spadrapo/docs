using System;
using System.Linq;
using System.Threading.Tasks;
using Drapo.Tooling.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using Xunit;

namespace Drapo.Tests.LanguageServer
{
    public class HoverProviderTests
    {
        private static Hover HoverAt(string text, string marker, int delta = 1) =>
            ServerFixture.Hover.GetHover(text, ServerFixture.At(text, marker, delta));

        [Fact]
        public async Task AttributeHoverShowsDocumentedDescription()
        {
            string text = "<ul d-for=\"item in {{items}}\"></ul>";
            Hover hover = HoverAt(text, "d-for", 2);
            Assert.NotNull(hover);
            AttributeVM doc = await TestContentRoot.Attributes().Get("d-for");
            string md = hover.Contents.MarkupContent.Value;
            Assert.StartsWith("**d-for**", md);
            Assert.Contains(doc.Description.Substring(0, 30), md);
            Assert.Equal(new Range(new Position(0, 4), new Position(0, 9)), hover.Range);
        }

        [Fact]
        public void FunctionHoverShowsSignatureAndParameterTable()
        {
            string text = "<button d-on-click=\"UpdateSector('s', '~/u')\">Go</button>";
            Hover hover = HoverAt(text, "UpdateSector", 3);
            Assert.NotNull(hover);
            string md = hover.Contents.MarkupContent.Value;
            Assert.Contains("`UpdateSector(SectorName: text, Url: url, [Title: text = null]", md);
            Assert.Contains("| Parameter | Types | Optional | Default | Description |", md);
            Assert.Contains("| Title | text | yes | null |", md);
            Assert.Contains("sector", md, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void FunctionHoverIsCaseInsensitive()
        {
            string text = "<button d-on-click=\"updatesector('s', '~/u')\">Go</button>";
            Assert.NotNull(HoverAt(text, "updatesector", 3));
        }

        [Fact]
        public void NoHoverOnPlainText()
        {
            Assert.Null(HoverAt("<p>UpdateSector d-for</p>", "UpdateSector", 3));
            Assert.Null(HoverAt("<p>UpdateSector d-for</p>", "d-for", 2));
        }

        [Fact]
        public void NoHoverOnUnknownAttributeOrFunction()
        {
            Assert.Null(HoverAt("<div d-nope=\"1\"></div>", "d-nope", 2));
            Assert.Null(HoverAt("<div d-on-click=\"Nope()\"></div>", "Nope", 1));
        }

        [Fact]
        public void NoFunctionHoverOutsideHandlerValues()
        {
            // Function-looking token inside a non d-on-* value: not a handler, so nothing.
            Assert.Null(HoverAt("<div d-if=\"UpdateSector\"></div>", "UpdateSector", 3));
        }

        [Fact]
        public void EngineOnlyAttributeGetsRecognisedNote()
        {
            string undocumented = ServerFixture.Index.EngineAttributes
                .FirstOrDefault(a => !ServerFixture.Index.Attributes.ContainsKey(a));
            if (undocumented == null)
                return; // every engine attribute is documented; nothing to check
            string text = $"<div {undocumented}=\"1\"></div>";
            Hover hover = HoverAt(text, undocumented, 2);
            Assert.NotNull(hover);
            Assert.Contains("Recognised by the Drapo engine", hover.Contents.MarkupContent.Value);
        }
    }
}

namespace Drapo.Tests.LanguageServer
{
    /// <summary>Visual Studio only accepts plaintext hover/signature documentation (traced from VS 2026 18.7).</summary>
    public class PlainTextRenderingTests
    {
        [Fact]
        public void FunctionHoverInPlainTextHasNoMarkdownAndListsParameters()
        {
            string text = "<button d-on-click=\"UpdateSector('s', '~/u')\">Go</button>";
            Hover hover = ServerFixture.Hover.GetHover(text, ServerFixture.At(text, "UpdateSector", 3), MarkupKind.PlainText);
            Assert.NotNull(hover);
            Assert.Equal(MarkupKind.PlainText, hover.Contents.MarkupContent.Kind);
            string value = hover.Contents.MarkupContent.Value;
            Assert.StartsWith("UpdateSector(", value);
            Assert.Contains("Parameters:", value);
            Assert.Contains("  Title (text, optional, default null)", value);
            Assert.DoesNotContain("| Parameter |", value); // no Markdown table (the signature itself contains type alternatives such as mustache|text)
            Assert.DoesNotContain("**", value);
            Assert.DoesNotContain("`", value);
        }

        [Fact]
        public void AttributeHoverInPlainTextKeepsNameAndDescription()
        {
            string text = "<ul d-for=\"item in {{items}}\"></ul>";
            Hover hover = ServerFixture.Hover.GetHover(text, ServerFixture.At(text, "d-for", 2), MarkupKind.PlainText);
            Assert.StartsWith("d-for\n\n", hover.Contents.MarkupContent.Value);
            Assert.DoesNotContain("**", hover.Contents.MarkupContent.Value);
        }

        [Fact]
        public void SignatureHelpInPlainTextUsesPlainDocumentation()
        {
            string text = "<button d-on-click=\"UpdateSector(\">Go</button>";
            SignatureHelp help = ServerFixture.SignatureHelp.GetSignatureHelp(text, ServerFixture.After(text, "UpdateSector("), MarkupKind.PlainText);
            Assert.NotNull(help);
            SignatureInformation sig = help.Signatures.First();
            Assert.Equal(MarkupKind.PlainText, sig.Documentation.MarkupContent.Kind);
            Assert.DoesNotContain("`", sig.Documentation.MarkupContent.Value);
        }

        [Theory]
        [InlineData("**bold** and `code`", "bold and code")]
        [InlineData("see [the guide](https://x/y) now", "see the guide now")]
        [InlineData("- one\n* two", "- one\n- two")]
        [InlineData("_optional_ value", "optional value")]
        [InlineData("snake_case_name stays", "snake_case_name stays")]
        public void ToPlainTextStripsMarkdown(string markdown, string expected)
        {
            Assert.Equal(expected, Drapo.Tooling.Helpers.DrapoDocContent.ToPlainText(markdown));
        }
    }
}
