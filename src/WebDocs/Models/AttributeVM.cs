namespace WebDocs.Models
{
    /// <summary>
    /// View model representing a Drapo attribute, including its name, description, and details.
    /// </summary>
    public class AttributeVM
    {
        /// <summary>
        /// Gets or sets the attribute name (file name without numeric prefix).
        /// </summary>
        public string Name { set; get; }
        /// <summary>
        /// Gets or sets the attribute description (first line of the file).
        /// </summary>
        public string Description { set; get; }
        /// <summary>
        /// Gets or sets the attribute details (full file content, if requested).
        /// </summary>
        public string Details { set; get; }
    }
}
