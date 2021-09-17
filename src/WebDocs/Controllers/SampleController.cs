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

        [HttpPost]
        public object SetNameInListNames([FromBody] object names) 
        {
            return (names);
        }

        [HttpGet]
        public List<NodeVM> GetNames() 
        {
            List<NodeVM> namesVM = new List<NodeVM>();
            Dictionary<string, string> ListName = new Dictionary<string, string>();
            ListName.Add("1", "Akuma");
            ListName.Add("2", "Ken");
            ListName.Add("3", "Ryo");

            foreach (KeyValuePair<string, string> name in ListName)
            {
                NodeVM nameVM = new NodeVM();
                nameVM.Key = name.Key;
                nameVM.Value = name.Value;
                namesVM.Add(nameVM);
            }
            return (namesVM);
        }

        [HttpPost]
        public List<NodeVM> GetProfessionsByPeaples([FromBody] List<NodeVM> names) 
        {
            List<NodeVM> namesProfession = new List<NodeVM>();
            Dictionary<string, string> professions = new Dictionary<string, string>();
            professions.Add("1", "Developer");
            professions.Add("2", "Engineer");
            professions.Add("3", "Support");

            foreach (KeyValuePair<string, string> profession in professions)
            {
                NodeVM nameProfession = new NodeVM();
                NodeVM name= names.FirstOrDefault(_ => _.Key == profession.Key);
                nameProfession.Key = name.Value;
                nameProfession.Value = profession.Value;
                namesProfession.Add(nameProfession);
            }
            return (namesProfession);
        }

        [HttpGet]
        public NodeVM GetNameById(string idName) 
        {
            List<NodeVM> names = GetNames();
            NodeVM name = names.FirstOrDefault(_ => _.Key == idName);
            return name;
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
