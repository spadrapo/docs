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
    }
}
