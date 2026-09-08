using System;
using System.Collections.Generic;
using System.Linq;
using Drapo.Tooling.Models;
using Drapo.Tooling.Services;

namespace Drapo.LanguageServer
{
    /// <summary>
    /// Immutable startup snapshot of everything completion/hover/signature help need: the engine's
    /// attribute and function sets plus the documented descriptions and signatures. Built once;
    /// content is fixed for the process lifetime (it ships with the extension).
    /// </summary>
    public sealed class DrapoSymbolIndex
    {
        private readonly Lazy<Snapshot> _snapshot;

        public DrapoSymbolIndex(IDrapoEngineCatalog catalog, IAttributeService attributes, IFunctionService functions)
        {
            _snapshot = new Lazy<Snapshot>(() => Build(catalog, attributes, functions));
        }

        /// <summary>Fixed engine attribute names, lower-case.</summary>
        public IReadOnlyCollection<string> EngineAttributes => _snapshot.Value.EngineAttributes;

        /// <summary>Dynamic prefixes, lower-case, ending with '-'.</summary>
        public IReadOnlyCollection<string> Prefixes => _snapshot.Value.Prefixes;

        /// <summary>Documented attributes recognised by the engine, keyed by lower-case name.</summary>
        public IReadOnlyDictionary<string, AttributeVM> Attributes => _snapshot.Value.Attributes;

        /// <summary>Documented functions (with parameters and signature), keyed by lower-case name.</summary>
        public IReadOnlyDictionary<string, FunctionVM> Functions => _snapshot.Value.Functions;

        public IDrapoEngineCatalog Catalog => _snapshot.Value.Catalog;

        public bool TryGetAttribute(string name, out AttributeVM attribute)
        {
            attribute = null;
            return !string.IsNullOrEmpty(name) && Attributes.TryGetValue(name.ToLowerInvariant(), out attribute);
        }

        public bool TryGetFunction(string name, out FunctionVM function)
        {
            function = null;
            return !string.IsNullOrEmpty(name) && Functions.TryGetValue(name.ToLowerInvariant(), out function);
        }

        private static Snapshot Build(IDrapoEngineCatalog catalog, IAttributeService attributeService, IFunctionService functionService)
        {
            var attributes = new Dictionary<string, AttributeVM>(StringComparer.Ordinal);
            foreach (AttributeVM a in attributeService.GetList().GetAwaiter().GetResult())
            {
                if (a?.Name == null || !catalog.IsValidAttribute(a.Name))
                    continue; // Principle I: never offer what the engine does not recognise.
                attributes[a.Name.ToLowerInvariant()] = a;
            }

            var functions = new Dictionary<string, FunctionVM>(StringComparer.Ordinal);
            foreach (string name in functionService.GetNames().GetAwaiter().GetResult())
            {
                FunctionVM f = functionService.Get(name).GetAwaiter().GetResult();
                if (f == null)
                    continue;
                f.Parameters ??= new List<FunctionParameterVM>();
                functions[name.ToLowerInvariant()] = f;
            }

            return new Snapshot
            {
                Catalog = catalog,
                EngineAttributes = catalog.Attributes.ToList(),
                Prefixes = catalog.AttributePrefixes.ToList(),
                Attributes = attributes,
                Functions = functions
            };
        }

        private sealed class Snapshot
        {
            public IDrapoEngineCatalog Catalog;
            public IReadOnlyCollection<string> EngineAttributes;
            public IReadOnlyCollection<string> Prefixes;
            public IReadOnlyDictionary<string, AttributeVM> Attributes;
            public IReadOnlyDictionary<string, FunctionVM> Functions;
        }
    }
}
