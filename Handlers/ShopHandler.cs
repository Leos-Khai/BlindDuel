using System.Collections.Generic;
using Il2CppYgomSystem.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BlindDuel
{
    public class ShopHandler : IMenuHandler
    {
        public bool CanHandle(string viewControllerName) =>
            viewControllerName is "Shop" or "ShopMenu";

        public bool OnScreenEntered(string viewControllerName) => false;

        public string OnButtonFocused(SelectionButton button)
        {
            // Pack listing in shop
            if (button.transform.parent != null && button.transform.parent.name.Contains("Shop"))
            {
                var texts = TextExtractor.ExtractAll(button.gameObject);
                if (texts.Count == 0) return null;

                string FindByPath(string keyword)
                {
                    foreach (var t in texts)
                        if (t.Path.Contains(keyword)) return t.Text;
                    return null;
                }

                string pickup = FindByPath("PickupMessage") ?? "";
                string name = FindByPath("Name") ?? "";
                string isNew = FindByPath("New") ?? "";
                string limit = FindByPath("Limit") ?? "None";
                string price = FindByPath("PriceGroup") ?? "";

                // Category header
                string category = TextExtractor.ExtractFirst(
                    "UI/ContentCanvas/ContentManager/Shop/ShopUI(Clone)/Root/Main/ProductsRoot/ShowcaseWidget/ListRoot/ProductList/Viewport/Mask/Content/ShopGroupHeaderWidget(Clone)/Label",
                    new TextSearchOptions { FilterBanned = false });

                string itemName = $"{pickup}{(pickup != "" ? " - " : "")}{name}{(isNew != "" ? $" ({isNew})" : "")}";

                string result = $"{itemName}\nCategory: {category}\nTime left: {limit}\nPrice: {price}";

                var (index, total) = TransformSearch.GetButtonIndex(button);
                if (total > 1)
                    result += $"\n{index} of {total}";

                return result;
            }

            // Individual card in pack
            if (button.name == "CardPict")
            {
                var numArea = button.transform.parent?.Find("NumTextArea");
                string ownedText = numArea != null ? TextExtractor.ExtractFirst(numArea.gameObject) : "";
                if (!string.IsNullOrEmpty(ownedText)) ownedText = "x" + ownedText[1..];

                var rarityIcon = button.transform.parent?.Find("IconRarity");
                string rarity = "";
                if (rarityIcon != null)
                    rarity = EnumUtil.ParseRarity(rarityIcon.GetComponent<Image>()?.sprite?.name);

                var newIcon = button.transform.parent?.Find("NewIcon");
                bool isNew = newIcon != null && newIcon.gameObject.activeInHierarchy;

                string result = $"Rarity: {rarity}, New: {(isNew ? "Yes" : "No")}, Owned: {ownedText}";
                return result;
            }

            return null;
        }
    }
}
