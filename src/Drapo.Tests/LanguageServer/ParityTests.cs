using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Drapo.LanguageServer.Providers;
using Drapo.Tests.Tooling;
using Drapo.Tooling.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using Xunit;

namespace Drapo.Tests.LanguageServer
{
    /// <summary>
    /// SC-001: the editor diagnostics are the validator diagnostics. Both consume the same
    /// validator; this proves the LSP projection preserves count, position, severity, code and
    /// message for the whole corpus (fixtures + every documented function sample).
    /// </summary>
    public class ParityTests
    {
        public static IEnumerable<object[]> Corpus()
        {
            foreach (object[] n in FixtureCorpus.Names())
                yield return new object[] { "fixture:" + n[0] };
            foreach (object[] s in FunctionSamplesValidateTest.Samples())
                yield return new object[] { "sample:" + s[0] };
        }

        private static string Load(string id)
        {
            return id.StartsWith("fixture:")
                ? FixtureCorpus.Html(id.Substring("fixture:".Length))
                : File.ReadAllText(Path.Combine(TestContentRoot.AppPath, id.Substring("sample:".Length)));
        }

        [Theory]
        [MemberData(nameof(Corpus))]
        public async Task MappedDiagnosticsEqualValidatorDiagnostics(string id)
        {
            string text = Load(id);
            DrapoValidationResultVM result = await TestContentRoot.Validator().Validate(text);
            List<Diagnostic> mapped = DiagnosticMapper.Map(result.Diagnostics, text);

            Assert.Equal(result.Diagnostics.Count, mapped.Count);
            for (int i = 0; i < mapped.Count; i++)
            {
                DrapoDiagnosticVM vm = result.Diagnostics[i];
                Diagnostic d = mapped[i];
                Assert.Equal(vm.Line - 1, d.Range.Start.Line);
                Assert.Equal(vm.Column - 1, d.Range.Start.Character);
                Assert.Equal(vm.Level == "warning" ? DiagnosticSeverity.Warning : DiagnosticSeverity.Error, d.Severity);
                Assert.Equal(vm.Rule, d.Code.Value.String);
                Assert.Equal(vm.Message, d.Message);
                Assert.Equal(DiagnosticMapper.Source, d.Source);
                Assert.Equal(d.Range.Start.Line, d.Range.End.Line);
                Assert.True(d.Range.End.Character > d.Range.Start.Character, "range must be non-empty");
            }
        }

        [Fact]
        public async Task RangeCoversTheOffendingToken()
        {
            string text = "<div d-nope=\"x\"></div>";
            DrapoValidationResultVM result = await TestContentRoot.Validator().Validate(text);
            Diagnostic d = Assert.Single(DiagnosticMapper.Map(result.Diagnostics, text));
            Assert.Equal(new Range(new Position(0, 5), new Position(0, 11)), d.Range); // "d-nope"
        }

        [Fact]
        public async Task MustacheDiagnosticsGetTwoCharacterRange()
        {
            string text = "<span>value}}</span>";
            DrapoValidationResultVM result = await TestContentRoot.Validator().Validate(text);
            Diagnostic d = Assert.Single(DiagnosticMapper.Map(result.Diagnostics, text));
            Assert.Equal(new Range(new Position(0, 11), new Position(0, 13)), d.Range);
        }

        [Fact]
        public async Task CrlfDocumentsKeepColumns()
        {
            string text = "<div>\r\n  <p d-bad=\"1\"></p>\r\n</div>";
            DrapoValidationResultVM result = await TestContentRoot.Validator().Validate(text);
            Diagnostic d = Assert.Single(DiagnosticMapper.Map(result.Diagnostics, text));
            Assert.Equal(new Position(1, 5), d.Range.Start);
            Assert.Equal(new Position(1, 10), d.Range.End);
        }
    }
}
