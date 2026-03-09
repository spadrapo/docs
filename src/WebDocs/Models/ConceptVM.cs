namespace WebDocs.Models
{
    /// <summary>
    /// View model representing a Drapo concept, including its name, description, and details.
    /// </summary>
    public class ConceptVM
    {
        /// <summary>
        /// Gets or sets the concept name (file name without numeric prefix and extension).
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the concept description (extracted from first paragraph).
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Gets or sets the concept details (full file content, if requested).
        /// </summary>
        public string Details { get; set; }
    }
}
