using System;
using Il2CppYgomSystem.UI;

namespace BlindDuel
{
    public class TitleHandler : IMenuHandler
    {
        public bool CanHandle(string viewControllerName) => viewControllerName == "Title";

        public bool OnScreenEntered(string viewControllerName)
        {
            var focusVC = ScreenDetector.GetFocusVC();
            var eom = focusVC != null ? ScreenDetector.GetView(focusVC) : null;

            string announcement = "Title screen";
            if (eom != null)
            {
                string version = ElementReader.GetElementText(eom, "CodeVer");
                if (!string.IsNullOrWhiteSpace(version))
                {
                    if (version.StartsWith("Ver:", StringComparison.OrdinalIgnoreCase))
                        version = version[4..].Trim();
                    announcement += $". Version {version}";
                }

                string playerId = ElementReader.GetElementText(eom, "PlayerID");
                if (!string.IsNullOrWhiteSpace(playerId))
                    announcement += $". Player ID: {playerId}";
            }

            Speech.AnnounceScreen(announcement);

            // Queue the start prompt once after the screen announcement
            if (eom != null)
            {
                string startText = ElementReader.GetElementText(eom, "TextGameStart")
                                ?? ElementReader.GetElementText(eom, "PressMsgText");
                if (!string.IsNullOrWhiteSpace(startText))
                    Speech.SayQueued(startText);
            }

            return true;
        }

        public string OnButtonFocused(SelectionButton button)
        {
            string name = button.name;

            // The start button spams focus events during init — suppress it since
            // OnScreenEntered already queues the game start text once
            if (name.Contains("Start", StringComparison.OrdinalIgnoreCase))
                return "";

            return null;
        }
    }
}
