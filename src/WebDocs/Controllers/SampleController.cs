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
        public List<NodeVM> CreateNodes(int length = 5, int levels = 0, string prefix = null) 
        {
            List<NodeVM> nodes = new List<NodeVM>();
            this.Append(nodes, length, 0, levels, prefix);
            return (nodes);
        }

        private void Append(List<NodeVM> buffer, int length, int currentLevel, int levels, string prefix) 
        {
            if (currentLevel > levels)
                return;
            string prefixLocal = (currentLevel == 0) && (levels == 0) ? string.Empty : "L" + currentLevel.ToString();
            for (int i = 0; i < length; i++)
            {
                NodeVM node = new NodeVM();
                node.Key = $"{prefix ?? string.Empty}{prefixLocal}K{i}";
                node.Value = $"{prefixLocal}V{i}";
                this.Append(node.Nodes, length, currentLevel + 1, levels, prefix);
                buffer.Add(node);
            }
        }

        [HttpGet]
        public List<string> GetArray(int start = 0, int length = 10, string prefix = null, int? divisor = null)
        {
            List<string> array = new List<string>();
            for (int i = start; i < length; i++)
            {
                if ((divisor.HasValue) && ((i % divisor.Value) != 0))
                    continue;
                string value = $"{prefix}{i.ToString()}";
                array.Add(value);
            }
            return (array);
        }
    }
}
