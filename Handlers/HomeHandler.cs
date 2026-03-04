using Il2CppYgomSystem.UI;
using UnityEngine;

namespace BlindDuel
{
    public class HomeHandler : IMenuHandler
    {
        public bool CanHandle(string viewControllerName) => viewControllerName == "Home";

        public void OnScreenEntered(string viewControllerName) { }

        public string OnButtonFocused(SelectionButton button)
        {
            // Profile button — append player level
            if (button.name == "ButtonPlayer")
            {
                var t = TransformSearch.FindByName(button.transform, "TextLevel");
                if (t != null)
                {
                    string level = TextExtractor.ExtractFirst(t.gameObject, TextSearchOptions.AllInclusive);
                    if (!string.IsNullOrEmpty(level))
                    {
                        string baseText = TextExtractor.ExtractFirst(button.gameObject);
                        return $"{baseText}, level {level}";
                    }
                }
            }

            // Friends button
            if (button.name == "SearchButton")
                return "Add friend button";
            if (button.name == "OpenToggle")
                return TextExtractor.ExtractFirst(button.transform.parent?.gameObject);

            // Event banner
            if (button.name == "DuelShortcut")
                return "Event banner";

            // Topics banner with page number
            if (button.name == "ButtonBanner")
            {
                var snap = button.transform.parent?.GetComponent<ScrollRectPageSnap>();
                if (snap != null)
                    return $"Topic banner, page {snap.hpage}";
            }

            // Notification popup on home screen
            try
            {
                var ancestor = button.transform.parent?.parent?.parent?.parent?.parent?.parent;
                if (ancestor != null && ancestor.name == "NotificationWidget")
                    return ReadNotificationText(button);
            }
            catch { }

            return null;
        }

        internal static string ReadNotificationText(SelectionButton button)
        {
            var textBody = button.transform.Find("TextBody");
            if (textBody == null) return null;

            string text = TextExtractor.ExtractFirst(textBody.gameObject, new TextSearchOptions { FilterBanned = false });

            var baseCategory = button.transform.Find("BaseCategory");
            if (baseCategory != null && baseCategory.gameObject.activeInHierarchy)
            {
                string status = TextExtractor.ExtractFirst(baseCategory.gameObject);
                if (!string.IsNullOrEmpty(status))
                    text += $"\nStatus: {status}";
            }

            return text;
        }
    }
}
