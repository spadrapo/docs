namespace WebDocs.Models
{
    /// <summary>
    /// ViewModel representing a sample for a Drapo function, including its name, description, and content.
    /// </summary>
    public class FunctionSampleVM
    {
        /// <summary>
        /// The name of the sample.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The description of the sample.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// The content (HTML/code) of the sample.
        /// </summary>
        public string Content { get; set; }
    }
}
