using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WebDocs.Models;
using WebDocs.Services;

namespace WebDocs.Controllers
{
    [Route("api/[controller]/[action]")]
    public class FunctionController : Controller
    {
        private readonly IFunctionService _functionService;
        public FunctionController(IFunctionService functionService)
        {
            _functionService = functionService;
        }

        [HttpGet]
        public async Task<ActionResult> GetContent([FromQuery] string name)
        {
            string content = await this._functionService.GetContent(name);
            if (content == null)
                return (NotFound());
            string etag = GenerateETag(Encoding.UTF8.GetBytes(content));
            string etagQuoted = '"' + etag + '"';
            Response.Headers.Add("ETag", new[] { etagQuoted });
            Response.Headers.Add("Last-Modified", new[] { (new DateTime(1980, 6, 3)).ToString("R") });
            Response.Headers.Add("Cache-Control", new[] { "no-cache" });
            Response.Headers.Add("Content-Type", new[] { "text/html" });
            if (IsNotModified(etagQuoted))
            {
                Response.StatusCode = 304;
                return Content(String.Empty);
            }
            Response.StatusCode = 200;
            return Content(content);
        }

        private bool IsNotModified(string etag)
        {
            if (!Request.Headers.Keys.Contains("If-None-Match"))
                return (false);
            return (Request.Headers["If-None-Match"] == etag);
        }

        private string GenerateETag(byte[] data)
        {
            string etag = string.Empty;
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(data);
                string hex = BitConverter.ToString(hash);
                etag = hex.Replace("-", "");
            }
            return (etag);
        }
    }
}
