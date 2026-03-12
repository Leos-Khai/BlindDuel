using System;
using Il2CppYgomGame;
using Il2CppYgomGame.Card;
using Il2CppYgomGame.Deck;
using Il2CppYgomSystem.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BlindDuel
{
    public class DeckHandler : IMenuHandler
    {
        public bool CanHandle(string viewControllerName) =>
            viewControllerName is "DeckSelect" or "DeckEdit" or "DeckBrowser";

        public bool OnScreenEntered(string viewControllerName)
        {
            if (viewControllerName == "DeckEdit")
                return AnnounceDeckEdit();

            if (viewControllerName == "DeckSelect")
                return AnnounceDeckSelect();

            return false;
        }

        public string OnButtonFocused(SelectionButton button)
        {
            // DeckBox widget (deck list items on DeckSelect)
            try
            {
                var deckBox = button.GetComponentInParent<DeckBox>();
                if (deckBox != null)
                    return HandleDeckBox(deckBox);
            }
            catch (Exception ex) { Log.Write($"[DeckHandler] DeckBox: {ex.Message}"); }

            // Card buttons (deck editor + card collection)
            try
            {
                var cardBase = button.GetComponentInParent<CardBase>();
                if (cardBase != null)
                    return HandleCard(cardBase);
            }
            catch (Exception ex) { Log.Write($"[DeckHandler] Card: {ex.Message}"); }

            // Category filter buttons
            try
            {
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
            }
            catch (Exception ex) { Log.Write($"[DeckHandler] Filter: {ex.Message}"); }

            return null;
        }

        // --- Screen announcements ---

        private bool AnnounceDeckSelect()
        {
            string header = ScreenDetector.ReadGameHeaderText();
            string announcement = header ?? "Deck";

            try
            {
                var vc = GetDeckSelectVC();
                if (vc != null)
                {
                    string deckNum = vc.m_TextDeckNum?.text?.Trim();
                    if (!string.IsNullOrEmpty(deckNum))
                        announcement += $", {deckNum}";
                }
            }
            catch (Exception ex) { Log.Write($"[DeckHandler] DeckSelect announce: {ex.Message}"); }

            Speech.AnnounceScreen(announcement);
            return true;
        }

        private bool AnnounceDeckEdit()
        {
            try
            {
                var vc = GetDeckEditVC();
                if (vc != null)
                {
                    string deckName = vc.m_DeckName;
                    int mainCount = vc.m_MainDeckCards?.Count ?? 0;
                    int extraCount = vc.m_ExtraDeckCards?.Count ?? 0;

                    string announcement = !string.IsNullOrEmpty(deckName) ? deckName : "Deck Editor";
                    announcement += $", main {mainCount}, extra {extraCount}";
                    Speech.AnnounceScreen(announcement);
                    return true;
                }
            }
            catch (Exception ex) { Log.Write($"[DeckHandler] DeckEdit announce: {ex.Message}"); }

            Speech.AnnounceScreen("Deck Editor");
            return true;
        }

        // --- Button handlers ---

        private string HandleDeckBox(DeckBox deckBox)
        {
            if (deckBox.m_Condition == DeckSelectViewController2.DeckCondition.New)
                return "New deck";

            string name = deckBox.deckName;
            if (string.IsNullOrEmpty(name))
                name = deckBox.m_DeckNameText?.text?.Trim();
            if (string.IsNullOrEmpty(name))
                return null;

            string result = name;

            try
            {
                if (deckBox.m_CurrentDeckIcon != null && deckBox.m_CurrentDeckIcon.activeInHierarchy)
                    result += ", current";
            }
            catch { }

            try
            {
                if (deckBox.m_DisabledIcon != null && deckBox.m_DisabledIcon.activeInHierarchy)
                    result += ", disabled";
            }
            catch { }

            // Index from m_Decks
            try
            {
                var vc = GetDeckSelectVC();
                if (vc?.m_Decks != null)
                {
                    int deckId = deckBox.deckID;
                    int myIdx = 0, myTotal = 0;
                    for (int i = 0; i < vc.m_Decks.Count; i++)
                    {
                        var dr = vc.m_Decks[i];
                        if (dr.deckType == DeckSelectViewController2.DeckEventType.MyDeck)
                        {
                            myTotal++;
                            if (dr.deckID == deckId)
                                myIdx = myTotal;
                        }
                    }
                    if (myIdx > 0)
                        result += $"\n{myIdx} of {myTotal}";
                }
            }
            catch { }

            return result;
        }

        private string HandleCard(CardBase cardBase)
        {
            var baseData = cardBase.m_BaseData;
            int cardId = baseData._CardID_k__BackingField;

            // Look up card name from game data
            string cardName = null;
            if (cardId > 0)
                cardName = Content.s_instance?.GetName(cardId);

            if (string.IsNullOrEmpty(cardName))
                return null;

            string result = cardName;

            // Rarity from sprite
            try
            {
                var rarityIcon = cardBase.transform.Find("IconRarity");
                if (rarityIcon == null)
                {
                    // DeckEditCard has it on the card widget, try parent search
                    var editCard = cardBase.GetComponentInChildren<DeckEditCard>();
                    if (editCard?.RarityIcon != null)
                    {
                        string rarity = EnumUtil.ParseRarity(editCard.RarityIcon.sprite?.name);
                        if (!string.IsNullOrEmpty(rarity))
                            result += $", {rarity}";
                    }
                }
                else
                {
                    var img = rarityIcon.GetComponent<Image>();
                    string rarity = EnumUtil.ParseRarity(img?.sprite?.name);
                    if (!string.IsNullOrEmpty(rarity))
                        result += $", {rarity}";
                }
            }
            catch { }

            // Owned count
            int owned = baseData._Inventory_k__BackingField;
            if (owned >= 0)
                result += $", owned {owned}";

            return result;
        }

        // --- VC accessors ---

        private static DeckSelectViewController2 GetDeckSelectVC()
        {
            return ScreenDetector.GetFocusVC()?.TryCast<DeckSelectViewController2>();
        }

        private static DeckEditViewController2 GetDeckEditVC()
        {
            return ScreenDetector.GetFocusVC()?.TryCast<DeckEditViewController2>();
        }
    }
}
