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
        // Section tracking — detect when user moves between deck/collection/main/extra
        private DeckEditViewController2.ViewType _lastViewType = DeckEditViewController2.ViewType.None;
        private DeckCard.LocationInDeck _lastDeckLocation = DeckCard.LocationInDeck.NA;

        public bool CanHandle(string viewControllerName) =>
            viewControllerName is "DeckSelect" or "DeckEdit" or "DeckBrowser";

        public bool OnScreenEntered(string viewControllerName)
        {
            _lastViewType = DeckEditViewController2.ViewType.None;
            _lastDeckLocation = DeckCard.LocationInDeck.NA;

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

            // CardActionMenu buttons (card utility panel: bookmark, how to get, craft, etc.)
            try
            {
                var actionMenu = button.GetComponentInParent<CardActionMenu>();
                if (actionMenu != null)
                    return HandleActionMenu(button, actionMenu);
            }
            catch (Exception ex) { Log.Write($"[DeckHandler] ActionMenu: {ex.Message}"); }

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

        private string HandleActionMenu(SelectionButton button, CardActionMenu actionMenu)
        {
            string text = null;

            var menu = actionMenu.m_MenuArea;
            if (menu != null)
            {
                if (button == menu.m_BookmarkButton)
                {
                    bool isOn = menu.m_BookmarkOn != null
                        && menu.m_BookmarkOn.gameObject.activeInHierarchy;
                    text = isOn ? "Bookmark, on" : "Bookmark, off";
                }
                else if (button == menu.m_HowToGetButton)
                    text = "How to obtain";
                else if (button == menu.m_RelatedCardButton)
                    text = "Related cards";
                else if (button == menu.m_AddCardButton)
                    text = "Add to deck";
                else if (button == menu.m_RemoveCardButton)
                    text = "Remove from deck";
            }

            var craft = actionMenu.m_CraftArea;
            if (text == null && craft != null)
            {
                if (button == craft.CreateButton)
                    text = ReadCraftButton(craft.CraftCreateButton, "Generate");
                else if (button == craft.DismantleButton)
                    text = ReadCraftButton(craft.CraftDismantleButton, "Dismantle");
            }

            // Card image button
            var cardArea = actionMenu.m_CardArea;
            if (text == null && cardArea != null && button == cardArea.m_CardImageButton)
                text = "Card image";

            if (text == null) return null;

            // Index from MenuArea's Selector (game-native navigable items list)
            try
            {
                var selector = menu?.m_Selector;
                var items = selector?.items;
                if (items != null && items.Count > 1)
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (items[i]?.TryCast<SelectionButton>() == button)
                        {
                            text += $"\n{i + 1} of {items.Count}";
                            break;
                        }
                    }
                }
            }
            catch { }

            return text;
        }

        private static string ReadCraftButton(CardActionMenu.CraftArea.CraftButtonWidget widget, string label)
        {
            if (widget == null) return label;

            string buttonText = widget.m_ButtonText?.text?.Trim();
            if (!string.IsNullOrEmpty(buttonText))
                label = buttonText;

            string cost = widget.m_TextCP?.text?.Trim();
            if (!string.IsNullOrEmpty(cost))
            {
                // Get rarity from icon sprite name
                string rarity = "";
                try
                {
                    var sprite = widget.m_IconCP?.sprite;
                    if (sprite != null)
                        rarity = EnumUtil.ParseRarity(sprite.name);
                }
                catch { }

                label += !string.IsNullOrEmpty(rarity)
                    ? $", {cost} {rarity} CP"
                    : $", {cost} CP";
            }

            // Check if disabled
            try
            {
                if (widget.m_IconDisabled != null && widget.m_IconDisabled.gameObject.activeInHierarchy)
                    label += ", unavailable";
                else if (widget.m_IconLocked != null && widget.m_IconLocked.gameObject.activeInHierarchy)
                    label += ", locked";
            }
            catch { }

            return label;
        }

        private string HandleCard(CardBase cardBase)
        {
            var baseData = cardBase.m_BaseData;
            int cardId = baseData._CardID_k__BackingField;

            var content = Content.s_instance;
            if (content == null || cardId <= 0) return null;

            string cardName = content.GetName(cardId);
            if (string.IsNullOrEmpty(cardName)) return null;

            // --- Section detection & index ---
            string sectionPrefix = null;
            int index = -1, total = 0;

            try
            {
                var vc = GetDeckEditVC();
                if (vc != null)
                {
                    var viewType = vc.currentView;

                    if (viewType == DeckEditViewController2.ViewType.Deck)
                    {
                        var deckCard = cardBase.TryCast<DeckCard>()
                            ?? cardBase.GetComponentInParent<DeckCard>();
                        var location = deckCard?.m_Location ?? DeckCard.LocationInDeck.NA;

                        // Detect section change (Collection→Deck, or Main↔Extra)
                        if (_lastViewType != viewType || _lastDeckLocation != location)
                        {
                            sectionPrefix = location switch
                            {
                                DeckCard.LocationInDeck.M => "Main Deck",
                                DeckCard.LocationInDeck.E => "Extra Deck",
                                _ => "Deck"
                            };
                        }

                        // Index within the deck section
                        var deckList = location == DeckCard.LocationInDeck.E
                            ? vc.m_ExtraDeckCards : vc.m_MainDeckCards;
                        total = deckList?.Count ?? 0;
                        index = GetDeckCardIndex(cardBase, deckList, cardId,
                            baseData._PremiumID_k__BackingField);

                        _lastDeckLocation = location;
                    }
                    else if (viewType == DeckEditViewController2.ViewType.CardCollection)
                    {
                        if (_lastViewType != viewType)
                            sectionPrefix = "Card Collection";

                        var collection = vc.m_CardCollection;
                        total = collection?.Count ?? 0;
                        index = FindCollectionIndex(collection, cardId,
                            baseData._PremiumID_k__BackingField);

                        _lastDeckLocation = DeckCard.LocationInDeck.NA;
                    }

                    _lastViewType = viewType;
                }
            }
            catch (Exception ex) { Log.Write($"[DeckHandler] Section: {ex.Message}"); }

            // --- Build speech text ---
            string result = "";

            // Prepend section change announcement
            if (sectionPrefix != null)
                result = sectionPrefix + ", ";

            bool canDismantle = false;
            try { canDismantle = CardCollectionInfo.IsDismantleable(baseData); }
            catch { }

            result += BuildCardSpeech(cardId, baseData._Rarity_k__BackingField,
                baseData._PremiumID_k__BackingField, baseData._Inventory_k__BackingField,
                index, total, canDismantle);

            return result;
        }

        // --- Shared card speech builder ---

        /// <summary>
        /// Build full card speech text from card ID and metadata.
        /// Used by HandleCard and the CardActionMenu close patch.
        /// </summary>
        public static string BuildCardSpeech(int cardId, int rarityId, int premiumId,
            int owned = -1, int index = -1, int total = 0, bool canDismantle = false)
        {
            var content = Content.s_instance;
            if (content == null || cardId <= 0) return null;

            string cardName = content.GetName(cardId);
            if (string.IsNullOrEmpty(cardName)) return null;

            string result = cardName;

            // Rarity
            string rarity = CardCollectionInfo.GetCardRarityText(rarityId);
            if (!string.IsNullOrEmpty(rarity))
                result += $", {rarity}";

            // Card finish (Glossy, Royal, etc.)
            string style = CardCollectionInfo.GetCardStyleText(premiumId);
            if (!string.IsNullOrEmpty(style))
                result += $", {style}";

            // Card stats from Content API
            var attr = content.GetAttr(cardId);

            if (attr == Content.Attribute.Magic || attr == Content.Attribute.Trap)
            {
                string spellType = content.GetIconFullText(cardId);
                if (!string.IsNullOrEmpty(spellType))
                    result += $", {spellType}";
            }
            else
            {
                int rank = content.GetRank(cardId);
                int star = content.GetStar(cardId);
                if (rank > 0)
                    result += $", Rank {rank}";
                else if (star > 0)
                    result += $", Level {star}";

                int atk = content.GetAtk(cardId);
                result += $", Attack {(atk >= 0 ? atk.ToString() : "?")}";

                var frame = content.GetFrame(cardId);
                if (frame == Content.Frame.Link)
                    result += $", Link {content.GetLinkNum(cardId)}";
                else
                {
                    int def = content.GetDef(cardId);
                    result += $", Defense {(def >= 0 ? def.ToString() : "?")}";
                }

                int scaleL = content.GetScaleL(cardId);
                if (scaleL > 0)
                    result += $", Pendulum scale {scaleL}";
            }

            // Element
            string element = content.GetAttributeText(attr);
            if (!string.IsNullOrEmpty(element))
                result += $", {element}";

            // Type line
            var type = content.GetType(cardId);
            var kind = content.GetKind(cardId);
            string typeText = content.GetTypeText(type);
            string kindText = content.GetKindText(kind);
            if (!string.IsNullOrEmpty(typeText) && !string.IsNullOrEmpty(kindText))
                result += $", {typeText}/{kindText}";
            else if (!string.IsNullOrEmpty(typeText))
                result += $", {typeText}";

            // Owned count
            if (owned >= 0)
                result += $", owned {owned}";

            // In-deck count
            try
            {
                var vc = GetDeckEditVC();
                if (vc != null)
                {
                    int inDeck = CountCardInDeck(vc, cardId);
                    result += $", in deck {inDeck}";
                }
            }
            catch { }

            // Index
            if (total > 0 && index >= 0)
                result += $"\n{index + 1} of {total},";

            // Description
            try
            {
                string desc = content.GetDesc(cardId);
                if (!string.IsNullOrEmpty(desc))
                    result += $"\n{desc}";
            }
            catch { }

            // Dismantle status
            result += canDismantle ? ", can dismantle" : ", cannot dismantle";

            return result;
        }

        // --- Index helpers ---

        /// <summary>
        /// Find card index within a deck section by matching against the data list.
        /// For duplicates, uses sibling position in the visual container as tiebreaker.
        /// </summary>
        private static int GetDeckCardIndex(CardBase cardBase, Il2CppSystem.Collections.Generic.List<CardBaseData> deckList,
            int cardId, int premiumId)
        {
            if (deckList == null) return -1;

            // Count which visual duplicate this is among siblings
            int visualDupeIdx = 0;
            try
            {
                var parent = cardBase.transform.parent;
                if (parent != null)
                {
                    for (int i = 0; i < parent.childCount; i++)
                    {
                        var child = parent.GetChild(i);
                        if (!child.gameObject.activeInHierarchy) continue;
                        if (child == cardBase.transform) break;
                        var cb = child.GetComponent<CardBase>();
                        if (cb != null && cb.m_BaseData._CardID_k__BackingField == cardId
                            && cb.m_BaseData._PremiumID_k__BackingField == premiumId)
                            visualDupeIdx++;
                    }
                }
            }
            catch { }

            // Find the nth matching entry in the data list
            int dupesSeen = 0;
            for (int i = 0; i < deckList.Count; i++)
            {
                var entry = deckList[i];
                if (entry._CardID_k__BackingField == cardId
                    && entry._PremiumID_k__BackingField == premiumId)
                {
                    if (dupesSeen == visualDupeIdx)
                        return i;
                    dupesSeen++;
                }
            }

            return -1;
        }

        /// <summary>
        /// Find card index in the collection list by matching CardID + PremiumID.
        /// </summary>
        public static int FindCollectionIndex(Il2CppSystem.Collections.Generic.List<CardBaseData> collection,
            int cardId, int premiumId)
        {
            if (collection == null) return -1;

            for (int i = 0; i < collection.Count; i++)
            {
                var entry = collection[i];
                if (entry._CardID_k__BackingField == cardId
                    && entry._PremiumID_k__BackingField == premiumId)
                    return i;
            }

            return -1;
        }

        private static int CountCardInDeck(DeckEditViewController2 vc, int cardId)
        {
            int count = 0;
            var mainCards = vc.m_MainDeckCards;
            if (mainCards != null)
            {
                for (int i = 0; i < mainCards.Count; i++)
                {
                    if (mainCards[i]._CardID_k__BackingField == cardId)
                        count++;
                }
            }
            var extraCards = vc.m_ExtraDeckCards;
            if (extraCards != null)
            {
                for (int i = 0; i < extraCards.Count; i++)
                {
                    if (extraCards[i]._CardID_k__BackingField == cardId)
                        count++;
                }
            }
            return count;
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
