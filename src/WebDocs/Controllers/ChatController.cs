using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Sysphera.Middleware.Drapo.Pipe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebDocs.Models;

namespace WebDocs.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]/[action]")]
    public class ChatController : Controller
    {
        private IDrapoPlumber _plumber;
        public ChatController(IDrapoPlumber plumber)
        {
            this._plumber = plumber;
        }
        private static List<ChatVM> _data = Create();
        public IActionResult Index()
        {
            return View();
        }

        public static List<ChatVM> Create()
        {
            List<ChatVM> chats = new List<ChatVM>();
            return (chats);
        }

        [HttpGet]
        public List<ChatVM> Get()
        {
            return (_data);
        }

        [HttpPost]
        public List<ChatVM> Set([FromBody] JObject data)
        {
            bool updated = false;
            //Inserted
            if (data["Inserted"] != null)
            {
                List<ChatVM> inserted = data["Inserted"].ToObject<List<ChatVM>>();
                if (inserted != null)
                {
                    foreach (ChatVM insert in inserted)
                    {
                        insert.Code = Guid.NewGuid();
                        insert.Date = DateTime.Now;
                        _data.Add(insert);
                        updated = true;
                    }
                }
            }
            //Deleted
            if (data["Deleted"] != null)
            {
                List<ChatVM> deleted = data["Deleted"].ToObject<List<ChatVM>>();
                if (deleted != null)
                {
                    foreach (ChatVM delete in deleted)
                    {
                        for (int i = 0; i < _data.Count; i++)
                        {
                            ChatVM item = _data[i];
                            if (item.Code != delete.Code)
                                continue;
                            _data.RemoveAt(i);
                            updated = true;
                            break;
                        }
                    }
                }
            }
            if (updated)
                _plumber.Send(new DrapoPipeMessage() { Data = "chat" });
            return (_data);
        }
    }
}
