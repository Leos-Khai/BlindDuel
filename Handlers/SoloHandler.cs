using System;
using System.Collections.Generic;
using Il2CppTMPro;
using Il2CppYgomSystem.UI;
using UnityEngine;

namespace BlindDuel
{
    public class SoloHandler : IMenuHandler
    {
        public bool CanHandle(string viewControllerName) =>
            viewControllerName is "SoloMode" or "SoloGate" or "SoloSelectChapter";

        public bool OnScreenEntered(string viewControllerName) => false;

        public string OnButtonFocused(SelectionButton button)
        {
            // Solo gate listing (has a "Main" child)
            if (button.transform.childCount > 0 && button.transform.GetChild(0).name == "Main")
            {
                try
                {
                    var elements = TextExtractor.ExtractAll(button.gameObject, new TextSearchOptions { FilterBanned = false });
                    if (elements.Count == 0) return null;

                    string gateName = elements[^1].Text;
                    string completion = "";
                    foreach (var e in elements)
                    {
                        if (e.Path.Contains("Complete"))
                        {
                            completion = e.Text;
                            break;
                        }
                    }

                    string result = gateName;
                    if (!string.IsNullOrEmpty(completion))
                        result += $", {completion}";

                    // Append gate overview from captured data
                    if (!string.IsNullOrEmpty(gateName) && SoloState.GateOverviews.TryGetValue(gateName, out string overview))
                    {
                        Speech.SayDescription(TextUtil.StripTags(overview).Trim());
                    }

                    return result;
                }
                catch (Exception ex) { Log.Write($"[SoloHandler] Gate: {ex.Message}"); }
            }

            // Solo chapter map nodes
            string chapterInfo = GetChapterInfo(button);
            if (chapterInfo != null)
            {
                string baseText = TextExtractor.ExtractFirst(button.gameObject);
                if (!string.IsNullOrEmpty(baseText))
                    return $"{baseText}, {chapterInfo}";
                return chapterInfo;
            }

            return null;
        }

        /// <summary>
        /// Extract chapter type and level from solo chapter map buttons.
        /// Walks up to 4 parents looking for a "Chapter*" container.
        /// </summary>
        private static string GetChapterInfo(SelectionButton button)
        {
            string chapterType = null;
            Transform current = button.transform.parent;
            for (int i = 0; i < 4 && current != null; i++)
            {
                if (current.name.StartsWith("Chapter"))
                {
                    chapterType = current.name;
                    break;
                }
                current = current.parent;
            }
            if (chapterType == null) return null;

            string typeName = chapterType.StartsWith("Chapter") ? chapterType[7..] : chapterType;

            // Try to find level text
            string level = null;
            try
            {
                var levelTransform = button.transform.parent?.Find("Level");
                if (levelTransform != null)
                {
                    var tmp = levelTransform.GetComponentInChildren<TMP_Text>(true);
                    if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text))
                        level = tmp.text.Trim();
                }
            }
            catch (Exception ex) { Log.Write($"[SoloHandler] Chapter level: {ex.Message}"); }

            string info = typeName;
            if (!string.IsNullOrEmpty(level))
                info += $", Level {level}";

            return info;
        }
    }
}
