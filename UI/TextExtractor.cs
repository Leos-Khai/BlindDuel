using System;
using System.Collections.Generic;
using UnityEngine;
using Il2CppTMPro;

namespace BlindDuel
{
    /// <summary>
    /// Result from text extraction — includes where the text was found and its content.
    /// </summary>
    public readonly record struct TextResult(string Path, string Text);

    /// <summary>
    /// Options for controlling text extraction behavior.
    /// </summary>
    public class TextSearchOptions
    {
        /// <summary>Only include text from active GameObjects.</summary>
        public bool ActiveOnly { get; init; } = true;

        /// <summary>Apply banned text filtering.</summary>
        public bool FilterBanned { get; init; } = true;

        /// <summary>Whether the current context has a menu (affects ban filter strictness).</summary>
        public bool HasMenuContext { get; init; }

        /// <summary>Maximum depth to recurse into children. -1 = unlimited.</summary>
        public int MaxDepth { get; init; } = -1;

        /// <summary>Skip elements whose path contains any of these substrings (case-insensitive).</summary>
        public List<string> ExcludePathContaining { get; init; }

        /// <summary>Only include elements whose path contains one of these substrings (case-insensitive).</summary>
        public List<string> IncludePathContaining { get; init; }

        /// <summary>Log prefix for debug output. Null = no logging.</summary>
        public string LogPrefix { get; init; }

        public static readonly TextSearchOptions Default = new();
        public static readonly TextSearchOptions ActiveFiltered = new() { ActiveOnly = true, FilterBanned = true };
        public static readonly TextSearchOptions AllInclusive = new() { ActiveOnly = false, FilterBanned = false };
    }

    /// <summary>
    /// Unified text extraction from Unity UI hierarchies.
    /// One tree walker to replace all the duplicated find-text functions.
    /// </summary>
    public static class TextExtractor
    {
        /// <summary>
        /// Extract all text from TMP_Text components under a root GameObject.
        /// </summary>
        public static List<TextResult> ExtractAll(GameObject root, TextSearchOptions options = null)
        {
            options ??= TextSearchOptions.Default;
            var results = new List<TextResult>();
            if (root == null) return results;

            var allTmp = root.GetComponentsInChildren<TMP_Text>(!options.ActiveOnly);
            if (allTmp == null) return results;

            foreach (var tmp in allTmp)
            {
                if (tmp == null) continue;
                if (options.ActiveOnly && !tmp.gameObject.activeInHierarchy) continue;

                string rawText = tmp.text;
                if (string.IsNullOrWhiteSpace(rawText)) continue;

                string cleanText = TextUtil.StripTags(rawText).Trim();
                if (string.IsNullOrWhiteSpace(cleanText)) continue;

                string path = $"{tmp.transform.parent?.name ?? "root"}/{tmp.name}";

                if (!PassesPathFilter(path, options)) continue;
                if (options.FilterBanned && TextUtil.IsBannedText(tmp.gameObject, cleanText, options.HasMenuContext)) continue;

                results.Add(new TextResult(path, cleanText));

                if (options.LogPrefix != null)
                    Log.Write($"{options.LogPrefix} {path} = {cleanText}");
            }

            return results;
        }

        /// <summary>
        /// Extract the first matching text from a root GameObject.
        /// </summary>
        public static string ExtractFirst(GameObject root, TextSearchOptions options = null)
        {
            var results = ExtractAll(root, options);
            return results.Count > 0 ? results[0].Text : null;
        }

        /// <summary>
        /// Extract text from a GameObject found by path.
        /// </summary>
        public static string ExtractFirst(string gameObjectPath, TextSearchOptions options = null)
        {
            var go = GameObject.Find(gameObjectPath);
            return go != null ? ExtractFirst(go, options) : null;
        }

        /// <summary>
        /// Extract all text from a GameObject found by path.
        /// </summary>
        public static List<TextResult> ExtractAll(string gameObjectPath, TextSearchOptions options = null)
        {
            var go = GameObject.Find(gameObjectPath);
            return go != null ? ExtractAll(go, options) : new List<TextResult>();
        }

        private static bool PassesPathFilter(string path, TextSearchOptions options)
        {
            if (options.ExcludePathContaining != null)
            {
                foreach (var exclude in options.ExcludePathContaining)
                {
                    if (path.Contains(exclude, StringComparison.OrdinalIgnoreCase))
                        return false;
                }
            }

            if (options.IncludePathContaining != null)
            {
                foreach (var include in options.IncludePathContaining)
                {
                    if (path.Contains(include, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                return false; // Had includes but none matched
            }

            return true;
        }
    }
}
