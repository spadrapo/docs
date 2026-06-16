using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Threading.Tasks;
using WebDocs.Models;
using WebDocs.Services;

namespace WebDocs.Tools
{
    /// <summary>
    /// Tool exposing catalog-drift detection to the MCP server.
    /// </summary>
    [McpServerToolType]
    public class CatalogTool
    {
        private readonly ICatalogDriftService _catalogDriftService;

        public CatalogTool(ICatalogDriftService catalogDriftService)
        {
            this._catalogDriftService = catalogDriftService;
        }

        /// <summary>
        /// Reports drift between the documentation catalog and the bundled Drapo engine.
        /// </summary>
        [McpServerTool(Name = "verify_catalog"), Description("Verify the Drapo documentation catalog against the engine bundled in this docs app. Returns functions/attributes that exist in the engine but are not documented (Undocumented), and documented entries missing from the engine (Stale). Function drift is authoritative; attribute drift is heuristic (string-literal based, curated by an allow-list). Use this to confirm the catalog the other tools serve is complete before relying on it.")]
        public async Task<CatalogDriftVM> VerifyCatalog()
        {
            return (await this._catalogDriftService.GetDrift());
        }
    }
}
