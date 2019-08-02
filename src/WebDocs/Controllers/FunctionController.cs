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

namespace WebDocs.Controllers
{
    [Route("api/[controller]/[action]")]
    public class FunctionController : Controller
    {
        IHostingEnvironment _env;
        public FunctionController(IHostingEnvironment env)
        {
            _env = env;
        }

        [HttpGet]
        public async Task<ActionResult> GetContent([FromQuery] string name)
        {
            string content = await this.CreateContent(name);
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

        public async Task<List<string>> GetNames()
        {
            List<string> functions = new List<string>();
            string path = Path.Combine(_env.WebRootPath, "app", "functions");
            IEnumerable<string> entries = Directory.EnumerateFileSystemEntries(path);
            List<string> entriesSorted = this.Sort(entries);
            for (int i = 0; i < entriesSorted.Count; i++)
            {
                string entry = entriesSorted[i];
                string pathEntry = Path.Combine(path, entry);
                if (Directory.Exists(pathEntry))
                    functions.Add(Path.GetFileName(entry));
            }
            return (await Task.FromResult<List<string>>(functions));
        }

        private List<string> Sort(IEnumerable<string> entries)
        {
            return (new List<string>(entries.OrderBy(s => s)));
        }

        private async Task<string> CreateContent(string functionName)
        {
            StringBuilder content = new StringBuilder();
            string path = Path.Combine(_env.WebRootPath, "app", "functions", functionName);
            if (!Directory.Exists(path))
                return (await Task.FromResult<string>(null));
            //Title
            content.AppendLine("<div>");
            content.AppendLine($"<h2>{functionName}</h2>");
            content.AppendLine("</div>");
            //Description
            await AppendContent(content, Path.Combine(path, "description.html"));
            content.AppendLine("<br>");
            //Syntax
            List<FunctionParameterVM> parameters = await this.GetParameters(functionName);
            content.AppendLine($"<h3>Syntax</h3>");
            content.Append($"<span>{functionName}(");
            for (int i = 0; i < parameters.Count; i++)
            {
                if (i > 0)
                    content.Append($",");
                FunctionParameterVM parameter = parameters[i];
                string prefix = parameter.Optional ? "[" : "";
                string sufix = parameter.Optional ? "]" : "";
                content.Append($"{prefix}{parameter.Name}{sufix}");
            }
            content.AppendLine(")<br>");
            content.AppendLine("<br>");
            //Parameters
            if (parameters.Count > 0)
            {
                content.AppendLine($"<h3>Parameters</h3>");
                content.AppendLine("<table class='dFunctionParametersTable'>");
                content.AppendLine("<tr>");
                content.AppendLine("<th>Name</th>");
                content.AppendLine("<th>Types</th>");
                content.AppendLine("<th>Optional</th>");
                content.AppendLine("<th>Default Value</th>");
                content.AppendLine("<th>Description</th>");
                content.AppendLine("</tr>");
                for (int i = 0; i < parameters.Count; i++)
                {
                    FunctionParameterVM parameter = parameters[i];
                    content.AppendLine("<tr>");
                    content.AppendLine($"<td>{parameter.Name}</td>");
                    content.AppendLine($"<td>{GetTypes(parameter.Types)}</td>");
                    content.AppendLine($"<td>{(parameter.Optional ? "true" : "false")}</td>");
                    content.AppendLine($"<td>{parameter.DefaultValue}</td>");
                    content.AppendLine($"<td>{parameter.Description}</td>");
                    content.AppendLine("</tr>");
                }
                content.AppendLine("</table>");
                content.AppendLine("<br>");
            }
            //Samples
            string pathSamples = Path.Combine(path, "samples");
            if (Directory.Exists(pathSamples))
            {
                List<string> samples = Sort(new List<string>(Directory.GetDirectories(pathSamples)));
                if (samples.Count > 0)
                {
                    content.AppendLine($"<h3>Samples</h3>");
                    foreach (string sample in samples)
                    {
                        await AppendContent(content, Path.Combine(pathSamples, sample, "description.html"));
                        await AppendContent(content, Path.Combine(pathSamples, sample, "content.html"), " <d-sample>", "</d-sample>");
                        content.AppendLine("<br>");
                    }
                }
            }
            return (await Task.FromResult<string>(content.ToString()));
        }

        private async Task AppendContent(StringBuilder content, string file, string prefix = null, string sufix = null)
        {
            if (!System.IO.File.Exists(file))
                return;
            string fileContent = await System.IO.File.ReadAllTextAsync(file);
            if (!string.IsNullOrEmpty(prefix))
                content.AppendLine(prefix);
            content.AppendLine(fileContent);
            if (!string.IsNullOrEmpty(sufix))
                content.AppendLine(sufix);
        }

        private async Task<List<FunctionParameterVM>> GetParameters(string functionName)
        {
            string path = Path.Combine(_env.WebRootPath, "app", "functions", functionName, "parameters.json");
            if (!System.IO.File.Exists(path))
                return (await Task.FromResult<List<FunctionParameterVM>>(new List<FunctionParameterVM>()));
            string fileContent = await System.IO.File.ReadAllTextAsync(path);
            return (JsonConvert.DeserializeObject<List<FunctionParameterVM>>(fileContent));
        }

        private string GetTypes(List<string> types)
        {
            StringBuilder content = new StringBuilder();
            for (int i = 0; i < types.Count; i++)
            {
                if (i > 0)
                    content.Append(", ");
                content.AppendFormat("{0}", types[i]);
            }
            return (content.ToString());
        }
    }
}
