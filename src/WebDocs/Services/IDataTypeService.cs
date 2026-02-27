using System.Collections.Generic;
using System.Threading.Tasks;
using WebDocs.Models;

namespace WebDocs.Services
{
    /// <summary>
    /// Service interface for retrieving Drapo data type metadata.
    /// </summary>
    public interface IDataTypeService
    {
        /// <summary>
        /// Gets a list of all Drapo data types (name and description only).
        /// </summary>
        /// <returns>List of <see cref="DataTypeVM"/> objects with basic info.</returns>
        Task<List<DataTypeVM>> GetList();
        /// <summary>
        /// Gets the details for a specific Drapo data type (name, description, and full content).
        /// </summary>
        /// <param name="name">The data type name (without numeric prefix).</param>
        /// <returns>The <see cref="DataTypeVM"/> for the data type, or null if not found.</returns>
        Task<DataTypeVM> Get(string name);
    }
}
