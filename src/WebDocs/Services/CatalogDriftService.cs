using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sysphera.Middleware.Drapo;
using WebDocs.Models;

namespace WebDocs.Services
{
    /// <summary>
    /// Compares the documentation catalog against the Drapo engine bundled in the docs app.
    /// The engine is read from the embedded <c>drapo.js</c> resource of the
    /// Sysphera.Middleware.Drapo assembly (the same script the app serves at /drapo.js), so the
    /// check always runs against the exact engine version the docs ship — no cross-repo access.
    /// </summary>
    public class CatalogDriftService : ICatalogDriftService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IFunctionService _functions;
        private readonly IAttributeService _attributes;

        // Function dispatch in the engine is an exact comparison: functionParsed.Name === 'name'.
        private static readonly Regex FunctionRegex = new Regex(@"functionParsed\.Name\s*===\s*['""]([a-z0-9_]+)['""]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        // Attributes appear only as string literals (no central dispatch), so this is heuristic.
        private static readonly Regex AttributeRegex = new Regex(@"['""](d-[a-z][a-z-]*)['""]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // The engine is fixed for the process lifetime, so parse it once.
        private static readonly object EngineLock = new object();
        private static HashSet<string> _engineFunctions;
        private static HashSet<string> _engineAttributes;
        private static string _engineVersion;

        public CatalogDriftService(IWebHostEnvironment env, IFunctionService functions, IAttributeService attributes)
        {
            _env = env;
            _functions = functions;
            _attributes = attributes;
        }

        public async Task<CatalogDriftVM> GetDrift()
        {
            EnsureEngineParsed();
            AllowList allow = await LoadAllowList();

            // Functions: authoritative exact-name diff.
            var docFunctions = new HashSet<string>(
                (await _functions.GetNames()).Select(n => n.ToLowerInvariant()));
            CatalogCategoryDriftVM functions = BuildCategory(
                _engineFunctions, docFunctions,
                isSuppressed: name => allow.Functions.ContainsKey(name),
                allowReasonLookup: name => allow.Functions.GetValueOrDefault(name));

            // Attributes: heuristic diff (string literals + dynamic prefixes).
            var docAttributes = new HashSet<string>(
                (await _attributes.GetList()).Select(a => a.Name.ToLowerInvariant()));
            CatalogCategoryDriftVM attributes = BuildCategory(
                _engineAttributes, docAttributes,
                isSuppressed: name => IsSuppressedAttribute(name, allow),
                allowReasonLookup: name => allow.Attributes.GetValueOrDefault(name));

            return new CatalogDriftVM
            {
                EngineVersion = _engineVersion,
                Functions = functions,
                Attributes = attributes,
                AttributesAreHeuristic = true,
                HasDrift = functions.HasDrift
            };
        }

        private static CatalogCategoryDriftVM BuildCategory(
            HashSet<string> engine, HashSet<string> documented,
            Func<string, bool> isSuppressed, Func<string, string> allowReasonLookup)
        {
            var result = new CatalogCategoryDriftVM
            {
                EngineCount = engine.Count,
                DocumentedCount = documented.Count
            };
            foreach (string name in engine.Except(documented).OrderBy(n => n))
            {
                if (isSuppressed(name))
                    result.AllowListed.Add(Describe(name, allowReasonLookup(name)));
                else
                    result.Undocumented.Add(name);
            }
            foreach (string name in documented.Except(engine).OrderBy(n => n))
            {
                if (isSuppressed(name))
                    result.AllowListed.Add(Describe(name, allowReasonLookup(name)));
                else
                    result.Stale.Add(name);
            }
            return result;
        }

        private static string Describe(string name, string reason)
            => string.IsNullOrEmpty(reason) ? name : $"{name} ({reason})";

        // An attribute is not reported when it is an explicit allow-list entry, a dynamic prefix
        // itself (ends with '-'), or a member of a dynamic prefix family (e.g. d-on-click).
        private static bool IsSuppressedAttribute(string name, AllowList allow)
        {
            if (allow.Attributes.ContainsKey(name))
                return true;
            if (name.EndsWith("-", StringComparison.Ordinal))
                return true;
            foreach (string prefix in allow.AttributePrefixes)
            {
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static void EnsureEngineParsed()
        {
            if (_engineFunctions != null)
                return;
            lock (EngineLock)
            {
                if (_engineFunctions != null)
                    return;
                Assembly engineAssembly = typeof(DrapoMiddlewareOptions).Assembly;
                string js = ReadEngineJs(engineAssembly);
                _engineFunctions = ExtractMatches(FunctionRegex, js);
                _engineAttributes = ExtractMatches(AttributeRegex, js);
                _engineVersion = ResolveEngineVersion(engineAssembly);
            }
        }

        // The assembly version is a static 1.0.0.0; the package/file version (e.g. 2025.12.9.6)
        // is the meaningful one, so prefer informational/file version.
        private static string ResolveEngineVersion(Assembly engineAssembly)
        {
            try
            {
                if (!string.IsNullOrEmpty(engineAssembly.Location))
                {
                    string fileVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(engineAssembly.Location).FileVersion;
                    if (!string.IsNullOrWhiteSpace(fileVersion) && fileVersion != "1.0.0.0")
                        return fileVersion;
                }
            }
            catch
            {
                // fall through to attribute / assembly version
            }
            string informational = engineAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(informational))
                return informational;
            return engineAssembly.GetName().Version?.ToString() ?? "unknown";
        }

        private static HashSet<string> ExtractMatches(Regex regex, string js)
        {
            return new HashSet<string>(
                regex.Matches(js).Select(m => m.Groups[1].Value.ToLowerInvariant()));
        }

        private static string ReadEngineJs(Assembly engineAssembly)
        {
            string[] names = engineAssembly.GetManifestResourceNames();
            // Prefer the unminified drapo.js (stable identifiers) over drapo.min.js.
            string resource = names.FirstOrDefault(n => n.EndsWith("drapo.js", StringComparison.OrdinalIgnoreCase) && !n.EndsWith("min.js", StringComparison.OrdinalIgnoreCase))
                              ?? names.FirstOrDefault(n => n.EndsWith("drapo.js", StringComparison.OrdinalIgnoreCase));
            if (resource == null)
                throw new InvalidOperationException("Could not find the embedded drapo.js engine resource in the Drapo assembly.");
            using Stream stream = engineAssembly.GetManifestResourceStream(resource);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private async Task<AllowList> LoadAllowList()
        {
            string path = Path.Combine(_env.WebRootPath, "app", "catalog-allowlist.json");
            if (!File.Exists(path))
                return new AllowList();
            string json = await File.ReadAllTextAsync(path);
            return JsonConvert.DeserializeObject<AllowList>(json) ?? new AllowList();
        }

        /// <summary>Deserialized shape of catalog-allowlist.json.</summary>
        private class AllowList
        {
            [JsonProperty("functions")]
            public Dictionary<string, string> Functions { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            [JsonProperty("attributes")]
            public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            [JsonProperty("attributePrefixes")]
            public List<string> AttributePrefixes { get; set; } = new List<string>();
        }
    }
}
