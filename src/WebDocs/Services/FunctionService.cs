using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WebDocs.Models;

namespace WebDocs.Services
{
    public class FunctionService : IFunctionService
    {
        private readonly IWebHostEnvironment _env;
        public FunctionService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> GetContent(string name)
        {
            return await CreateContent(name);
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
            return await Task.FromResult(functions);
        }

        private List<string> Sort(IEnumerable<string> entries)
        {
            return new List<string>(entries.OrderBy(s => s));
        }

        private async Task<string> CreateContent(string functionName)
        {
            StringBuilder content = new StringBuilder();
            string path = Path.Combine(_env.WebRootPath, "app", "functions", functionName);
            if (!Directory.Exists(path))
                return await Task.FromResult<string>(null);
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
            return await Task.FromResult(content.ToString());
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
                return await Task.FromResult(new List<FunctionParameterVM>());
            string fileContent = await System.IO.File.ReadAllTextAsync(path);
            return JsonConvert.DeserializeObject<List<FunctionParameterVM>>(fileContent);
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
            return content.ToString();
        }

        public async Task<List<FunctionVM>> GetList()
        {
            List<FunctionVM> functions = new List<FunctionVM>();
            List<string> names = await this.GetNames();
            foreach (string name in names)
            {
                FunctionVM function = await GetFunctionInternal(name, false);
                if (function != null)
                    functions.Add(function);
            }
            return (functions);
        }

        public async Task<FunctionVM> Get(string name)
        {
            List<string> names = await this.GetNames();
            if (!names.Contains(name))
                return null;
            return await GetFunctionInternal(name, true);
        }

        private async Task<FunctionVM> GetFunctionInternal(string name, bool loadDetails)
        {
            string path = Path.Combine(_env.WebRootPath, "app", "functions", name);
            string descriptionPath = Path.Combine(path, "description.html");
            if (!System.IO.File.Exists(descriptionPath))
                return (null);
            FunctionVM function = new FunctionVM();
            function.Name = name;
            function.Description = await File.ReadAllTextAsync(descriptionPath);
            if (loadDetails)
            {
                // Load parameters
                function.Parameters = await GetParameters(name);
                // Load samples
                string samplesPath = Path.Combine(path, "samples");
                var samples = new List<FunctionSampleVM>();
                if (Directory.Exists(samplesPath))
                {
                    List<string> sampleDirs = Sort(new List<string>(Directory.GetDirectories(samplesPath)));
                    foreach (var sampleDir in sampleDirs)
                    {
                        var sample = new FunctionSampleVM();
                        sample.Name = Path.GetFileName(sampleDir);
                        string descFile = Path.Combine(sampleDir, "description.html");
                        string contentFile = Path.Combine(sampleDir, "content.html");
                        sample.Description = System.IO.File.Exists(descFile) ? await File.ReadAllTextAsync(descFile) : null;
                        sample.Content = System.IO.File.Exists(contentFile) ? await File.ReadAllTextAsync(contentFile) : null;
                        samples.Add(sample);
                    }
                }
                function.Samples = samples;
            }
            return (function);
        }
    }
}
