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
        // CJK Unified Ideographs, Hiragana, Katakana, CJK Symbols, Fullwidth Forms
        private static readonly Regex CJKPattern = new(@"[\u3000-\u303F\u3040-\u309F\u30A0-\u30FF\u4E00-\u9FFF\uFF00-\uFFEF]+", RegexOptions.Compiled);
        // Matches "1 word(s)" patterns — resolves to singular/plural based on the number
        private static readonly Regex PluralPattern = new(@"(\d+)\s+(\w+?)\(s\)", RegexOptions.Compiled);

        public static string StripTags(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return TagPattern.Replace(text, "");
        }

        /// <summary>
        /// Strips CJK unicode characters (Chinese/Japanese/Korean) from text.
        /// Screen readers often can't handle these characters properly.
        /// </summary>
        public static string StripCJK(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return CJKPattern.Replace(text, "").Trim();
        }

        /// <summary>
        /// Converts "43 day(s)" → "43 days", "1 day(s)" → "1 day", etc.
        /// Works for any word: day(s), hour(s), ticket(s), pack(s), etc.
        /// </summary>
        public static string FixPlurals(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return PluralPattern.Replace(text, FixPluralMatch);
        }

        private static string FixPluralMatch(Match m)
        {
            string num = m.Groups[1].Value;
            string word = m.Groups[2].Value;
            return num == "1" ? num + " " + word : num + " " + word + "s";
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
