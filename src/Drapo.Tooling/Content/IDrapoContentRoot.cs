namespace Drapo.Tooling.Content
{
    /// <summary>
    /// Locates the convention-bound documentation content: the folder that contains
    /// <c>functions/&lt;Name&gt;/</c> and <c>menu/NNNN - &lt;Section&gt;/</c> (see the constitution,
    /// Principle II). In the website this is <c>wwwroot/app</c>; in the language server it is the
    /// copy shipped next to the binary; in tests it is the repository's <c>wwwroot/app</c>.
    /// </summary>
    public interface IDrapoContentRoot
    {
        /// <summary>Absolute path of the <c>app</c> content folder.</summary>
        string AppPath { get; }
    }
}
