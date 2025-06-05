using ModelContextProtocol.Server;
using System.ComponentModel;

namespace WebDocs.Tools
{
    [McpServerToolType]
    public class FunctionTool
    {
        [McpServerTool(Name = "get_functions"), Description("Get the list of all drapo functions. These functions does not have too much detail.")]
        public string[] GetFunctions()
        {
            return new[] { "Function1", "Function2", "Function3" };
        }

        [McpServerTool(Name = "get_function"), Description("Get details about a drapo function with samples of how to use it")]
        public string GetFunction(string name)
        {
            return $"Function: {name}";
        }
    }
}
