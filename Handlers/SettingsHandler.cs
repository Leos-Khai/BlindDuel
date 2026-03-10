using System.Linq;
using Il2CppYgomSystem.UI;
using Il2CppYgomSystem.YGomTMPro;
using UnityEngine.UI;

namespace BlindDuel
{
    public class SettingsHandler : IMenuHandler
    {
        public bool CanHandle(string viewControllerName) =>
            viewControllerName is "SettingMenuViewController" or "GameSettingMenu";

        public bool OnScreenEntered(string viewControllerName) => false;

        public string OnButtonFocused(SelectionButton button)
        {
            // Skip layout/navigation buttons
            if (button.transform.parent?.parent?.name == "Layout") return null;
            if (button.transform.parent?.parent?.parent?.name == "EntryButtonsScrollView") return null;
            if (button.name == "CancelButton") return null;

            string baseText = TextExtractor.ExtractFirst(button.gameObject);
            string value;

            // Try slider first
            var slider = button.GetComponentInChildren<Slider>();
            if (slider != null)
            {
                value = $"{slider.value} of {slider.maxValue}";
            }
            else
            {
                // Mode text (toggle value)
                var modeTexts = button.GetComponentsInChildren<ExtendedTextMeshProUGUI>();
                var modeText = modeTexts?.FirstOrDefault(e => e.name == "ModeText");
                value = modeText?.text ?? "";
            }

            if (!string.IsNullOrEmpty(baseText) && !string.IsNullOrEmpty(value))
                return $"{baseText}\nValue is {value}";

            return null;
        }
    }
}
