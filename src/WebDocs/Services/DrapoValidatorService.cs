using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebDocs.Models;

namespace WebDocs.Services
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
        private static readonly Regex FunctionCallRegex = new Regex(@"([A-Za-z_]\w*)\s*\(", RegexOptions.Compiled);

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
                CheckMustaches(html, diagnostics);
                await CheckAttributesAndFunctions(html, diagnostics);
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

        private async Task CheckAttributesAndFunctions(string html, List<DrapoDiagnosticVM> diagnostics)
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
                    Add(diagnostics, html, match.Groups[1].Index, "error", "unknown-attribute",
                        $"Unknown Drapo attribute '{attribute}'. It is not defined in the engine.");
                    // Even if the attribute is unknown, still inspect its value below where useful.
                }

                if (lower == "d-for")
                {
                    if (!DForRegex.IsMatch(value))
                        Add(diagnostics, html, valueGroup.Index, "error", "malformed-dfor",
                            $"Malformed d-for: '{value.Trim()}'. Expected the form '{{item}} in {{iterator}}'.");
                }

                if (lower.StartsWith("d-on-", StringComparison.Ordinal))
                {
                    foreach (var call in ScanFunctionCalls(value))
                    {
                        int index = valueGroup.Index + call.NameIndex;
                        if (!_engine.IsValidFunction(call.Name))
                        {
                            Add(diagnostics, html, index, "error", "unknown-function",
                                $"Unknown Drapo function '{call.Name}'. It is not dispatched by the engine.");
                            continue;
                        }
                        await CheckArity(call, index, docFunctionNames, parameterCache, html, diagnostics);
                    }
                }
            }
        }

        private async Task CheckArity(
            FunctionCall call, int index,
            Dictionary<string, string> docFunctionNames,
            Dictionary<string, List<FunctionParameterVM>> parameterCache,
            string html, List<DrapoDiagnosticVM> diagnostics)
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
            int provided = SplitArguments(call.Arguments).Count;
            // Only flag too-few arguments: many Drapo functions are variadic (e.g. CreateData),
            // so an upper bound would produce false positives.
            if (provided < required)
            {
                Add(diagnostics, html, index, "warning", "wrong-arity",
                    $"Function '{docName}' expects at least {required} argument(s) but got {provided}.");
            }
        }

        // Scans a handler expression for function calls, returning each name, its position, and
        // its raw argument text. Nested calls are returned too (each is validated independently).
        private static IEnumerable<FunctionCall> ScanFunctionCalls(string value)
        {
            foreach (Match m in FunctionCallRegex.Matches(value))
            {
                int parenIndex = m.Index + m.Length - 1;
                int depth = 0;
                int close = -1;
                for (int i = parenIndex; i < value.Length; i++)
                {
                    if (value[i] == '(') depth++;
                    else if (value[i] == ')') { depth--; if (depth == 0) { close = i; break; } }
                }
                string args = close > parenIndex ? value.Substring(parenIndex + 1, close - parenIndex - 1) : value.Substring(parenIndex + 1);
                yield return new FunctionCall
                {
                    Name = m.Groups[1].Value,
                    NameIndex = m.Groups[1].Index,
                    Arguments = args
                };
            }
        }

        // Splits a function argument string at top-level commas, ignoring commas nested inside
        // parentheses or mustache expressions.
        private static List<string> SplitArguments(string args)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(args))
                return result;
            int depth = 0;
            int mustache = 0;
            int start = 0;
            for (int i = 0; i < args.Length; i++)
            {
                if (i + 1 < args.Length && args[i] == '{' && args[i + 1] == '{') { mustache++; i++; continue; }
                if (i + 1 < args.Length && args[i] == '}' && args[i + 1] == '}') { if (mustache > 0) mustache--; i++; continue; }
                if (mustache > 0) continue;
                char c = args[i];
                if (c == '(') depth++;
                else if (c == ')') { if (depth > 0) depth--; }
                else if (c == ',' && depth == 0)
                {
                    result.Add(args.Substring(start, i - start));
                    start = i + 1;
                }
            }
            result.Add(args.Substring(start));
            return result;
        }

        private static void CheckMustaches(string html, List<DrapoDiagnosticVM> diagnostics)
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
                        Add(diagnostics, html, i, "error", "unbalanced-mustache", "Unexpected '}}' without a matching '{{'.");
                    else
                        openStack.Pop();
                    i++;
                }
            }
            foreach (int index in openStack)
                Add(diagnostics, html, index, "error", "unbalanced-mustache", "Unclosed '{{' without a matching '}}'.");
        }

        private static void Add(List<DrapoDiagnosticVM> diagnostics, string html, int index, string level, string rule, string message)
        {
            (int line, int column) = Position(html, index);
            diagnostics.Add(new DrapoDiagnosticVM { Level = level, Rule = rule, Message = message, Line = line, Column = column });
        }

        private static (int line, int column) Position(string text, int index)
        {
            int line = 1, column = 1;
            int max = Math.Min(index, text.Length);
            for (int i = 0; i < max; i++)
            {
                if (text[i] == '\n') { line++; column = 1; }
                else column++;
            }
            return (line, column);
        }

        private class FunctionCall
        {
            public string Name { get; set; }
            public int NameIndex { get; set; }
            public string Arguments { get; set; }
        }
    }
}
