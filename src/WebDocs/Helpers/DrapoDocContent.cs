using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using WebDocs.Models;

namespace WebDocs.Helpers
{
    /// <summary>
    /// Converts the raw HTML documentation stored on disk into clean Markdown/plain text
    /// suitable for MCP (LLM) consumption: no BOM, no raw HTML tags, decoded entities.
    /// Also builds human-readable function signatures from parameter metadata.
    /// </summary>
    public static class DrapoDocContent
    {
        private static readonly Regex TagRegex = new Regex("<[^>]+>", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex MultiBlankLine = new Regex(@"\n{3,}", RegexOptions.Compiled);
        private static readonly Regex TrailingSpaces = new Regex(@"[ \t]+\n", RegexOptions.Compiled);

        /// <summary>
        /// Converts documentation HTML (prose) to Markdown: strips BOM, maps common tags to
        /// Markdown, removes remaining tags, decodes entities, and normalizes whitespace.
        /// Code/pre blocks are preserved verbatim (decoded) so examples are not mangled.
        /// </summary>
        public static string ToMarkdown(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return html?.Trim();

            string text = StripBom(html);

            // Preserve code blocks first so the tag-stripping pass below can't damage examples.
            var preserved = new List<string>();
            text = Regex.Replace(text, @"<pre[^>]*>(.*?)</pre>", m =>
            {
                string code = WebUtility.HtmlDecode(StripTags(m.Groups[1].Value)).Trim();
                preserved.Add("\n```\n" + code + "\n```\n");
                return PlaceHolder(preserved.Count - 1);
            }, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            text = Regex.Replace(text, @"<code[^>]*>(.*?)</code>", m =>
            {
                string code = WebUtility.HtmlDecode(StripTags(m.Groups[1].Value));
                preserved.Add("`" + code + "`");
                return PlaceHolder(preserved.Count - 1);
            }, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // Block / inline tags -> Markdown.
            text = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<h1[^>]*>(.*?)</h1>", "\n# $1\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            text = Regex.Replace(text, @"<h2[^>]*>(.*?)</h2>", "\n## $1\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            text = Regex.Replace(text, @"<h3[^>]*>(.*?)</h3>", "\n### $1\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            text = Regex.Replace(text, @"<h[4-6][^>]*>(.*?)</h[4-6]>", "\n#### $1\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            text = Regex.Replace(text, @"</?(strong|b)\s*>", "**", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"</?(em|i)\s*>", "*", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<li[^>]*>(.*?)</li>", "- $1\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            text = Regex.Replace(text, @"<a[^>]*>(.*?)</a>", "$1", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            text = Regex.Replace(text, @"<p[^>]*>", "\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"</p>", "\n", RegexOptions.IgnoreCase);

            // Strip anything left, decode remaining entities, normalize whitespace.
            text = StripTags(text);
            text = WebUtility.HtmlDecode(text);
            text = Normalize(text);

            // Restore preserved code blocks.
            for (int i = 0; i < preserved.Count; i++)
                text = text.Replace(PlaceHolder(i), preserved[i]);

            return text.Trim();
        }

        /// <summary>
        /// Cleans a literal code sample (Drapo HTML markup) for LLM consumption: strips BOM,
        /// normalizes line endings, and trims. The markup itself is preserved verbatim.
        /// </summary>
        public static string CleanCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return code?.Trim();
            return StripBom(code).Replace("\r\n", "\n").Replace("\r", "\n").Trim();
        }

        /// <summary>
        /// Builds a one-line, human-readable signature from a function's parameters,
        /// e.g. <c>UpdateSector(SectorName: text, Url: url, [Title: text = null])</c>.
        /// Optional parameters are wrapped in brackets with their default value.
        /// </summary>
        public static string BuildFunctionSignature(string name, List<FunctionParameterVM> parameters)
        {
            var sb = new StringBuilder();
            sb.Append(name).Append('(');
            if (parameters != null)
            {
                for (int i = 0; i < parameters.Count; i++)
                {
                    if (i > 0)
                        sb.Append(", ");
                    FunctionParameterVM p = parameters[i];
                    string types = (p.Types != null && p.Types.Count > 0) ? string.Join("|", p.Types) : "any";
                    string core = $"{p.Name}: {types}";
                    if (p.Optional)
                    {
                        string def = string.IsNullOrEmpty(p.DefaultValue) ? "" : $" = {p.DefaultValue}";
                        sb.Append('[').Append(core).Append(def).Append(']');
                    }
                    else
                    {
                        sb.Append(core);
                    }
                }
            }
            sb.Append(')');
            return sb.ToString();
        }

        private static string StripTags(string value) => TagRegex.Replace(value, "");

        private static string StripBom(string value) => value.Replace("﻿", "").Replace("​", "");

        private static string PlaceHolder(int index) => $"PRESERVED{index}";

        private static string Normalize(string text)
        {
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");
            text = TrailingSpaces.Replace(text, "\n");
            text = MultiBlankLine.Replace(text, "\n\n");
            return text;
        }
    }
}
