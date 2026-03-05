using Il2CppYgomSystem.UI;
using UnityEngine;

namespace BlindDuel
{
    public class MissionsHandler : IMenuHandler
    {
        public bool CanHandle(string viewControllerName) =>
            viewControllerName is "MissionMenu" or "Mission";

        public bool OnScreenEntered(string viewControllerName) => false;

        public string OnButtonFocused(SelectionButton button)
        {
            if (button.name != "Locator") return null;

            try
            {
                // Walk up to the root mission container
                Transform rootParent = button.transform;
                for (int i = 0; i < 9 && rootParent.parent != null; i++)
                    rootParent = rootParent.parent;

                if (rootParent == null || rootParent.childCount == 0) return null;

                // Mission name from root
                string missionName = TextExtractor.ExtractFirst(rootParent.gameObject, new TextSearchOptions { FilterBanned = false });

                // Reward text
                string rewardText = "";
                var rewardChild = TransformSearch.GetChild(
                    TransformSearch.GetChild(button.transform, 0, "Missions/reward"),
                    2, "Missions/reward");
                if (rewardChild != null)
                {
                    string raw = TextExtractor.ExtractFirst(rewardChild.gameObject, new TextSearchOptions { FilterBanned = false });
                    rewardText = raw != null ? "x" + raw[1..] : "";
                }

                // Time remaining
                string timeText = "None";
                var timeChild = TransformSearch.GetChild(rootParent, 1, "Missions/time");
                timeChild = TransformSearch.GetChild(timeChild, 0, "Missions/time");
                timeChild = TransformSearch.GetChild(timeChild, 3, "Missions/time");
                timeChild = TransformSearch.GetChild(timeChild, 0, "Missions/time");
                if (timeChild != null)
                    timeText = TextExtractor.ExtractFirst(timeChild.gameObject, new TextSearchOptions { FilterBanned = false }) ?? "None";

                string result = missionName ?? "";
                Speech.SayDescription($"Reward: {rewardText}\nTime left: {timeText}");
                return result;
            }
            catch (System.Exception ex)
            {
                Log.Write($"[MissionsHandler] {ex.Message}");
                return null;
            }
        }
    }
}
