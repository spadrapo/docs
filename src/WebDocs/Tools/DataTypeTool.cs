using ModelContextProtocol.Server;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using WebDocs.Models;
using WebDocs.Services;

namespace WebDocs.Tools
{
    /// <summary>
    /// Tool for exposing Drapo data type operations to the MCP server.
    /// </summary>
    [McpServerToolType]
    public class DataTypeTool
    {
        private readonly IDataTypeService _dataTypeService;
        /// <summary>
        /// Initializes a new instance of the <see cref="DataTypeTool"/> class.
        /// </summary>
        /// <param name="dataTypeService">The data type service to use.</param>
        public DataTypeTool(IDataTypeService dataTypeService)
        {
            this._dataTypeService = dataTypeService;
        }
        /// <summary>
        /// Gets the list of all Drapo data types (basic info only).
        /// </summary>
        /// <returns>List of <see cref="DataTypeVM"/> objects.</returns>
        [McpServerTool(Name = "get_data_types"), Description("Get the list of all drapo data types. These types are used to define the type of data storage items. These data types do not have too much detail.")]
        public async Task<List<DataTypeVM>> GetDataTypes()
        {
            return (await this._dataTypeService.GetList());
        }

        /// <summary>
        /// Gets details about a specific Drapo data type, including full content and usage samples.
        /// </summary>
        /// <param name="name">The data type name (without numeric prefix).</param>
        /// <returns>The <see cref="DataTypeVM"/> for the data type, or null if not found.</returns>
        [McpServerTool(Name = "get_data_type_details"), Description("Get details about a drapo data type with samples of how to use it. These types are used to define the type of data storage items like value, object, array, function, etc.")]
        public async Task<DataTypeVM> GetDataTypeDetails(string name)
        {
            return (await this._dataTypeService.Get(name));
        }
    }
}
