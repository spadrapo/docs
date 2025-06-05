using System.Collections.Generic;

namespace WebDocs.Models
{
    /// <summary>
    /// ViewModel representing a Drapo function, including its name, description, parameters, and samples.
    /// </summary>
    public class FunctionVM
    {
        /// <summary>
        /// The name of the Drapo function.
        /// </summary>
        public string Name { set; get; }
        /// <summary>
        /// The description of the Drapo function.
        /// </summary>
        public string Description { set; get; }
        /// <summary>
        /// The list of parameters for the Drapo function.
        /// </summary>
        public List<FunctionParameterVM> Parameters { get; set; }
        /// <summary>
        /// The list of samples demonstrating the Drapo function.
        /// </summary>
        public List<FunctionSampleVM> Samples { get; set; }
    }
}