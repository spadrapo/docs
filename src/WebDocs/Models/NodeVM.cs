using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebDocs.Models
{
    public class NodeVM
    {
        public string Key { set; get; }
        public string Value { set; get; }
        public List<NodeVM> Nodes { set; get; }

        public NodeVM() {
            this.Nodes = new List<NodeVM>();
        }
    }
}
