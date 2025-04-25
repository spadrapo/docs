using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using WebDocs.Models;

namespace WebDocs.Controllers
{
    [Route("api/[controller]/[action]")]
    public class MenuController : Controller
    {
        IWebHostEnvironment _env;
        FunctionController _function;
        public MenuController(IWebHostEnvironment env, FunctionController function)
        {
            _env = env;
            _function = function;
        }
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<MenuItemVM>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Exception), (int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult> GetItems()
        {
            try
            {
                return (Ok(await Task.FromResult<List<MenuItemVM>>(await this.GetItemsInternal())));
            }
            catch (Exception e)
            {
                return (StatusCode((int)HttpStatusCode.InternalServerError, e));
            }
        }

        [HttpGet]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Exception), (int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult> GetUrl(string id)
        {
            try
            {
                List<MenuItemVM> items = await this.GetItemsInternal();
                MenuItemVM item = GetMenuItemByID(items, id);
                if (item != null)
                    return (Ok(item.Url));
                return (NotFound());
            }
            catch (Exception e)
            {
                return (StatusCode((int)HttpStatusCode.InternalServerError, e));
            }
        }

        private MenuItemVM GetMenuItemByID(List<MenuItemVM> items, string id) 
        {
            foreach (MenuItemVM item in items) 
            {
                if (item.ID == id)
                    return (item);
                MenuItemVM itemChild = GetMenuItemByID(item.Items, id);
                if (itemChild != null)
                    return (itemChild);
            }
            return (null);
        }

        public async Task<List<MenuItemVM>> GetItemsInternal()
        {
            string path = Path.Combine(_env.WebRootPath, "app", "menu");
            List<MenuItemVM> items = await this.GetItems(path);
            //Inject Functions
            MenuItemVM itemFunctions = FindByName(items, "Functions");
            itemFunctions.Items.Clear();
            List<string> functionNames = await this._function.GetNames();
            foreach (string functionName in functionNames)
                itemFunctions.Items.Add(CreateMenuItemFunction(functionName));
            return (items);
        }

        private async Task<List<MenuItemVM>> GetItems(string path)
        {
            List<MenuItemVM> items = new List<MenuItemVM>();
            IEnumerable<string> entries = Directory.EnumerateFileSystemEntries(path);
            List<string> entriesSorted = this.Sort(entries);
            for (int i = 0; i < entriesSorted.Count; i++)
            {
                string entry = entriesSorted[i];
                string pathEntry = Path.Combine(path, entry);
                MenuItemVM item = null;
                if (Directory.Exists(pathEntry))
                    item = await CreateMenuItemGroup(entry);
                else
                    item = this.CreateMenuItemLink(pathEntry);
                items.Add(item);
            }
            return (items);
        }

        private List<string> Sort(IEnumerable<string> entries)
        {
            return (new List<string>(entries.OrderBy(s => s)));
        }

        private async Task<MenuItemVM> CreateMenuItemGroup(string fullPath)
        {
            MenuItemVM menuItem = new MenuItemVM();
            menuItem.IsExpandable = true;
            menuItem.Name = this.GetFriendlyName(Path.GetFileName(fullPath));
            menuItem.Action = "ToggleItemField({{item.IsExpanded}});Notify(menuItems)";
            menuItem.Items.AddRange(await this.GetItems(fullPath));
            return (menuItem);
        }

        private MenuItemVM CreateMenuItemFunction(string functionName)
        {
            MenuItemVM menuItem = new MenuItemVM();
            menuItem.Name = functionName;
            menuItem.Action = $"ApplyRoute(/function/{functionName})";
            return (menuItem);
        }

        private MenuItemVM CreateMenuItemLink(string fullPath)
        {
            MenuItemVM menuItem = new MenuItemVM();
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullPath);
            menuItem.Name = this.GetFriendlyName(fileNameWithoutExtension);
            menuItem.ID = GetMenuID(fileNameWithoutExtension);
            menuItem.Action = $"ApplyRoute(/doc/{menuItem.ID})";
            menuItem.Url = GetFullPathUrl(fullPath);
            return (menuItem);
        }

        private string GetFriendlyName(string name)
        {
            int index = name.IndexOf(" - ");
            if (index < 0)
                return (name);
            string nameFriendly = name.Substring(index + 3);
            return (nameFriendly);
        }

        private string GetMenuID(string name) 
        {
            int index = name.IndexOf(" - ");
            if (index < 0)
                return (name);
            string nameFriendly = name.Substring(index + 3);
            string nameWithoutSpaces = nameFriendly.Replace(" ", "").Replace("-", "");
            return (nameWithoutSpaces);
        }

        private string GetFullPathUrl(string fullPath)
        {
            string relative = fullPath.Replace(_env.WebRootPath, string.Empty);
            string relativeRightSlash = relative.Replace(@"\","/");
            return (string.Format("~{0}", relativeRightSlash));
        }

        private MenuItemVM FindByName(List<MenuItemVM> items, string name)
        {
            foreach (MenuItemVM item in items)
            {
                if (item.Name == name)
                    return (item);
                MenuItemVM itemChild = FindByName(item.Items, name);
                if (itemChild != null)
                    return(itemChild);
            }
            return (null);
        }
    }
}