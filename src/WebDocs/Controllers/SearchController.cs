using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WebDocs.Models;
using WebDocs.Controllers;

namespace WebDocs.Controllers
{
    [Route("api/[controller]/[action]")]
    public class SearchController : Controller
    {
        private readonly MenuController _menuController;
        
        public SearchController(MenuController menuController)
        {
            _menuController = menuController;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<SearchItemVM>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Exception), (int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult> Search([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
                {
                    return Ok(new List<SearchItemVM>());
                }

                var searchResults = new List<SearchItemVM>();
                var menuItems = await _menuController.GetItemsInternal();
                
                // Search through all menu items recursively
                searchResults.AddRange(SearchMenuItems(menuItems, query.ToLower()));
                
                // Sort results by relevance (exact matches first, then partial matches)
                var sortedResults = searchResults
                    .OrderBy(r => !r.Name.ToLower().StartsWith(query.ToLower()))
                    .ThenBy(r => r.Name.ToLower())
                    .Take(10) // Limit results to prevent UI overwhelm
                    .ToList();

                return Ok(sortedResults);
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, e);
            }
        }

        private List<SearchItemVM> SearchMenuItems(List<MenuItemVM> items, string query)
        {
            var results = new List<SearchItemVM>();
            
            foreach (var item in items)
            {
                // Check if current item matches search query
                if (item.Name.ToLower().Contains(query))
                {
                    var searchItem = new SearchItemVM
                    {
                        Name = item.Name,
                        Type = item.Action.Contains("/function/") ? "Function" : "Document",
                        Action = item.Action,
                        Description = item.Action.Contains("/function/") ? "Drapo Function" : "Documentation"
                    };
                    results.Add(searchItem);
                }
                
                // Search recursively in child items
                if (item.Items != null && item.Items.Count > 0)
                {
                    results.AddRange(SearchMenuItems(item.Items, query));
                }
            }
            
            return results;
        }
    }
}