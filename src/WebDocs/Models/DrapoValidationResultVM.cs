using System.Collections.Generic;

namespace WebDocs.Models
{
    /// <summary>
    /// Result of validate_drapo: whether the template is valid and the diagnostics found.
    /// </summary>
    public class DrapoValidationResultVM
    {
        /// <summary>True when there are no error-level diagnostics (warnings may still be present).</summary>
        public bool Valid { get; set; }
        /// <summary>Number of error-level diagnostics.</summary>
        public int ErrorCount { get; set; }
        /// <summary>Number of warning-level diagnostics.</summary>
        public int WarningCount { get; set; }
        /// <summary>Version of the bundled engine the template was validated against.</summary>
        public string EngineVersion { get; set; }
        /// <summary>The diagnostics, in document order.</summary>
        public List<DrapoDiagnosticVM> Diagnostics { get; set; } = new List<DrapoDiagnosticVM>();
    }
}
