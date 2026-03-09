using ModelContextProtocol.Server;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using WebDocs.Models;
using WebDocs.Services;

namespace WebDocs.Tools
{
    /// <summary>
    /// Tool for exposing Drapo concept documentation to the MCP server.
    /// </summary>
    [McpServerToolType]
    public class ConceptTool
    {
        private readonly IConceptService _conceptService;
        /// <summary>
        /// Initializes a new instance of the <see cref="ConceptTool"/> class.
        /// </summary>
        /// <param name="conceptService">The concept service to use.</param>
        public ConceptTool(IConceptService conceptService)
        {
            this._conceptService = conceptService;
        }
        /// <summary>
        /// Gets the list of all Drapo concepts (basic info only).
        /// </summary>
        /// <returns>List of <see cref="ConceptVM"/> objects.</returns>
        [McpServerTool(Name = "get_concepts"), Description("Get the list of all Drapo concepts. These concepts explain how Drapo works including architecture, data binding, storage, sectors, pipes, configuration, expressions, components, routing, and events.")]
        public async Task<List<ConceptVM>> GetConcepts()
        {
            return (await this._conceptService.GetList());
        }

        /// <summary>
        /// Gets the full details for a specific Drapo concept, including explanations and code samples.
        /// </summary>
        /// <param name="name">The concept name (without numeric prefix).</param>
        /// <returns>The <see cref="ConceptVM"/> for the concept, or null if not found.</returns>
        [McpServerTool(Name = "get_concept_details"), Description("Get full details about a specific Drapo concept including explanations and code samples. Use get_concepts first to get the list of available concept names.")]
        public async Task<ConceptVM> GetConceptDetails(string name)
        {
            return (await this._conceptService.Get(name));
        }
    }
}
