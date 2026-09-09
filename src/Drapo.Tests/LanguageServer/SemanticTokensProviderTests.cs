using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Drapo.LanguageServer.Providers;
using Drapo.Tooling.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace Drapo.Tests.LanguageServer
{
    /// <summary>
    /// FR-019/FR-020/FR-023: the four classifications, sourced from the same catalog as the
    /// diagnostics, over the same text-based reading of the document.
    /// </summary>
    public class SemanticTokensProviderTests
    {
        private static List<DrapoToken> Tokens(string text) => ServerFixture.SemanticTokens.GetTokens(text);

        private static string Describe(DrapoToken t) =>
            $"{t.Line}:{t.Character}+{t.Length} {(string)t.Type}{(t.Modifiers.Count > 0 ? "." + string.Join(".", t.Modifiers.Select(m => (string)m)) : "")}";

        [Fact]
        public void ClassifiesKnownAndUnknownAttributesFunctionsAndMustaches()
        {
            string text = "<div d-nope=\"x\" d-if=\"{{show}}\"><button d-on-click=\"UpdateSector(s,~/u);Bogus(1)\">{{title}}</button></div>";
            List<string> tokens = Tokens(text).Select(Describe).ToList();
            Assert.Equal(new[]
            {
                "0:5+6 keyword.unknown",               // d-nope
                "0:16+4 keyword.defaultLibrary",       // d-if
                "0:22+8 variable",                     // {{show}}
                "0:40+10 keyword.defaultLibrary",      // d-on-click
                "0:52+12 function.defaultLibrary",     // UpdateSector
                "0:72+5 function.unknown",             // Bogus
                "0:82+9 variable",                     // {{title}}
            }, tokens);
        }

        [Fact]
        public async Task KnownUnknownAgreesWithTheValidator()
        {
            string text = "<div d-nope=\"x\" d-if=\"1\"><button d-on-click=\"Bogus(1);UpdateSector(a,b)\">x</button></div>";
            DrapoValidationResultVM result = await TestContentRoot.Validator().Validate(text);
            var unknownByColumn = result.Diagnostics
                .Where(d => d.Rule == "unknown-attribute" || d.Rule == "unknown-function")
                .Select(d => d.Column - 1).OrderBy(c => c).ToList();
            var unknownTokens = Tokens(text)
                .Where(t => t.Modifiers.Contains(DrapoSemanticTokens.UnknownModifier))
                .Select(t => t.Character).OrderBy(c => c).ToList();
            Assert.Equal(unknownByColumn, unknownTokens);
        }

        [Fact]
        public void ValuelessAttributeInsideTagIsClassifiedButProseIsNot()
        {
            string text = "<p>Use d-if to hide things.</p>\n<div d-router d-if=\"1\"></div>";
            List<string> tokens = Tokens(text).Select(Describe).ToList();
            Assert.DoesNotContain(tokens, t => t.StartsWith("0:"));
            Assert.Contains("1:5+8 keyword.unknown", tokens);   // d-router is not an engine attribute
            Assert.Contains("1:14+4 keyword.defaultLibrary", tokens);
        }

        [Fact]
        public void DynamicPrefixesAreKnown()
        {
            string text = "<div d-attr-class=\"a\" d-on-mouseover=\"UpdateSector(a,b)\"></div>";
            List<string> tokens = Tokens(text).Select(Describe).ToList();
            Assert.Contains("0:5+12 keyword.defaultLibrary", tokens);
            Assert.Contains("0:22+14 keyword.defaultLibrary", tokens);
        }

        [Fact]
        public void MultiLineMustacheIsSplitPerLine()
        {
            string text = "<span>{{a\n.b}}</span>";
            List<string> tokens = Tokens(text).Select(Describe).ToList();
            Assert.Equal(new[] { "0:6+3 variable", "1:0+4 variable" }, tokens);
        }

        [Fact]
        public void NestedMustachesAreOneToken()
        {
            string text = "<span>{{{{outer}}.x}}</span>";
            Assert.Equal(new[] { "0:6+15 variable" }, Tokens(text).Select(Describe));
        }

        [Fact]
        public void UnbalancedAndEmptyInputsNeverThrow()
        {
            Assert.Empty(Tokens(null));
            Assert.Empty(Tokens(""));
            Assert.Empty(Tokens("}} {{ unclosed"));
            Assert.Empty(Tokens("<div d-if=\"unterminated></div>"));
            Assert.Empty(Tokens("plain text with d-if in prose and (parens)"));
        }

        [Fact]
        public void TokensNeverOverlapAndAreOrdered()
        {
            string text = "<div d-on-click=\"Foo({{x}},Bar({{y}}))\" d-if=\"{{a}}{{b}}\">{{c}}</div>";
            List<DrapoToken> tokens = Tokens(text);
            for (int i = 1; i < tokens.Count; i++)
            {
                Assert.True(tokens[i - 1].Offset + tokens[i - 1].Length <= tokens[i].Offset,
                    $"overlap between {Describe(tokens[i - 1])} and {Describe(tokens[i])}");
            }
        }

        [Fact]
        public void LegendIsStableAndStandard()
        {
            Assert.Equal(new[] { "keyword", "function", "variable" }, DrapoSemanticTokens.TokenTypes.Select(t => (string)t));
            Assert.Equal(new[] { "defaultLibrary", "unknown" }, DrapoSemanticTokens.TokenModifiers.Select(m => (string)m));
        }

        /// <summary>SC-007: a 5,000-line document tokenizes well under the typing budget.</summary>
        [Fact]
        public void LargeDocumentIsFast()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 5000; i++)
                sb.Append("<div d-if=\"{{show").Append(i).Append("}}\" d-on-click=\"UpdateSector(s,~/u,{{t}})\">{{item.name}} text</div>\n");
            string text = sb.ToString();
            Tokens(text); // warm up
            var sw = Stopwatch.StartNew();
            List<DrapoToken> tokens = Tokens(text);
            sw.Stop();
            Assert.Equal(5000 * 6, tokens.Count); // d-if, {{showN}}, d-on-click, UpdateSector, {{t}}, {{item.name}}
            Assert.True(sw.ElapsedMilliseconds < 1000, $"took {sw.ElapsedMilliseconds} ms");
        }
    }
}
