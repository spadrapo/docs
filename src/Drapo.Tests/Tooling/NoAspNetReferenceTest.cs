using System;
using System.Linq;
using Drapo.Tooling.Services;
using Xunit;

namespace Drapo.Tests.Tooling
{
    public class NoAspNetReferenceTest
    {
        [Fact]
        public void ToolingAssemblyDoesNotReferenceAspNetCore()
        {
            var referenced = typeof(DrapoValidatorService).Assembly.GetReferencedAssemblies()
                .Select(a => a.Name)
                .Where(n => n.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase))
                .ToList();
            Assert.Empty(referenced);
        }
    }
}
