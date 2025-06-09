using ModelContextProtocol.Server;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using WebDocs.Models;
using WebDocs.Services;

namespace WebDocs.Tools
{
    [McpServerToolType]
    public class FunctionTool
    {
        private readonly IFunctionService _functionService;
        public FunctionTool(IFunctionService functionService)
        {
            this._functionService = functionService;
        }
        [McpServerTool(Name = "get_functions"), Description("Get the list of all drapo functions. These functions does not have too much detail.")]
        public async Task<List<FunctionVM>> GetFunctions()
        {
            return (await this._functionService.GetList());
        }

        [McpServerTool(Name = "get_function_details"), Description("Get details about a drapo function with samples of how to use it and the parameters needed for the function")]
        public async Task<FunctionVM> GetFunctionDetails(string name)
        {
            return (await this._functionService.Get(name));
        }
    }
}
