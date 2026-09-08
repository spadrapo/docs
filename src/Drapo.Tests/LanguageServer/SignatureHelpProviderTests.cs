using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace Drapo.Tests.LanguageServer
{
    public class SignatureHelpProviderTests
    {
        private static SignatureHelp HelpAfter(string text, string marker) =>
            ServerFixture.SignatureHelp.GetSignatureHelp(text, ServerFixture.After(text, marker));

        [Fact]
        public void FirstParameterActiveRightAfterOpenParen()
        {
            SignatureHelp help = HelpAfter("<button d-on-click=\"UpdateSector(\">", "UpdateSector(");
            Assert.NotNull(help);
            Assert.Equal(0, help.ActiveParameter);
            Assert.Equal(0, help.ActiveSignature);
            SignatureInformation sig = Assert.Single(help.Signatures);
            Assert.StartsWith("UpdateSector(SectorName: text, Url: url", sig.Label);
            Assert.Equal(6, sig.Parameters.Count());
            Assert.Equal("SectorName", sig.Parameters.First().Label.Label);
            Assert.Contains("types: text", sig.Parameters.First().Documentation.String);
            Assert.Contains("optional", sig.Parameters.ElementAt(2).Documentation.String);
            Assert.Contains("default: null", sig.Parameters.ElementAt(2).Documentation.String);
        }

        [Fact]
        public void SecondParameterActiveAfterComma()
        {
            SignatureHelp help = HelpAfter("<button d-on-click=\"UpdateSector(a, \">", "UpdateSector(a, ");
            Assert.NotNull(help);
            Assert.Equal(1, help.ActiveParameter);
        }

        [Fact]
        public void NestedCallCommasDoNotAdvanceOuterParameter()
        {
            SignatureHelp help = HelpAfter("<button d-on-click=\"UpdateSector(a, Notify(b, c), \">", "Notify(b, c), ");
            Assert.NotNull(help);
            Assert.Equal("UpdateSector", help.Signatures.Single().Label.Substring(0, 12));
            Assert.Equal(2, help.ActiveParameter);
        }

        [Fact]
        public void InnermostCallWins()
        {
            SignatureHelp help = HelpAfter("<button d-on-click=\"UpdateSector(a, Notify(\">", "Notify(");
            Assert.NotNull(help);
            Assert.StartsWith("Notify(", help.Signatures.Single().Label);
            Assert.Equal(0, help.ActiveParameter);
        }

        [Fact]
        public void MustacheCommasDoNotAdvanceParameter()
        {
            SignatureHelp help = HelpAfter("<button d-on-click=\"UpdateSector({{a,b}}\">", "{{a,b}}");
            Assert.NotNull(help);
            Assert.Equal(0, help.ActiveParameter);
        }

        [Fact]
        public void ActiveParameterIsClampedForVariadicOveruse()
        {
            SignatureHelp help = HelpAfter("<button d-on-click=\"Notify(a, b, c, \">", "Notify(a, b, c, ");
            Assert.NotNull(help);
            Assert.Equal(0, help.ActiveParameter); // Notify documents a single parameter
        }

        [Fact]
        public void NullForUnknownCallee()
        {
            Assert.Null(HelpAfter("<button d-on-click=\"Unknown(\">", "Unknown("));
        }

        [Fact]
        public void NullOutsideHandlerValues()
        {
            Assert.Null(HelpAfter("<div d-if=\"UpdateSector(\">", "UpdateSector("));
            Assert.Null(HelpAfter("<p>UpdateSector(</p>", "UpdateSector("));
        }

        [Fact]
        public void NullAfterCallIsClosed()
        {
            Assert.Null(HelpAfter("<button d-on-click=\"UpdateSector(a, b)\">", "UpdateSector(a, b)"));
        }

        [Fact]
        public void WorksWithMultipleStatements()
        {
            SignatureHelp help = HelpAfter("<button d-on-click=\"Notify(x);UpdateSector(a, \">", "UpdateSector(a, ");
            Assert.NotNull(help);
            Assert.StartsWith("UpdateSector(", help.Signatures.Single().Label);
            Assert.Equal(1, help.ActiveParameter);
        }
    }
}
