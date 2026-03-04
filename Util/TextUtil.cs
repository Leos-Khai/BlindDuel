using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BlindDuel
{
    public static class TextUtil
    {
        private static readonly HashSet<string> BannedTexts = new() { "00:00", "You can add new Cards to your Deck." };

        private static readonly Regex TagPattern = new(@"<[^>]+>", RegexOptions.Compiled);
        private static readonly Regex WhitespaceOnly = new(@"^\s*$", RegexOptions.Compiled);
        private static readonly Regex WhitespaceOrPunctuation = new(@"^\s*$|[.!]+$", RegexOptions.Compiled);

        public static string StripTags(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return TagPattern.Replace(text, "");
        }

        /// <summary>
        /// Returns true if the text should be filtered out (not spoken).
        /// </summary>
        public static bool IsBannedText(string text, bool hasMenuContext)
        {
            if (string.IsNullOrWhiteSpace(text)) return true;
            if (BannedTexts.Contains(text)) return true;

            Regex pattern = hasMenuContext ? WhitespaceOnly : WhitespaceOrPunctuation;
            return pattern.IsMatch(text);
        }

        /// <summary>
        /// Returns true if the text should be filtered out, considering the element name.
        /// </summary>
        public static bool IsBannedText(GameObject textElement, string text, bool hasMenuContext)
        {
            if (string.IsNullOrWhiteSpace(text)) return true;
            if (BannedTexts.Contains(text)) return true;

            bool useStrictFilter = hasMenuContext || textElement.name.Equals("Button");
            Regex pattern = useStrictFilter ? WhitespaceOnly : WhitespaceOrPunctuation;
            return pattern.IsMatch(text);
        }
    }
}
