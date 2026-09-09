using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Drapo.Tooling.Helpers;
using Drapo.Tooling.Models;
using static Drapo.Tooling.Helpers.DrapoHandlerSyntax;

namespace Drapo.Tooling.Services
{
    /// <summary>
    /// Validates Drapo templates against the bundled engine (for attribute/function existence)
    /// and the documentation catalog (for function arity). See <see cref="IDrapoValidatorService"/>.
    /// </summary>
    public class DrapoValidatorService : IDrapoValidatorService
    {
        private readonly IDrapoEngineCatalog _engine;
        private readonly IFunctionService _functions;

        // d-name="value" or d-name='value' preceded by whitespace (i.e. inside a tag). Only
        // valued attributes are checked, to avoid matching attribute names mentioned in prose.
        private static readonly Regex AttributeRegex = new Regex(
            @"(?<=\s)(d-[A-Za-z][\w-]*)\s*=\s*(?:""([^""]*)""|'([^']*)')",
            RegexOptions.Compiled);
        private static readonly Regex DForRegex = new Regex(@"^\s*[A-Za-z_]\w*\s+in\s+\S+\s*$", RegexOptions.Compiled);

        public DrapoValidatorService(IDrapoEngineCatalog engine, IFunctionService functions)
        {
            _engine = engine;
            _functions = functions;
        }

        public async Task<DrapoValidationResultVM> Validate(string html)
        {
            var diagnostics = new List<DrapoDiagnosticVM>();
            if (!string.IsNullOrEmpty(html))
            {
                var lines = new LineMap(html);
                CheckMustaches(html, lines, diagnostics);
                await CheckAttributesAndFunctions(html, lines, diagnostics);
            }

            var ordered = diagnostics.OrderBy(d => d.Line).ThenBy(d => d.Column).ToList();
            return new DrapoValidationResultVM
            {
                EngineVersion = _engine.EngineVersion,
                Diagnostics = ordered,
                ErrorCount = ordered.Count(d => d.Level == "error"),
                WarningCount = ordered.Count(d => d.Level == "warning"),
                Valid = !ordered.Any(d => d.Level == "error")
            };
        }

        private async Task CheckAttributesAndFunctions(string html, LineMap lines, List<DrapoDiagnosticVM> diagnostics)
        {
            // Resolve documented function names once for case-insensitive arity lookup.
            var docFunctionNames = (await _functions.GetNames())
                .ToDictionary(n => n.ToLowerInvariant(), n => n);
            var parameterCache = new Dictionary<string, List<FunctionParameterVM>>();

            foreach (Match match in AttributeRegex.Matches(html))
            {
                string attribute = match.Groups[1].Value;
                string lower = attribute.ToLowerInvariant();
                Group valueGroup = match.Groups[2].Success ? match.Groups[2] : match.Groups[3];
                string value = valueGroup.Value;

                if (!_engine.IsValidAttribute(attribute))
                {
                    Add(diagnostics, lines, match.Groups[1].Index, "error", "unknown-attribute",
                        $"Unknown Drapo attribute '{attribute}'. It is not defined in the engine.");
                    // Even if the attribute is unknown, still inspect its value below where useful.
                }

                if (lower == "d-for")
                {
                    if (!DForRegex.IsMatch(value))
                        Add(diagnostics, lines, valueGroup.Index, "error", "malformed-dfor",
                            $"Malformed d-for: '{value.Trim()}'. Expected the form '{{item}} in {{iterator}}'.");
                }

                if (lower.StartsWith("d-on-", StringComparison.Ordinal))
                {
                    foreach (var call in ScanFunctionCalls(value))
                    {
                        int index = valueGroup.Index + call.NameIndex;
                        if (!_engine.IsValidFunction(call.Name))
                        {
                            Add(diagnostics, lines, index, "error", "unknown-function",
                                $"Unknown Drapo function '{call.Name}'. It is not dispatched by the engine.");
                            continue;
                        }
                        await CheckArity(call, index, docFunctionNames, parameterCache, lines, diagnostics);
                    }
                }
            }
        }

        private async Task CheckArity(
            FunctionCall call, int index,
            Dictionary<string, string> docFunctionNames,
            Dictionary<string, List<FunctionParameterVM>> parameterCache,
            LineMap lines, List<DrapoDiagnosticVM> diagnostics)
        {
            string key = call.Name.ToLowerInvariant();
            if (!docFunctionNames.TryGetValue(key, out string docName))
                return; // not documented -> no signature to check against

            if (!parameterCache.TryGetValue(key, out var parameters))
            {
                FunctionVM vm = await _functions.Get(docName);
                parameters = vm?.Parameters ?? new List<FunctionParameterVM>();
                parameterCache[key] = parameters;
            }

            int required = parameters.Count(p => !p.Optional);
            List<string> arguments = SplitArguments(call.Arguments);
            int provided = arguments.Count;
            // Only flag too-few arguments: many Drapo functions are variadic (e.g. CreateData),
            // so an upper bound would produce false positives.
            if (provided < required)
            {
                Add(diagnostics, lines, index, "warning", "wrong-arity",
                    $"Function '{docName}' expects at least {required} argument(s) but got {provided}.");
                return;
            }
            CheckConditionalArity(key, docName, arguments, index, lines, diagnostics);
        }

        /// <summary>
        /// Minimums the engine decides from the <em>value</em> of an earlier argument. The
        /// documented minimum is the unconditional one (so no valid call is ever flagged); these
        /// rules add the conditional case only when it is decidable from the literal text.
        /// </summary>
        private static void CheckConditionalArity(string key, string docName, List<string> arguments, int index, LineMap lines, List<DrapoDiagnosticVM> diagnostics)
        {
            // ExecuteFunctionShowWindow: `did = isUri ? Parameters[1] : null`, where the engine's
            // Parser.IsUri is "starts with '~' or '/'". A window definition name needs no did.
            if (key == "showwindow" && arguments.Count == 1 && IsLiteralUrl(arguments[0]))
            {
                Add(diagnostics, lines, index, "warning", "wrong-arity",
                    $"Function '{docName}' opens a url, so it needs the did of the container as second argument (only a window definition name can be used alone).");
            }

            // ExecuteFunctionCreateGuid: `if (Parameters.length == 0) return (value)`, otherwise
            // Parameters[0] and [1] are read unconditionally. Valid shapes are 0 or at least 2.
            if (key == "createguid" && arguments.Count == 1)
            {
                Add(diagnostics, lines, index, "warning", "wrong-arity",
                    $"Function '{docName}' takes either no argument (returns the guid) or a DataKey and a DataField; a DataKey alone does nothing useful.");
            }
        }

        /// <summary>The engine's Parser.IsUri rule applied to a literal argument (mustaches and nested calls are not decidable).</summary>
        private static bool IsLiteralUrl(string argument)
        {
            string value = argument?.Trim();
            return !string.IsNullOrEmpty(value) && (value[0] == '~' || value[0] == '/');
        }

        private static void CheckMustaches(string html, LineMap lines, List<DrapoDiagnosticVM> diagnostics)
        {
            var openStack = new Stack<int>();
            for (int i = 0; i + 1 < html.Length; i++)
            {
                if (html[i] == '{' && html[i + 1] == '{')
                {
                    openStack.Push(i);
                    i++;
                }
                else if (html[i] == '}' && html[i + 1] == '}')
                {
                    if (openStack.Count == 0)
                        Add(diagnostics, lines, i, "error", "unbalanced-mustache", "Unexpected '}}' without a matching '{{'.");
                    else
                        openStack.Pop();
                    i++;
                }
            }
            foreach (int index in openStack)
                Add(diagnostics, lines, index, "error", "unbalanced-mustache", "Unclosed '{{' without a matching '}}'.");
        }

        private static void Add(List<DrapoDiagnosticVM> diagnostics, LineMap lines, int index, string level, string rule, string message)
        {
            (int line, int column) = lines.Position(index);
            diagnostics.Add(new DrapoDiagnosticVM { Level = level, Rule = rule, Message = message, Line = line, Column = column });
        }

        /// <summary>
        /// Offset → 1-based (line, column) in O(log n): lines are split on LF only, so a CR
        /// counts as an ordinary column character (it sits at the line end, never before a token).
        /// </summary>
        private sealed class LineMap
        {
            private readonly int[] _lineStarts;
            private readonly int _length;

            public LineMap(string text)
            {
                _length = text.Length;
                var starts = new List<int> { 0 };
                for (int i = 0; i < text.Length; i++)
                    if (text[i] == '\n')
                        starts.Add(i + 1);
                _lineStarts = starts.ToArray();
            }

            public (int line, int column) Position(int index)
            {
                int offset = Math.Min(Math.Max(0, index), _length);
                int lo = 0, hi = _lineStarts.Length - 1;
                while (lo < hi)
                {
                    int mid = (lo + hi + 1) / 2;
                    if (_lineStarts[mid] <= offset) lo = mid; else hi = mid - 1;
                }
                return (lo + 1, offset - _lineStarts[lo] + 1);
            }
        }
    }
}
