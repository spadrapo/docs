using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebDocs.Models
{
    public class SearchItemVM
    {
        public string Name { set; get; }
        public string Type { set; get; } // "Function" or "Document"
        public string Action { set; get; }
        public string Description { set; get; }
    }
}