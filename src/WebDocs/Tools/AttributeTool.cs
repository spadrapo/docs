using ModelContextProtocol.Server;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using WebDocs.Models;
using WebDocs.Services;

namespace WebDocs.Tools
{
    /// <summary>
    /// Tool for exposing Drapo attribute operations to the MCP server.
    /// </summary>
    [McpServerToolType]
    public class AttributeTool
    {
        private readonly IAttributeService _attributeService;
        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeTool"/> class.
        /// </summary>
        /// <param name="attributeService">The attribute service to use.</param>
        public AttributeTool(IAttributeService attributeService)
        {
            this._attributeService = attributeService;
        }
        /// <summary>
        /// Gets the list of all Drapo attributes (basic info only).
        /// </summary>
        /// <returns>List of <see cref="AttributeVM"/> objects.</returns>
        [McpServerTool(Name = "get_attributes"), Description("Get the list of all drapo attributes. These attributes are used in elements in the DOM. These attributes do not have too much detail.")]
        public async Task<List<AttributeVM>> GetAttributes()
        {
            return (await this._attributeService.GetList());
        }

        /// <summary>
        /// Gets details about a specific Drapo attribute, including usage samples.
        /// </summary>
        /// <param name="name">The attribute file name (without numeric prefix).</param>
        /// <returns>The <see cref="AttributeVM"/> for the attribute, or null if not found.</returns>
        [McpServerTool(Name = "get_attribute_details"), Description("Get details about a drapo attribute with samples of how to use it. This attribute is used in elements in the DOM")]
        public async Task<AttributeVM> GetAttributeDetails(string name)
        {
            return (await this._attributeService.Get(name));
        }
    }
}
