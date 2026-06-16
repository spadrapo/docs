using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Threading.Tasks;
using WebDocs.Models;
using WebDocs.Services;

namespace WebDocs.Tools
{
    /// <summary>
    /// Tool exposing Drapo template validation to the MCP server.
    /// </summary>
    [McpServerToolType]
    public class ValidationTool
    {
        private readonly IDrapoValidatorService _validatorService;

        public ValidationTool(IDrapoValidatorService validatorService)
        {
            this._validatorService = validatorService;
        }

        /// <summary>
        /// Validates a Drapo template and returns diagnostics.
        /// </summary>
        [McpServerTool(Name = "validate_drapo"), Description("Validate a Drapo template (HTML snippet or full file content). Pass the markup as 'html' and get back diagnostics for: unknown d-* attributes, unknown functions used in d-on-* handlers, functions called with too few arguments, malformed d-for syntax, and unbalanced {{ }} mustaches. Attribute/function existence is checked against the bundled Drapo engine; argument counts against the documented signatures. Use after writing or editing Drapo markup to confirm it is correct.")]
        public async Task<DrapoValidationResultVM> ValidateDrapo(string html)
        {
            return (await this._validatorService.Validate(html));
        }
    }
}
