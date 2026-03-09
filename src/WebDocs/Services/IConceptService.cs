using System.Collections.Generic;
using System.Threading.Tasks;
using WebDocs.Models;

namespace WebDocs.Services
{
    /// <summary>
    /// Service interface for retrieving Drapo concept documentation metadata.
    /// </summary>
    public interface IConceptService
    {
        /// <summary>
        /// Gets a list of all Drapo concepts (name and description only).
        /// </summary>
        /// <returns>List of <see cref="ConceptVM"/> objects with basic info.</returns>
        Task<List<ConceptVM>> GetList();
        /// <summary>
        /// Gets the details for a specific Drapo concept (name, description, and full content).
        /// </summary>
        /// <param name="name">The concept name (without numeric prefix).</param>
        /// <returns>The <see cref="ConceptVM"/> for the concept, or null if not found.</returns>
        Task<ConceptVM> Get(string name);
    }
}
