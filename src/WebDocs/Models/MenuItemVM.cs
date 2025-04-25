using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebDocs.Models
{
    public class MenuItemVM
    {
        public string Name { set; get; }
        public string Action { set; get; }
        public List<MenuItemVM> Items { set; get; }
        public bool IsExpandable { set; get; }
        public bool IsExpanded { set; get; }
        public string Url { set; get; }
        public string ID { set; get; }
        public MenuItemVM()
        {
            this.Items = new List<MenuItemVM>();
        }
    }
}
