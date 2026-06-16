using System.Threading.Tasks;
using WebDocs.Models;

namespace WebDocs.Services
{
    /// <summary>
    /// Compares the documentation catalog (functions/attributes) against the Drapo engine
    /// bundled inside the docs application, reporting undocumented and stale entries.
    /// </summary>
    public interface ICatalogDriftService
    {
        /// <summary>
        /// Builds the drift report by extracting the function/attribute sets from the bundled
        /// engine JS and diffing them against the documented sets, applying the allow-list.
        /// </summary>
        Task<CatalogDriftVM> GetDrift();
    }
}
