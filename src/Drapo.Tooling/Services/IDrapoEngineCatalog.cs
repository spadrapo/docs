using System.Collections.Generic;

namespace Drapo.Tooling.Services
{
    /// <summary>
    /// The authoritative set of functions and attributes supported by the Drapo engine that is
    /// bundled inside this docs app (read from the embedded drapo.js). Used to validate templates
    /// without depending on the (possibly incomplete) documentation catalog.
    /// </summary>
    public interface IDrapoEngineCatalog
    {
        /// <summary>True if <paramref name="name"/> is a function dispatched by the engine (case-insensitive).</summary>
        bool IsValidFunction(string name);

        /// <summary>
        /// True if <paramref name="name"/> is a supported attribute (case-insensitive): an exact
        /// engine attribute, or a member of a dynamic prefix family such as d-on-{event}.
        /// </summary>
        bool IsValidAttribute(string name);

        /// <summary>Version identifier of the bundled engine.</summary>
        string EngineVersion { get; }

        /// <summary>All function names dispatched by the engine, lower-case.</summary>
        IReadOnlyCollection<string> Functions { get; }

        /// <summary>
        /// Fixed attribute names known to the engine, lower-case (prefix families excluded; see
        /// <see cref="AttributePrefixes"/>).
        /// </summary>
        IReadOnlyCollection<string> Attributes { get; }

        /// <summary>
        /// Dynamic attribute prefixes, lower-case, each ending with '-' (e.g. d-on-, d-attr-,
        /// d-validation-, d-dataproperty-). Any name that extends a prefix is a valid attribute.
        /// </summary>
        IReadOnlyCollection<string> AttributePrefixes { get; }
    }
}
