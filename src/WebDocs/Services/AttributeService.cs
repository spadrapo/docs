using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebDocs.Models;

namespace WebDocs.Services
{
    /// <summary>
    /// Service for retrieving Drapo attribute metadata from the Attributes folder.
    /// Provides methods to list all attributes and get details for a specific attribute.
    /// </summary>
    public class AttributeService : IAttributeService
    {
        private readonly IWebHostEnvironment _env;
        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeService"/> class.
        /// </summary>
        /// <param name="env">The web hosting environment.</param>
        public AttributeService(IWebHostEnvironment env)
        {
            _env = env;
        }

        /// <summary>
        /// Removes the leading number and dash from the file name (e.g., "0010 - d-model.html" → "d-model.html").
        /// </summary>
        /// <param name="fileName">The file name to process.</param>
        /// <returns>The file name without the numeric prefix.</returns>
        private string RemoveNumberPrefix(string fileName)
        {
            return Regex.Replace(fileName, @"^\d+\s*-\s*", "");
        }

        /// <summary>
        /// Internal method to retrieve attribute view models from the Attributes folder.
        /// </summary>
        /// <param name="withDetails">Whether to include the full file content as details.</param>
        /// <param name="matchName">If provided, only returns the attribute with this name (after prefix removal).</param>
        /// <returns>List of <see cref="AttributeVM"/> objects.</returns>
        private async Task<List<AttributeVM>> GetAttributesInternal(bool withDetails, string matchName = null)
        {
            string attributesFolder = Directory.GetDirectories(Path.Combine(_env.WebRootPath, "app", "menu"))
                .FirstOrDefault(d => d.EndsWith("Attributes", StringComparison.OrdinalIgnoreCase));
            if (attributesFolder == null)
                return new List<AttributeVM>();
            var files = Directory.GetFiles(attributesFolder, "*.html");
            var list = new List<AttributeVM>();
            foreach (var file in files)
            {
                string name = Path.GetFileName(file);
                string displayName = RemoveNumberPrefix(name);
                if (displayName.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                    displayName = displayName.Substring(0, displayName.Length - 5);
                if (matchName != null && !string.Equals(displayName, matchName, StringComparison.OrdinalIgnoreCase))
                    continue;
                string[] lines = await File.ReadAllLinesAsync(file);
                string description = null;
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("<p>", StringComparison.OrdinalIgnoreCase))
                    {
                        int start = trimmed.IndexOf("<p>") + 3;
                        int end = trimmed.IndexOf("</p>", start, StringComparison.OrdinalIgnoreCase);
                        if (end > start)
                            description = trimmed.Substring(start, end - start).Trim();
                        else
                            description = trimmed.Substring(start).Trim();
                        break;
                    }
                }
                list.Add(new AttributeVM
                {
                    Name = displayName,
                    Description = description,
                    Details = withDetails ? string.Join("\n", lines) : null
                });
            }
            return list;
        }

        /// <summary>
        /// Gets a list of all Drapo attributes (name and first line only).
        /// </summary>
        /// <returns>List of <see cref="AttributeVM"/> objects with basic info.</returns>
        public async Task<List<AttributeVM>> GetList()
        {
            return await GetAttributesInternal(false);
        }

        /// <summary>
        /// Gets the details for a specific Drapo attribute (name, first line, and full content).
        /// </summary>
        /// <param name="name">The attribute file name (without numeric prefix).</param>
        /// <returns>The <see cref="AttributeVM"/> for the attribute, or null if not found.</returns>
        public async Task<AttributeVM> Get(string name)
        {
            var result = await GetAttributesInternal(true, name);
            return result.FirstOrDefault();
        }
    }
}
