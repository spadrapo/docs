using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WebDocs.Models;

namespace WebDocs.Controllers
{
    [Route("api/[controller]/[action]")]
    public class MenuController : Controller
    {
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<MenuItemVM>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Exception), (int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult> GetItems()
        {
            try
            {
                List<MenuItemVM> items = new List<MenuItemVM>();
                items.Add(this.CreateMenuItemUrl("Installation", "~/app/installation/installation.html"));
                items.Add(this.CreateMenuItemUrl("Introduction", "~/app/introduction/introduction.html"));
                return (Ok(await Task.FromResult<List<MenuItemVM>>(items)));
            }
            catch (Exception e)
            {
                return (StatusCode((int)HttpStatusCode.InternalServerError, e));
            }
        }

        private MenuItemVM CreateMenuItemUrl(string name, string url)
        {
            MenuItemVM menuItem = new MenuItemVM();
            menuItem.Name = name;
            menuItem.Action = string.Format("UpdateSector(content,{0},false,false)", url);
            return (menuItem);
        }
    }
}