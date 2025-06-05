using System.Collections.Generic;

namespace WebDocs.Models
{
    public class FunctionVM
    {
        public string Name { set; get; }
        public string Description { set; get; }
        public List<FunctionParameterVM> Parameters { get; set; }
        public List<FunctionSampleVM> Samples { get; set; }
    }
}