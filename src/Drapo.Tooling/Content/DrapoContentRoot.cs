using System;

namespace Drapo.Tooling.Content
{
    /// <summary>Simple path-based <see cref="IDrapoContentRoot"/>.</summary>
    public sealed class DrapoContentRoot : IDrapoContentRoot
    {
        public DrapoContentRoot(string appPath)
        {
            if (string.IsNullOrWhiteSpace(appPath))
                throw new ArgumentException("The content root path must not be empty.", nameof(appPath));
            AppPath = appPath;
        }

        public string AppPath { get; }
    }
}
