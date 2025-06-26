using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebDocs.Models;

namespace WebDocs.Services
{
    /// <summary>
    /// Service for fetching NuGet package information from the NuGet API
    /// </summary>
    public class NuGetService : INuGetService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NuGetService> _logger;
        private const string NUGET_SEARCH_API = "https://azuresearch-usnc.nuget.org/query";

        public NuGetService(HttpClient httpClient, ILogger<NuGetService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Gets package information for the specified package ID
        /// </summary>
        /// <param name="packageId">The NuGet package ID</param>
        /// <returns>Package information or null if not found</returns>
        public async Task<NuGetPackageVM> GetPackageAsync(string packageId)
        {
            try
            {
                var url = $"{NUGET_SEARCH_API}?q=packageid:{packageId}";
                _logger.LogInformation($"Fetching NuGet package info for {packageId} from {url}");

                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"NuGet API returned {response.StatusCode} for package {packageId}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var searchResult = JsonConvert.DeserializeObject<JObject>(content);

                var data = searchResult["data"] as JArray;
                if (data == null || data.Count == 0)
                {
                    _logger.LogWarning($"No package found for {packageId}");
                    return null;
                }

                var packageInfo = data[0] as JObject;
                if (packageInfo == null)
                {
                    return null;
                }

                var package = new NuGetPackageVM
                {
                    Id = packageInfo["id"]?.ToString(),
                    LatestVersion = packageInfo["version"]?.ToString(),
                    Description = packageInfo["description"]?.ToString(),
                    TotalDownloads = packageInfo["totalDownloads"]?.ToObject<long>() ?? 0,
                    ProjectUrl = packageInfo["projectUrl"]?.ToString(),
                    Verified = packageInfo["verified"]?.ToObject<bool>() ?? false
                };

                // Extract authors
                var authors = packageInfo["authors"] as JArray;
                if (authors != null)
                {
                    foreach (var author in authors)
                    {
                        package.Authors.Add(author.ToString());
                    }
                }

                // Extract tags
                var tags = packageInfo["tags"] as JArray;
                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        package.Tags.Add(tag.ToString());
                    }
                }

                _logger.LogInformation($"Successfully fetched info for {packageId} - Version: {package.LatestVersion}, Downloads: {package.TotalDownloads}");
                return package;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching NuGet package info for {packageId}");
                return null;
            }
        }
    }
}