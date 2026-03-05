using Il2CppYgomSystem.UI;

namespace BlindDuel
{
    public class DuelPassHandler : IMenuHandler
    {
        public bool CanHandle(string viewControllerName) =>
            viewControllerName is "DuelPassMenu" or "DuelPass";

        public bool OnScreenEntered(string viewControllerName) => false;

        public string OnButtonFocused(SelectionButton button)
        {
            if (button.name.Contains("passRewardButton"))
            {
                string passType = button.name.Contains("Normalpass") ? "Normal" : "Gold";
                string grade = TextExtractor.ExtractFirst(button.transform.parent?.parent?.gameObject);
                string baseText = TextExtractor.ExtractFirst(button.gameObject);
                string quantity = !string.IsNullOrEmpty(baseText) ? "x" + baseText[1..] : "";

                return $"{passType} pass, grade {grade}, quantity: {quantity}";
            }

            return null;
        }
    }
}
