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
