using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Drapo.Tooling.Models;
using Newtonsoft.Json;
using Xunit;

namespace Drapo.Tests.Tooling
{
    /// <summary>
    /// The parity corpus: hand-written fixtures with their expected diagnostics. The same corpus
    /// is reused by the language-server parity tests.
    /// </summary>
    public static class FixtureCorpus
    {
        public static string Directory => Path.Combine(System.AppContext.BaseDirectory, "Fixtures");

        public static IEnumerable<object[]> Names() =>
            System.IO.Directory.GetFiles(Directory, "*.html").OrderBy(f => f)
                .Select(f => new object[] { Path.GetFileNameWithoutExtension(f) });

        public static string Html(string name) => File.ReadAllText(Path.Combine(Directory, name + ".html"));

        public static List<DrapoDiagnosticVM> Expected(string name) =>
            JsonConvert.DeserializeObject<List<DrapoDiagnosticVM>>(File.ReadAllText(Path.Combine(Directory, name + ".json")));
    }

    public class ValidatorTests
    {
        [Theory]
        [MemberData(nameof(FixtureCorpus.Names), MemberType = typeof(FixtureCorpus))]
        public async Task FixtureYieldsExpectedDiagnostics(string name)
        {
            DrapoValidationResultVM result = await TestContentRoot.Validator().Validate(FixtureCorpus.Html(name));
            List<DrapoDiagnosticVM> expected = FixtureCorpus.Expected(name);

            Assert.Equal(expected.Count, result.Diagnostics.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                DrapoDiagnosticVM e = expected[i], a = result.Diagnostics[i];
                Assert.Equal(e.Level, a.Level);
                Assert.Equal(e.Rule, a.Rule);
                Assert.Equal(e.Line, a.Line);
                Assert.Equal(e.Column, a.Column);
                Assert.Equal(e.Message, a.Message);
            }
            Assert.Equal(expected.Count(d => d.Level == "error"), result.ErrorCount);
            Assert.Equal(expected.Count(d => d.Level == "warning"), result.WarningCount);
            Assert.Equal(result.ErrorCount == 0, result.Valid);
        }

        [Fact]
        public async Task EmptyAndNullInputAreValid()
        {
            Assert.True((await TestContentRoot.Validator().Validate("")).Valid);
            Assert.True((await TestContentRoot.Validator().Validate(null)).Valid);
        }
    }

    public class FunctionSamplesValidateTest
    {
        public static IEnumerable<object[]> Samples() =>
            Directory.GetDirectories(Path.Combine(TestContentRoot.AppPath, "functions"))
                .SelectMany(f => Directory.Exists(Path.Combine(f, "samples")) ? Directory.GetDirectories(Path.Combine(f, "samples")) : new string[0])
                .Select(s => Path.Combine(s, "content.html"))
                .Where(File.Exists)
                .OrderBy(p => p)
                .Select(p => new object[] { Path.GetRelativePath(TestContentRoot.AppPath, p) });

        /// <summary>Constitution Principle III: every documented sample is valid Drapo.</summary>
        [Theory]
        [MemberData(nameof(Samples))]
        public async Task EveryFunctionSampleHasNoErrors(string relativePath)
        {
            string html = await File.ReadAllTextAsync(Path.Combine(TestContentRoot.AppPath, relativePath));
            DrapoValidationResultVM result = await TestContentRoot.Validator().Validate(html);
            Assert.True(result.Valid, string.Join("\n", result.Diagnostics.Select(d => $"{d.Level} {d.Rule} {d.Line}:{d.Column} {d.Message}")));
        }
    }
}
