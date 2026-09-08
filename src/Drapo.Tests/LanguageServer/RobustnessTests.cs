using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Drapo.LanguageServer.Providers;
using Drapo.Tooling.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace Drapo.Tests.LanguageServer
{
    /// <summary>SC-006 / FR-016: malformed input never throws from the validator or any provider.</summary>
    public class RobustnessTests
    {
        public static IEnumerable<object[]> Malformed()
        {
            yield return new object[] { "empty", "" };
            yield return new object[] { "unclosed-tag", "<div d-if=\"{{a}}\"" };
            yield return new object[] { "unbalanced-quote", "<div d-if=\"{{a}}><p d-for='x in {{y}}\">" };
            yield return new object[] { "truncated-call", "<button d-on-click=\"UpdateSector(" };
            yield return new object[] { "truncated-call-2", "<button d-on-click=\"UpdateSector('a', Foo(" };
            yield return new object[] { "lonely-lt", "<" };
            yield return new object[] { "lonely-d", "<div d-" };
            yield return new object[] { "only-mustache", "{{" };
            yield return new object[] { "nul-noise", "<div d-model=\"\0\0{{x}}\0\">\0</div>" };
            yield return new object[] { "unicode", "<div d-model=\"{{名前}}\" d-on-click=\"Notify(名前)\">émoji 🎉</div>" };
            yield return new object[] { "big", Generate(3000) };
        }

        private static string Generate(int lines)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < lines; i++)
                sb.Append("<div d-for=\"item in {{items}}\" d-on-click=\"UpdateSector({{item.a}}, '~/u')\" d-bad=\"").Append(i).Append("\">{{item.name}}</div>\n");
            return sb.ToString();
        }

        [Theory]
        [MemberData(nameof(Malformed))]
        public async Task NothingThrows(string name, string text)
        {
            Assert.NotNull(name);
            DrapoValidationResultVM result = await TestContentRoot.Validator().Validate(text);
            var mapped = DiagnosticMapper.Map(result.Diagnostics, text);
            Assert.Equal(result.Diagnostics.Count, mapped.Count);

            // Probe every position (bounded for the big document) with every provider.
            int step = text.Length > 500 ? text.Length / 200 + 1 : 1;
            for (int offset = 0; offset <= text.Length; offset += step)
            {
                var pos = PositionAt(text, offset);
                ServerFixture.Completion.GetCompletions(text, pos);
                ServerFixture.Hover.GetHover(text, pos);
                ServerFixture.SignatureHelp.GetSignatureHelp(text, pos);
            }
            // Out-of-range positions must be tolerated too.
            var far = new Position(100000, 100000);
            ServerFixture.Completion.GetCompletions(text, far);
            ServerFixture.Hover.GetHover(text, far);
            ServerFixture.SignatureHelp.GetSignatureHelp(text, far);
        }

        [Fact]
        public async Task LargeDocumentValidatesQuickly()
        {
            string text = Generate(3000);
            await TestContentRoot.Validator().Validate(text); // warm-up (engine parse, doc cache)
            var sw = Stopwatch.StartNew();
            DrapoValidationResultVM result = await TestContentRoot.Validator().Validate(text);
            DiagnosticMapper.Map(result.Diagnostics, text);
            sw.Stop();
            Assert.Equal(3000, result.ErrorCount);
            Assert.True(sw.ElapsedMilliseconds < 500, $"took {sw.ElapsedMilliseconds} ms");
        }

        private static Position PositionAt(string text, int offset)
        {
            int line = 0, ch = 0;
            for (int i = 0; i < offset && i < text.Length; i++)
            {
                if (text[i] == '\n') { line++; ch = 0; } else ch++;
            }
            return new Position(line, ch);
        }
    }
}
