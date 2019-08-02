using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebDocs.Models
{
    public class FunctionParameterVM
    {
        public string Name { set; get; }
        public string Description { set; get; }
        public List<string> Types { set; get; }
        public bool Optional { set; get; }
    }
}
