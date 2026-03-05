using Il2CppYgomSystem.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BlindDuel
{
    public class DeckHandler : IMenuHandler
    {
        public bool CanHandle(string viewControllerName) =>
            viewControllerName is "DeckEdit" or "DeckBrowser" or "DeckMenu";

        public bool OnScreenEntered(string viewControllerName) => false;

        public string OnButtonFocused(SelectionButton button)
        {
            // Card in deck editor — show rarity
            if (button.name == "ImageCard")
            {
                string baseText = TextExtractor.ExtractFirst(button.gameObject);
                var rarityIcon = button.transform.Find("IconRarity");
                if (rarityIcon != null)
                {
                    string rarity = EnumUtil.ParseRarity(rarityIcon.GetComponent<Image>()?.sprite?.name);
                    return $"Owned: {baseText}, rarity: {rarity}";
                }
            }

            // Category filter buttons
            string gpName = button.transform.parent?.parent?.parent?.name;
            switch (gpName)
            {
                case "Category":
                    string category = TextExtractor.ExtractFirst(button.transform.parent?.parent?.gameObject);
                    string text = TextExtractor.ExtractFirst(button.gameObject);
                    return $"{text}, category: {category}";
                case "InputButton":
                    return "Rename deck button";
                case "AutoBuildButton":
                    return "Auto-build button";
            }

            // New deck button
            var addIcon = button.transform.Find("IconAddDeck");
            if (addIcon != null && addIcon.gameObject.activeInHierarchy)
                return "New deck button";

            return null;
        }
    }
}
