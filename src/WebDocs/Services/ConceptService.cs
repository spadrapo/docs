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
        /// Recursively collects all .html files under a directory, sorted by file/folder name at each level.
        /// </summary>
        private List<string> GetHtmlFilesSorted(string directory)
        {
            var result = new List<string>();
            foreach (var file in Directory.GetFiles(directory, "*.html").OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase))
                result.Add(file);
            foreach (var sub in Directory.GetDirectories(directory).OrderBy(d => Path.GetFileName(d), StringComparer.OrdinalIgnoreCase))
                result.AddRange(GetHtmlFilesSorted(sub));
            return result;
        }

        /// <summary>
        /// Extracts the first paragraph text from a set of file lines.
        /// </summary>
        private string ExtractFirstParagraph(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith(ParagraphTagStart, StringComparison.OrdinalIgnoreCase))
                {
                    int start = trimmed.IndexOf(ParagraphTagStart) + ParagraphTagStart.Length;
                    int end = trimmed.IndexOf("</p>", start, StringComparison.OrdinalIgnoreCase);
                    return end > start
                        ? trimmed.Substring(start, end - start).Trim()
                        : trimmed.Substring(start).Trim();
                }
            }
            return null;
        }

        /// <summary>
        /// Internal method to retrieve concept view models from the Concepts folder.
        /// Supports both single .html files and sub-folders (multi-page concepts).
        /// </summary>
        /// <param name="withDetails">Whether to include the full file content as details.</param>
        /// <param name="matchName">If provided, only returns the concept with this name (after prefix removal).</param>
        /// <returns>List of <see cref="ConceptVM"/> objects.</returns>
        private async Task<List<ConceptVM>> GetConceptsInternal(bool withDetails, string matchName = null)
        {
            string conceptsFolder = Directory.GetDirectories(Path.Combine(_env.WebRootPath, "app", "menu"))
                .FirstOrDefault(d => d.EndsWith("Guide", StringComparison.OrdinalIgnoreCase));
            if (conceptsFolder == null)
                return new List<ConceptVM>();

            // Collect both .html files and sub-directories, sorted together by their entry name
            var entries = Directory.GetFiles(conceptsFolder, "*.html")
                .Select(f => (sortKey: Path.GetFileName(f), path: f, isDir: false))
                .Concat(Directory.GetDirectories(conceptsFolder)
                    .Select(d => (sortKey: Path.GetFileName(d), path: d, isDir: true)))
                .OrderBy(e => e.sortKey, StringComparer.OrdinalIgnoreCase);

            var list = new List<ConceptVM>();
            foreach (var (_, path, isDir) in entries)
            {
                string entryName = isDir ? Path.GetFileName(path) : Path.GetFileNameWithoutExtension(path);
                string displayName = RemoveNumberPrefix(entryName);

                if (matchName != null && !string.Equals(displayName, matchName, StringComparison.OrdinalIgnoreCase))
                    continue;

                string description = null;
                string details = null;

                if (isDir)
                {
                    var htmlFiles = GetHtmlFilesSorted(path);
                    if (htmlFiles.Count > 0)
                    {
                        description = ExtractFirstParagraph(await File.ReadAllLinesAsync(htmlFiles[0]));
                        if (withDetails)
                        {
                            var sb = new System.Text.StringBuilder();
                            foreach (var htmlFile in htmlFiles)
                            {
                                sb.AppendLine(await File.ReadAllTextAsync(htmlFile));
                            }
                            details = sb.ToString();
                        }
                    }
                }
                else
                {
                    var lines = await File.ReadAllLinesAsync(path);
                    description = ExtractFirstParagraph(lines);
                    if (withDetails)
                        details = string.Join("\n", lines);
                }

                list.Add(new ConceptVM { Name = displayName, Description = description, Details = details });
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
