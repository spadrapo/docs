using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Sysphera.Middleware.Drapo;

namespace WebDocs.Services
{
    /// <summary>
    /// Reads the engine bundled in the Sysphera.Middleware.Drapo assembly (the embedded drapo.js,
    /// the same script served at /drapo.js) and exposes its function/attribute sets. The engine is
    /// fixed for the process lifetime, so it is parsed once and cached.
    /// </summary>
    public class DrapoEngineCatalog : IDrapoEngineCatalog
    {
        // Function dispatch in the engine is an exact comparison: functionParsed.Name === 'name'.
        private static readonly Regex FunctionRegex = new Regex(@"functionParsed\.Name\s*===\s*['""]([a-z0-9_]+)['""]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        // Attributes appear only as string literals (no central dispatch).
        private static readonly Regex AttributeRegex = new Regex(@"['""](d-[a-z][a-z-]*)['""]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly object Lock = new object();
        private static HashSet<string> _functions;
        private static HashSet<string> _attributes;
        private static List<string> _attributePrefixes;
        private static string _version;

        public string EngineVersion
        {
            get { EnsureParsed(); return _version; }
        }

        public bool IsValidFunction(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;
            EnsureParsed();
            return _functions.Contains(name.ToLowerInvariant());
        }

        public bool IsValidAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;
            EnsureParsed();
            string lower = name.ToLowerInvariant();
            if (_attributes.Contains(lower))
                return true;
            // Dynamic prefix families: engine ships the prefix literal (ends with '-'),
            // e.g. d-on- -> d-on-click, d-attr- -> d-attr-href, d-dataproperty- -> d-dataproperty-x.
            foreach (string prefix in _attributePrefixes)
            {
                if (lower.StartsWith(prefix, StringComparison.Ordinal) && lower.Length > prefix.Length)
                    return true;
            }
            return false;
        }

        private static void EnsureParsed()
        {
            if (_functions != null)
                return;
            lock (Lock)
            {
                if (_functions != null)
                    return;
                Assembly engine = typeof(DrapoMiddlewareOptions).Assembly;
                string js = ReadEngineJs(engine);
                _functions = new HashSet<string>(FunctionRegex.Matches(js).Select(m => m.Groups[1].Value.ToLowerInvariant()));
                _attributes = new HashSet<string>(AttributeRegex.Matches(js).Select(m => m.Groups[1].Value.ToLowerInvariant()));
                _attributePrefixes = _attributes.Where(a => a.EndsWith("-", StringComparison.Ordinal)).ToList();
                _version = ResolveVersion(engine);
            }
        }

        private static string ReadEngineJs(Assembly engine)
        {
            string[] names = engine.GetManifestResourceNames();
            string resource = names.FirstOrDefault(n => n.EndsWith("drapo.js", StringComparison.OrdinalIgnoreCase) && !n.EndsWith("min.js", StringComparison.OrdinalIgnoreCase))
                              ?? names.FirstOrDefault(n => n.EndsWith("drapo.js", StringComparison.OrdinalIgnoreCase));
            if (resource == null)
                throw new InvalidOperationException("Could not find the embedded drapo.js engine resource in the Drapo assembly.");
            using Stream stream = engine.GetManifestResourceStream(resource);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static string ResolveVersion(Assembly engine)
        {
            string informational = engine.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(informational))
                return informational;
            return engine.GetName().Version?.ToString() ?? "unknown";
        }
    }
}
