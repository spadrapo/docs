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
    /// Service for retrieving Drapo data type metadata from the Data/Types folder.
    /// Provides methods to list all data types and get details for a specific data type.
    /// </summary>
    public class DataTypeService : IDataTypeService
    {
        private readonly IWebHostEnvironment _env;
        /// <summary>
        /// Initializes a new instance of the <see cref="DataTypeService"/> class.
        /// </summary>
        /// <param name="env">The web hosting environment.</param>
        public DataTypeService(IWebHostEnvironment env)
        {
            _env = env;
        }

        /// <summary>
        /// Removes the leading number and dash from the file name (e.g., "0001 - value.html" → "value.html").
        /// </summary>
        /// <param name="fileName">The file name to process.</param>
        /// <returns>The file name without the numeric prefix.</returns>
        private string RemoveNumberPrefix(string fileName)
        {
            return Regex.Replace(fileName, @"^\d+\s*-\s*", "");
        }

        /// <summary>
        /// Internal method to retrieve data type view models from the Data/Types folder.
        /// </summary>
        /// <param name="withDetails">Whether to include the full file content as details.</param>
        /// <param name="matchName">If provided, only returns the data type with this name (after prefix removal).</param>
        /// <returns>List of <see cref="DataTypeVM"/> objects.</returns>
        private async Task<List<DataTypeVM>> GetDataTypesInternal(bool withDetails, string matchName = null)
        {
            string dataTypesFolder = Directory.GetDirectories(Path.Combine(_env.WebRootPath, "app", "menu"))
                .FirstOrDefault(d => d.EndsWith("Data", StringComparison.OrdinalIgnoreCase));
            if (dataTypesFolder == null)
                return new List<DataTypeVM>();

            string typesFolder = Directory.GetDirectories(dataTypesFolder)
                .FirstOrDefault(d => d.EndsWith("Types", StringComparison.OrdinalIgnoreCase));
            if (typesFolder == null)
                return new List<DataTypeVM>();

            var files = Directory.GetFiles(typesFolder, "*.html");
            var list = new List<DataTypeVM>();
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
                list.Add(new DataTypeVM
                {
                    Name = displayName,
                    Description = description,
                    Details = withDetails ? string.Join("\n", lines) : null
                });
            }
            return list;
        }

        /// <summary>
        /// Gets a list of all Drapo data types (name and first line only).
        /// </summary>
        /// <returns>List of <see cref="DataTypeVM"/> objects with basic info.</returns>
        public async Task<List<DataTypeVM>> GetList()
        {
            return await GetDataTypesInternal(false);
        }

        /// <summary>
        /// Gets the details for a specific Drapo data type (name, first line, and full content).
        /// </summary>
        /// <param name="name">The data type name (without numeric prefix).</param>
        /// <returns>The <see cref="DataTypeVM"/> for the data type, or null if not found.</returns>
        public async Task<DataTypeVM> Get(string name)
        {
            var result = await GetDataTypesInternal(true, name);
            return result.FirstOrDefault();
        }
    }
}
