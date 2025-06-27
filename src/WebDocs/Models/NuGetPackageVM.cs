using System;
using System.Collections.Generic;

namespace WebDocs.Models
{
    /// <summary>
    /// View model for NuGet package information
    /// </summary>
    public class NuGetPackageVM
    {
        public string Id { get; set; }
        public string LatestVersion { get; set; }
        public string Description { get; set; }
        public long TotalDownloads { get; set; }
        public string ProjectUrl { get; set; }
        public List<string> Authors { get; set; }
        public List<string> Tags { get; set; }
        public DateTime? Published { get; set; }
        public bool Verified { get; set; }
        
        public NuGetPackageVM()
        {
            Authors = new List<string>();
            Tags = new List<string>();
        }
    }
}