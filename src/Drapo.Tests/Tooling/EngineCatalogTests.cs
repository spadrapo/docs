using System.Linq;
using Xunit;

namespace Drapo.Tests.Tooling
{
    public class EngineCatalogTests
    {
        private readonly Drapo.Tooling.Services.DrapoEngineCatalog _catalog = TestContentRoot.Catalog;

        [Fact]
        public void EnumerationsAgreeWithValidityChecks()
        {
            Assert.NotEmpty(_catalog.Attributes);
            Assert.NotEmpty(_catalog.AttributePrefixes);
            Assert.NotEmpty(_catalog.Functions);
            Assert.All(_catalog.Attributes, a => Assert.True(_catalog.IsValidAttribute(a), a));
            Assert.All(_catalog.AttributePrefixes, p => Assert.True(_catalog.IsValidAttribute(p + "x"), p));
            Assert.All(_catalog.AttributePrefixes, p => Assert.EndsWith("-", p));
            Assert.All(_catalog.Attributes, a => Assert.False(a.EndsWith("-"), a));
            Assert.All(_catalog.Functions, f => Assert.True(_catalog.IsValidFunction(f), f));
        }

        [Theory]
        [InlineData("d-on-click")]
        [InlineData("D-For")]
        [InlineData("d-validation-id")]
        [InlineData("d-dataproperty-x-name")]
        [InlineData("d-model")]
        public void KnownAttributesAreValid(string name) => Assert.True(_catalog.IsValidAttribute(name));

        [Theory]
        [InlineData("d-nope")]
        [InlineData("")]
        [InlineData(null)]
        public void UnknownAttributesAreInvalid(string name) => Assert.False(_catalog.IsValidAttribute(name));

        [Fact]
        public void FunctionsContainUpdateSector()
        {
            Assert.Contains("updatesector", _catalog.Functions);
            Assert.True(_catalog.IsValidFunction("UpdateSector"));
            Assert.False(_catalog.IsValidFunction("NoSuchFunction"));
        }

        [Fact]
        public void EngineVersionIsReported() => Assert.False(string.IsNullOrWhiteSpace(_catalog.EngineVersion));
    }
}
