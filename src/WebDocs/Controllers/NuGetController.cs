using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WebDocs.Models;
using WebDocs.Services;

namespace WebDocs.Controllers
{
    /// <summary>
    /// Controller for NuGet package information API endpoints
    /// </summary>
    [Route("api/[controller]/[action]")]
    public class NuGetController : Controller
    {
        private readonly INuGetService _nuGetService;

        public NuGetController(INuGetService nuGetService)
        {
            _nuGetService = nuGetService;
        }

        /// <summary>
        /// Gets package information for the specified package ID
        /// </summary>
        /// <param name="packageId">The NuGet package ID (defaults to "Drapo")</param>
        /// <returns>Package information</returns>
        [HttpGet]
        public async Task<IActionResult> GetPackage(string packageId = "Drapo")
        {
            var package = await _nuGetService.GetPackageAsync(packageId);
            
            if (package == null)
            {
                return NotFound($"Package '{packageId}' not found");
            }

            return Ok(package);
        }
    }
}