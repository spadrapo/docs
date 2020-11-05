using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebDocs.Models;

namespace WebDocs.Controllers
{
    [Route("api/[controller]/[action]")]
    public class SampleController : Controller
    {
        public static ValueDateVM _valueDate = null;
        [HttpGet]
        public ValueDateVM GetValue()
        {
            if (_valueDate == null)
            {
                _valueDate = new ValueDateVM();
                _valueDate.Date = DateTime.Now;
                _valueDate.Value = "John Doe";
            }
            return (_valueDate);
        }

        [HttpPost]
        public ValueDateVM SetValue([FromBody] ValueDateVM valueDate)
        {
            _valueDate = valueDate;
            _valueDate.Date = DateTime.Now;
            return (_valueDate);
        }

        [HttpGet]
        public List<NodeVM> CreateNodes(int length = 5, int levels = 0) 
        {
            List<NodeVM> nodes = new List<NodeVM>();
            this.Append(nodes, length, 0, levels);
            return (nodes);
        }

        private void Append(List<NodeVM> buffer, int length, int currentLevel, int levels) 
        {
            if (currentLevel > levels)
                return;
            string prefix = (currentLevel == 0) && (levels == 0) ? string.Empty : "L" + currentLevel.ToString();
            for (int i = 0; i < length; i++)
            {
                NodeVM node = new NodeVM();
                node.Key = $"{prefix}K{i}";
                node.Value = $"{prefix}V{i}";
                this.Append(node.Nodes, length, currentLevel + 1, levels);
                buffer.Add(node);
            }
        }
    }
}
