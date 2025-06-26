using System.Threading.Tasks;
using WebDocs.Models;

namespace WebDocs.Services
{
    /// <summary>
    /// Interface for fetching NuGet package information
    /// </summary>
    public interface INuGetService
    {
        /// <summary>
        /// Gets package information for the specified package ID
        /// </summary>
        /// <param name="packageId">The NuGet package ID</param>
        /// <returns>Package information or null if not found</returns>
        Task<NuGetPackageVM> GetPackageAsync(string packageId);
    }
}