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
        public string GetDateDelay()
        {
            System.Threading.Thread.Sleep(5000);
            return (DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff tt"));
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

        [HttpGet]
        public string GetUsage(string key)
        {
            if (key == "d-model-event")
                return "<input class=\"edit\" type=\"text\" d-model=\"{{todo.Task}}\" d-model-event=\"blur\" d-on-blur=\"UncheckItemField({{todo.Edit}},false)\">";
            else if (key == "d-on-click")
                return "<input type=\"button\" value=\"Talk\" d-on-click=\"AddDataItem(chats, user, false); PostData(chats)\"/>";
            else if (key == "d-on-change")
                return "<select d-model=\"{{drapo.theme}}\" d-on-change=\"PostData(drapo); ReloadPage()\">";
            else if (key == "d-on-keyup-enter")
                return "<input type=\"text\" class=\"new- todo\"  d-model=\"{{taskAdd.Task}}\" d-on-keyup-enter=\"AddDataItem(todos, taskAdd); ClearDataField(taskAdd, Task)\">";
            else if (key == "d-on-dblclick")
                return "<label d-on-dblclick=\"CheckItemField({{todo.Edit}})\">\"Text\"</label>";
            else if (key == "d-on-blur")
                return "<input class=\"edit\" type=\"text\" d-model=\"{{todo.Task}}\" d-model-event=\"blur\" d-on-blur=\"UncheckItemField({{todo.Edit}},false)\">";
            else if (key == "d-sector")
                return "<div d-sector=\"sector\" d-sector-url=\"/DrapoPages/RouterOne.html\">";
            else if (key == "d-sector-url")
                return "<div d-sector=\"sector\" d-sector-url=\"/DrapoPages/RouterOne.html\">";
            else if (key == "d-sector-parent")
                return "<div d-sector-parent-url=\"/DrapoPages/RouterMaster.html\" d-sector-parent=\"sector\">";
            else if (key == "d-sector-parent-url")
                return "<div d-sector-parent-url=\"/DrapoPages/RouterMaster.html\" d-sector-parent=\"sector\">";
            else if (key == "d-route")
                return "<div d-sector=\"sectorSubMenu\" d-route=\"false\">";
            else if (key == "d-dataKey")
                return "<div d-dataKey=\"users\" d-dataUrlGet=\"/Data/GetIFSample\">";
            else if (key == "d-dataUrlGet")
                return "<div d-dataKey=\"cultures\" d-dataUrlGet=\"/Culture/GetCultures\">";
            else if (key == "d-dataUrlSet")
                return "<div d-dataKey=\"chats\" d-dataUnitOfWork=\"true\" d-dataUrlGet=\"/Chat/Get\" d-dataUrlSet=\"/Chat/Set\">";
            else if (key == "d-dataType=\"object\"")
                return "<div d-dataKey=\"response\" d-dataType=\"object\">";
            else if (key == "d-dataType=\"array\"")
                return "<div d-dataKey=\"list\" d-dataType=\"array\">";
            else if (key == "d-dataType=\"value\"")
                return "<div d-dataKey=\"response\" d-dataType=\"value\" d-dataValue=\"NewValue\">";
            else if (key == "d-dataValue")
                return "<div d-dataKey=\"response\" d-dataType=\"value\" d-dataValue=\"NewValue\">";
            else if (key == "d-model")
                return "<input type=\"text\" d-model=\"{{keyValue.Key}}\"/>";
            else if (key == "d-dataProperty-<name>")
                return "<div d-datakey=\"config\" d-dataType=\"object\" d-dataproperty-showactive=\"true\" d-dataproperty-showcompleted=\"true\">";
            else if (key == "d-attr-<name>")
                return "<input d-attr-placeholder=\"{{res.placeholder}}\" type=\"text\" d-attr-title=\"{{res.Tooltip}} \" placeholder=\"Name here\" title=\"I am a tip !\"/>";
            else if (key == "d-dataProperty-<name>-name")
                return "<div d-datakey=\"keyValue\" d-dataType=\"object\" d-dataproperty-key-name=\"Key\" d-dataproperty-key-value=\"admin\" d-dataurlset=\"/Authentication/Login\">";
            else if (key == "d-dataProperty-<name>-value")
                return "<div d-datakey=\"keyValue\" d-dataType=\"object\" d-dataproperty-key-name=\"Key\" d-dataproperty-key-value=\"admin\" d-dataurlset=\"/Authentication/Login\">";
            else if (key == "d-dataLazy")
                return "< div d-dataKey = \"users\" d-dataLazy = \"true\" d-dataLazyStart = \"0\" d-dataLazyIncrement = \"100\" d - dataUrlGet = \"/Data/GetData\"> ";
            else if (key == "d-dataLazyStart")
                return "< div d-dataKey = \"users\" d-dataLazy = \"true\" d-dataLazyStart = \"0\" d-dataLazyIncrement = \"100\" d - dataUrlGet = \"/Data/GetData\"> ";
            else if (key == "d-dataLazyIncrement")
                return "< div d-dataKey = \"users\" d-dataLazy = \"true\" d-dataLazyStart = \"0\" d-dataLazyIncrement = \"100\" d - dataUrlGet = \"/Data/GetData\"> ";
            else if (key == "d-dataDelay")
                return "<div d-dataKey=\"culture\" d-dataDelay=\"true\" d-dataUrlGet=\"/Data/GetCulture\">";
            else if (key == "d-dataUnitOfWork")
                return "<div d-dataKey=\"chats\" d-dataUnitOfWork=\"true\" d-dataUrlGet=\"/Chat/Get\" d-dataUrlSet=\"/Chat/Set\">";
            else if (key == "d-for")
                return "<div d-for=\"user in users\">";
            else if (key == "d-if")
                return "<option d-for=\"user in users\" d-if=\"{{user.Visible}}\" value=\"{{user.Key}}\">";
            else if (key == "d-dataConfigGet")
                return "<div d-dataKey=\"views\" d-dataConfigGet=\"Views\">";
            else if (key == "d-dataCookieGet")
                return "<div d-dataKey=\"drapo\" d-dataCookieGet=\"drapo\">";
            return "";
        }
    }
}
