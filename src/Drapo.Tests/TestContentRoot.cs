using System;
using System.IO;
using Drapo.Tooling.Content;
using Drapo.Tooling.Services;

namespace Drapo.Tests
{
    /// <summary>
    /// Points the tooling services at the repository's real documentation content
    /// (<c>src/WebDocs/wwwroot/app</c>) so tests exercise the same files the website serves.
    /// </summary>
    public static class TestContentRoot
    {
        private static readonly Lazy<string> RepoSrc = new Lazy<string>(FindSrc);

        public static string SrcPath => RepoSrc.Value;

        public static string AppPath => Path.Combine(SrcPath, "WebDocs", "wwwroot", "app");

        public static IDrapoContentRoot Create() => new DrapoContentRoot(AppPath);

        public static DrapoEngineCatalog Catalog { get; } = new DrapoEngineCatalog();

        public static FunctionService Functions() => new FunctionService(Create());

        public static AttributeService Attributes() => new AttributeService(Create());

        public static DrapoValidatorService Validator() => new DrapoValidatorService(Catalog, Functions());

        private static string FindSrc()
        {
            string dir = AppContext.BaseDirectory;
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir, "docs.sln")))
                    return dir;
                dir = Path.GetDirectoryName(dir);
            }
            throw new InvalidOperationException("Could not locate src/docs.sln above " + AppContext.BaseDirectory);
        }
    }
}
