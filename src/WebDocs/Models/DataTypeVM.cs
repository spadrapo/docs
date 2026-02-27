namespace WebDocs.Models
{
    /// <summary>
    /// View model representing a Drapo data type, including its name, description, and details.
    /// </summary>
    public class DataTypeVM
    {
        /// <summary>
        /// Gets or sets the data type name (file name without numeric prefix and extension).
        /// </summary>
        public string Name { set; get; }
        /// <summary>
        /// Gets or sets the data type description (extracted from first paragraph).
        /// </summary>
        public string Description { set; get; }
        /// <summary>
        /// Gets or sets the data type details (full file content, if requested).
        /// </summary>
        public string Details { set; get; }
    }
}
