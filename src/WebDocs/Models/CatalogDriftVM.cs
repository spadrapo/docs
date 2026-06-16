using System.Collections.Generic;

namespace WebDocs.Models
{
    /// <summary>
    /// Drift report for a single catalog category (functions or attributes): how the
    /// documented set compares to the set extracted from the bundled Drapo engine.
    /// </summary>
    public class CatalogCategoryDriftVM
    {
        /// <summary>Distinct names found in the bundled engine.</summary>
        public int EngineCount { get; set; }
        /// <summary>Distinct names found in the documentation.</summary>
        public int DocumentedCount { get; set; }
        /// <summary>In the engine but not documented (after the allow-list is applied).</summary>
        public List<string> Undocumented { get; set; } = new List<string>();
        /// <summary>Documented but not present in the engine (after the allow-list is applied).</summary>
        public List<string> Stale { get; set; } = new List<string>();
        /// <summary>Names that differed but were suppressed by the allow-list.</summary>
        public List<string> AllowListed { get; set; } = new List<string>();
        /// <summary>True when there is unsuppressed drift in this category.</summary>
        public bool HasDrift => (Undocumented.Count > 0) || (Stale.Count > 0);
    }

    /// <summary>
    /// Result of verify_catalog: compares the documentation catalog against the Drapo
    /// engine bundled inside the docs app, so undocumented/stale entries surface to LLMs.
    /// </summary>
    public class CatalogDriftVM
    {
        /// <summary>Version of the bundled Sysphera.Middleware.Drapo engine that was inspected.</summary>
        public string EngineVersion { get; set; }
        /// <summary>Function drift. Authoritative: engine functions are dispatched by an exact name.</summary>
        public CatalogCategoryDriftVM Functions { get; set; }
        /// <summary>Attribute drift. Heuristic (see <see cref="AttributesAreHeuristic"/>).</summary>
        public CatalogCategoryDriftVM Attributes { get; set; }
        /// <summary>
        /// Attributes are extracted from engine string literals and include many internal /
        /// dynamically-prefixed attributes, so attribute drift is best-effort and curated via
        /// the allow-list. Function drift is exact.
        /// </summary>
        public bool AttributesAreHeuristic { get; set; } = true;
        /// <summary>True when the authoritative (function) drift is non-empty.</summary>
        public bool HasDrift { get; set; }
    }
}
