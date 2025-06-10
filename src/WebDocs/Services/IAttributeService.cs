using System.Collections.Generic;
using System.Threading.Tasks;
using WebDocs.Models;

namespace WebDocs.Services
{
    /// <summary>
    /// Interface for Drapo attribute service.
    /// Provides methods to list and get Drapo attributes.
    /// </summary>
    public interface IAttributeService
    {
        /// <summary>
        /// Gets a list of all Drapo attributes (name and first line only).
        /// </summary>
        /// <returns>List of <see cref="AttributeVM"/> objects with basic info.</returns>
        Task<List<AttributeVM>> GetList();
        /// <summary>
        /// Gets the details for a specific Drapo attribute (name, first line, and full content).
        /// </summary>
        /// <param name="name">The attribute file name (without numeric prefix).</param>
        /// <returns>The <see cref="AttributeVM"/> for the attribute, or null if not found.</returns>
        Task<AttributeVM> Get(string name);
    }
}
