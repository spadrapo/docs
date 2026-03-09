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
    /// Service for retrieving Drapo concept documentation metadata from the Concepts folder.
    /// Provides methods to list all concepts and get details for a specific concept.
    /// </summary>
    public class ConceptService : IConceptService
    {
        private readonly IWebHostEnvironment _env;
        private const string ParagraphTagStart = "<p>";

        /// <summary>
        /// Initializes a new instance of the <see cref="ConceptService"/> class.
        /// </summary>
        /// <param name="env">The web hosting environment.</param>
        public ConceptService(IWebHostEnvironment env)
        {
            _env = env;
        }

        /// <summary>
        /// Removes the leading number and dash from the file name (e.g., "0001 - Architecture.html" → "Architecture.html").
        /// </summary>
        /// <param name="fileName">The file name to process.</param>
        /// <returns>The file name without the numeric prefix.</returns>
        private string RemoveNumberPrefix(string fileName)
        {
            return Regex.Replace(fileName, @"^\d+\s*-\s*", "");
        }

        /// <summary>
        /// Internal method to retrieve concept view models from the Concepts folder.
        /// </summary>
        /// <param name="withDetails">Whether to include the full file content as details.</param>
        /// <param name="matchName">If provided, only returns the concept with this name (after prefix removal).</param>
        /// <returns>List of <see cref="ConceptVM"/> objects.</returns>
        private async Task<List<ConceptVM>> GetConceptsInternal(bool withDetails, string matchName = null)
        {
            string conceptsFolder = Directory.GetDirectories(Path.Combine(_env.WebRootPath, "app", "menu"))
                .FirstOrDefault(d => d.EndsWith("Concepts", StringComparison.OrdinalIgnoreCase));
            if (conceptsFolder == null)
                return new List<ConceptVM>();

            var files = Directory.GetFiles(conceptsFolder, "*.html");
            var list = new List<ConceptVM>();
            foreach (var file in files)
            {
                string name = Path.GetFileName(file);
                string displayName = RemoveNumberPrefix(name);
                if (displayName.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                    displayName = Path.GetFileNameWithoutExtension(displayName);
                if (matchName != null && !string.Equals(displayName, matchName, StringComparison.OrdinalIgnoreCase))
                    continue;
                string[] lines = await File.ReadAllLinesAsync(file);
                string description = null;
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith(ParagraphTagStart, StringComparison.OrdinalIgnoreCase))
                    {
                        int start = trimmed.IndexOf(ParagraphTagStart) + ParagraphTagStart.Length;
                        int end = trimmed.IndexOf("</p>", start, StringComparison.OrdinalIgnoreCase);
                        if (end > start)
                            description = trimmed.Substring(start, end - start).Trim();
                        else
                            description = trimmed.Substring(start).Trim();
                        break;
                    }
                }
                list.Add(new ConceptVM
                {
                    Name = displayName,
                    Description = description,
                    Details = withDetails ? string.Join("\n", lines) : null
                });
            }
            return list;
        }

        /// <summary>
        /// Gets a list of all Drapo concepts (name and description only).
        /// </summary>
        /// <returns>List of <see cref="ConceptVM"/> objects with basic info.</returns>
        public async Task<List<ConceptVM>> GetList()
        {
            return await GetConceptsInternal(false);
        }

        /// <summary>
        /// Gets the details for a specific Drapo concept (name, description, and full content).
        /// </summary>
        /// <param name="name">The concept name (without numeric prefix).</param>
        /// <returns>The <see cref="ConceptVM"/> for the concept, or null if not found.</returns>
        public async Task<ConceptVM> Get(string name)
        {
            var result = await GetConceptsInternal(true, name);
            return result.FirstOrDefault();
        }
    }
}
