using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Il2CppTMPro;
using Il2CppYgomSystem.UI;
using Il2CppYgomSystem.YGomTMPro;
using HarmonyLib;

namespace BlindDuel
{
    [HarmonyPatch(typeof(ColorContainerGraphic), nameof(ColorContainerGraphic.SetColor))]
    class PatchColorContainerGraphic
    {
        // parent.parent name → label
        private static readonly Dictionary<string, string> ParentLabels = new()
        {
            { "ButtonMaintenance", "Maintenance" },
            { "ButtonBug", "Issues" },
            { "ButtonNotification", "Notification" },
            { "AutoBuildButton", "Auto-build button" },
            { "ButtonBookmark", "Add card to bookmark button" },
            { "BookmarkButton", "Bookmarked cards button" },
            { "HowToGetButton", "How to get button" },
            { "RelatedCard", "Related cards button" },
            { "AddButton", "Add +1" },
            { "RemoveButton", "Remove -1" },
            { "CardListButton", "Card list button" },
            { "HistoryButton", "Card history button" },
            { "ButtonRegulation", "Regulation button" },
            { "ButtonSecretPack", "Secret pack button" },
            { "ButtonInfoSwitching", "Switch display mode button" },
            { "ButtonSave", "Save button" },
            { "ButtonMenu", "Menu button" },
            { "ButtonPickupCard", "Show cards on decks preview" },
            { "BulkDecksDeletionButton", "Bulk deck deletion button" },
            { "ButtonOpenNeuronDecks", "Link with Yu Gi Oh Database" },
            { "FilterButton", "Filters button" },
            { "SortButton", "Sort button" },
            { "ClearButton", "Clear filters button" },
            { "ButtonDismantleIncrement", "Increment dismantle amount" },
            { "ButtonDismantleDecrement", "Decrement dismantle amount" },
            { "ButtonEnter", "Play" },
            { "CopyButton", "Copy deck button" },
            { "OKButton", "Ok" },
            { "ShowOwnedNumToggle", "Show owned button" },
        };

        // parent.parent.parent name → label
        private static readonly Dictionary<string, string> GrandparentLabels = new()
        {
            { "TabMyDeck", "My Deck" },
            { "TabRental", "Loaner" },
            { "DuelMenuButton", "Menu button" },
        };

        [HarmonyPostfix]
        static void Postfix(ColorContainerGraphic __instance)
        {
            try
            {
                if (__instance.currentStatusMode != ColorContainer.StatusMode.Enter) return;

                var parent = __instance.transform.parent.parent;
                var grandparent = parent.parent;
                string parentName = parent.name;
                string grandparentName = grandparent.name;

                // Duel card list — click and read card info
                if (NavigationState.IsInDuel && parentName.Contains("DuelListCard"))
                {
                    parent.GetComponent<SelectionButton>().Click();
                    // TODO: CopyUI equivalent via DuelHandler
                    return;
                }

                string text = null;

                // Dynamic cases needing computed text
                if (parentName is "DismantleButton" or "CreateButton")
                {
                    var costChild = TransformSearch.GetChild(parent, 6, "Dismantle/Create");
                    if (costChild != null)
                    {
                        string costText = TextExtractor.ExtractFirst(costChild.gameObject);
                        string rarity = EnumUtil.ParseRarity(costChild.GetComponentInChildren<Image>().sprite.name);

                        if (parentName == "DismantleButton" && string.IsNullOrEmpty(costText))
                            text = "Cant be dismantled";
                        else
                            text = $"{(parentName == "DismantleButton" ? "Dismantle" : "Create")} card for: {costText} {rarity} cp";
                    }
                }
                else if (parentName == "InputButton")
                {
                    text = NavigationState.CurrentMenu == Menu.None ? "Rename button/input" : "Search card input";
                }
                else if (parentName is "Button0")
                {
                    text = $"{TextExtractor.ExtractFirst(grandparent.gameObject)}, lower to higher";
                }
                else if (parentName is "Button1")
                {
                    text = $"{TextExtractor.ExtractFirst(grandparent.gameObject)}, higher to lower";
                }
                else if (ParentLabels.TryGetValue(parentName, out string label))
                {
                    text = label;
                }

                // Grandparent-level overrides
                if (grandparentName == "ChapterDuel(Clone)")
                {
                    var starsChild = TransformSearch.GetChild(parent, 4, "ChapterDuel/stars");
                    if (starsChild != null)
                        text = $"Duel, {TextExtractor.ExtractFirst(starsChild.gameObject)} stars";
                }
                else if (GrandparentLabels.TryGetValue(grandparentName, out string gpLabel))
                {
                    text = gpLabel;
                }

                if (!string.IsNullOrEmpty(text))
                    Speech.SayItem(text);
            }
            catch (Exception ex) { Log.Write($"[ColorContainer] {ex.Message}"); }
        }
    }

    [HarmonyPatch(typeof(SelectionButton), nameof(SelectionButton.OnSelected), MethodType.Normal)]
    class PatchOnSelected
    {
        /// <summary>
        /// Search sibling elements for text (handles toggle/radio widgets).
        /// </summary>
        static string FindSiblingText(Transform start)
        {
            var current = start;
            for (int level = 0; level < 3 && current.parent != null; level++)
            {
                var parent = current.parent;
                for (int i = 0; i < parent.childCount; i++)
                {
                    var sibling = parent.GetChild(i);
                    if (sibling == current) continue;
                    var tmp = sibling.GetComponent<TMP_Text>();
                    if (tmp != null && tmp.gameObject.activeInHierarchy)
                    {
                        string clean = TextUtil.StripTags(tmp.text ?? "").Trim();
                        if (!string.IsNullOrEmpty(clean))
                            return clean;
                    }
                }
                current = current.parent;
            }
            return null;
        }

        [HarmonyPostfix]
        static void Postfix(SelectionButton __instance)
        {
            // Extract text from button hierarchy
            string text = TextExtractor.ExtractFirst(__instance.gameObject);

            // Fallback: sibling text for toggle/radio widgets
            if (string.IsNullOrWhiteSpace(text))
                text = FindSiblingText(__instance.transform) ?? text;

            // Let the active handler enhance the text
            var handler = HandlerRegistry.Current;
            if (handler != null)
            {
                string enhanced = handler.OnButtonFocused(__instance);
                if (enhanced != null)
                    text = enhanced;
            }

            if (string.IsNullOrWhiteSpace(text)) return;

            // Speak the item name (interrupts), then queue index after it
            Speech.SayItem(text);

            var (index, total) = TransformSearch.GetButtonIndex(__instance);
            if (total > 1)
                Speech.SayIndex(index, total);
        }
    }

    [HarmonyPatch(typeof(SelectionButton), nameof(SelectionButton.OnClick), MethodType.Normal)]
    class PatchOnClick
    {
        static readonly HashSet<string> PreviewElements = new()
        {
            "CardPict", "CardClone", "CreateButton", "ImageCard",
            "NextButton", "PrevButton", "Related Cards", "ThumbButton",
            "SlotTemplate(Clone)", "Locator", "GoldpassRewardButton",
            "NormalpassRewardButton", "ButtonDuelPass"
        };

        // Button text → Menu mapping for click-based menu detection
        private static readonly Dictionary<string, Menu> TextToMenu = new()
        {
            { "DUEL", Menu.Duel },
            { "DECK", Menu.Deck },
            { "SOLO", Menu.Solo },
            { "SHOP", Menu.Shop },
            { "MISSION", Menu.Missions },
            { "Notifications", Menu.Notifications },
            { "Game Settings", Menu.Settings },
            { "Duel Pass", Menu.DuelPass }
        };

        [HarmonyPostfix]
        static void Postfix(SelectionButton __instance)
        {
            try
            {
                string btnText = TextExtractor.ExtractFirst(__instance.gameObject);
                if (btnText != null && TextToMenu.TryGetValue(btnText, out Menu menu))
                    NavigationState.CurrentMenu = menu;
            }
            catch (Exception ex) { Log.Write($"[OnClick] {ex.Message}"); }

            // Duel surrender
            if (__instance.name == "ButtonDecidePositive(Clone)" && NavigationState.IsInDuel)
            {
                NavigationState.IsInDuel = false;
                DuelState.Clear();
            }

            // Preview card info on click
            if (PreviewElements.Contains(__instance.name))
            {
                // TODO: Schedule CopyUI via handler
            }
        }
    }

    [HarmonyPatch(typeof(SelectionButton), nameof(SelectionButton.OnDeselected), MethodType.Normal)]
    class PatchOnDeselected
    {
        [HarmonyPostfix]
        static void Postfix(SelectionButton __instance)
        {
            Speech.ResetDedup();
        }
    }
}
