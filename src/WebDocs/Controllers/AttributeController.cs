using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebDocs.Models;
using WebDocs.Services;

namespace WebDocs.Controllers
{
    /// <summary>
    /// API controller for Drapo attributes.
    /// Provides endpoints to list attributes and get attribute details.
    /// </summary>
    [Route("api/[controller]/[action]")]
    public class AttributeController : ControllerBase
    {
        private readonly IAttributeService _attributeService;
        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeController"/> class.
        /// </summary>
        /// <param name="attributeService">The attribute service to use.</param>
        public AttributeController(IAttributeService attributeService)
        {
            _attributeService = attributeService;
        }

        /// <summary>
        /// Gets the list of all Drapo attributes (name and description only).
        /// </summary>
        /// <returns>List of <see cref="AttributeVM"/> objects.</returns>
        [HttpGet]
        public async Task<ActionResult<List<AttributeVM>>> GetAttributes()
        {
            var list = await _attributeService.GetList();
            return Ok(list);
        }

        /// <summary>
        /// Gets the details for a specific Drapo attribute.
        /// </summary>
        /// <param name="name">The attribute file name (without numeric prefix).</param>
        /// <returns>The <see cref="AttributeVM"/> for the attribute, or NotFound if not found.</returns>
        [HttpGet("{name}")]
        public async Task<ActionResult<AttributeVM>> GetAttribute(string name)
        {
            var attribute = await _attributeService.Get(name);
            if (attribute == null)
                return NotFound();
            return Ok(attribute);
        }
    }
}
