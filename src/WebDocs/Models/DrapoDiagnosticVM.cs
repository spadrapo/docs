namespace WebDocs.Models
{
    /// <summary>
    /// A single problem found by validate_drapo in a Drapo template.
    /// </summary>
    public class DrapoDiagnosticVM
    {
        /// <summary>Severity: "error" (definitely wrong) or "warning" (likely wrong).</summary>
        public string Level { get; set; }
        /// <summary>Stable rule id, e.g. unknown-attribute, unknown-function, wrong-arity, malformed-dfor, unbalanced-mustache.</summary>
        public string Rule { get; set; }
        /// <summary>Human-readable explanation.</summary>
        public string Message { get; set; }
        /// <summary>1-based line of the offending token.</summary>
        public int Line { get; set; }
        /// <summary>1-based column of the offending token.</summary>
        public int Column { get; set; }
    }
}
