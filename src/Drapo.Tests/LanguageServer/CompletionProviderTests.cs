using System;
using System.Collections.Generic;
using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using Xunit;

namespace Drapo.Tests.LanguageServer
{
    public class CompletionProviderTests
    {
        private static CompletionList Complete(string text, string marker, int delta = 0) =>
            ServerFixture.Completion.GetCompletions(text, ServerFixture.At(text, marker, delta));

        [Fact]
        public void FixedItemsAreExactlyTheEngineAttributes()
        {
            string text = "<div d-></div>";
            CompletionList list = Complete(text, "d-", 2);
            var fixedLabels = list.Select(i => i.Label.ToLowerInvariant()).ToHashSet();
            // Every engine attribute is offered
            foreach (string engine in ServerFixture.Index.EngineAttributes)
                Assert.Contains(engine, fixedLabels);
            // Nothing is offered that the engine does not recognise
            foreach (CompletionItem item in list)
                Assert.True(TestContentRoot.Catalog.IsValidAttribute(item.Label) || item.Label.EndsWith("-"), item.Label);
            Assert.False(list.IsIncomplete);
        }

        [Fact]
        public void PrefixFamiliesAreOffered()
        {
            CompletionList list = Complete("<div d-></div>", "d-", 2);
            var keywords = list.Where(i => i.Kind == CompletionItemKind.Keyword).Select(i => i.Label).ToList();
            Assert.Contains("d-on-", keywords);
            Assert.Contains("d-attr-", keywords);
            Assert.Contains("d-validation-", keywords);
            Assert.Contains("d-dataproperty-", keywords);
        }

        [Fact]
        public void DocumentedFamilyMemberIsOfferedWithDocumentation()
        {
            CompletionList list = Complete("<input d-></input>", "d-", 2);
            CompletionItem item = Assert.Single(list, i => string.Equals(i.Label, "d-on-model-change", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(item.Documentation);
            Assert.False(string.IsNullOrWhiteSpace(item.Documentation.MarkupContent.Value));
        }

        [Fact]
        public void DForHasDocumentation()
        {
            CompletionList list = Complete("<ul d-></ul>", "d-", 2);
            CompletionItem item = Assert.Single(list, i => i.Label == "d-for");
            Assert.Contains("d-for", item.Documentation.MarkupContent.Value, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("<div>d-</div>", "d-", 2)]           // text content
        [InlineData("<div d-if=\"d-\"></div>", "\"d-", 3)] // inside an attribute value
        [InlineData("plain d- text", "d-", 2)]           // no tag at all
        [InlineData("<div class=\"x\"></div> d-", " d-", 3)] // after a closed tag
        public void NothingOutsideATag(string text, string marker, int delta)
        {
            Assert.Empty(Complete(text, marker, delta));
        }

        [Fact]
        public void NothingForNonDrapoWord()
        {
            Assert.Empty(Complete("<div cla></div>", "cla", 3));
        }

        [Fact]
        public void EmptyWordInsideTagOffersEverything()
        {
            CompletionList list = Complete("<div ></div>", "<div ", 5);
            Assert.NotEmpty(list);
        }

        [Fact]
        public void TextEditReplacesTypedPrefix()
        {
            string text = "<div d-fo></div>";
            CompletionList list = Complete(text, "d-fo", 4);
            CompletionItem item = Assert.Single(list, i => i.Label == "d-for");
            Assert.Equal(new Range(new Position(0, 5), new Position(0, 9)), item.TextEdit.TextEdit.Range);
            Assert.Equal("d-for", item.TextEdit.TextEdit.NewText);
        }

        [Fact]
        public void ItemsAreUniqueCaseInsensitively()
        {
            CompletionList list = Complete("<div d-></div>", "d-", 2);
            var labels = list.Select(i => i.Label.ToLowerInvariant()).ToList();
            Assert.Equal(labels.Count, labels.Distinct().Count());
        }
    }
}
