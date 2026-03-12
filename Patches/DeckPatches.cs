using System;
using Il2CppYgomGame;
using Il2CppYgomGame.Card;
using Il2CppYgomGame.Deck;
using HarmonyLib;

namespace BlindDuel
{
    [HarmonyPatch(typeof(DeckEditViewController2), nameof(DeckEditViewController2.AddToMainOrExtraDeck),
        new Type[] { typeof(CardBaseData), typeof(bool) })]
    class PatchDeckAdd
    {
        [HarmonyPostfix]
        static void Postfix(DeckCard __result, CardBaseData baseData)
        {
            try
            {
                if (__result == null) return;
                int cardId = baseData._CardID_k__BackingField;
                string name = Content.s_instance?.GetName(cardId);
                if (string.IsNullOrEmpty(name)) return;

                // Count how many copies are now in the deck
                var vc = ScreenDetector.GetFocusVC()?.TryCast<DeckEditViewController2>();
                int inDeck = 0;
                if (vc != null)
                {
                    var mainCards = vc.m_MainDeckCards;
                    if (mainCards != null)
                        for (int i = 0; i < mainCards.Count; i++)
                            if (mainCards[i]._CardID_k__BackingField == cardId) inDeck++;

                    var extraCards = vc.m_ExtraDeckCards;
                    if (extraCards != null)
                        for (int i = 0; i < extraCards.Count; i++)
                            if (extraCards[i]._CardID_k__BackingField == cardId) inDeck++;
                }

                string text = inDeck > 0 ? $"Added {name}, now {inDeck} in deck" : $"Added {name}";
                Speech.SayImmediate(text);
            }
            catch (Exception ex) { Log.Write($"[DeckAdd] {ex.Message}"); }
        }
    }

    [HarmonyPatch(typeof(DeckEditViewController2), nameof(DeckEditViewController2.RemoveFromDeck),
        new Type[] { typeof(CardBaseData), typeof(bool) })]
    class PatchDeckRemoveByData
    {
        [HarmonyPostfix]
        static void Postfix(DeckCard __result, CardBaseData baseData)
        {
            try
            {
                if (__result == null) return;
                int cardId = baseData._CardID_k__BackingField;
                DeckPatchHelper.AnnounceRemoved(cardId);
            }
            catch (Exception ex) { Log.Write($"[DeckRemove] {ex.Message}"); }
        }
    }

    [HarmonyPatch(typeof(CardActionMenu), nameof(CardActionMenu.Close))]
    class PatchCardActionMenuClose
    {
        [HarmonyPrefix]
        static void Prefix(CardActionMenu __instance)
        {
            try
            {
                int cardId = __instance._m_CurrentCardID_k__BackingField;
                if (cardId <= 0) return;

                int rarityId = CardCollectionInfo.GetCardRarityID(cardId);
                int premiumId = (int)__instance.m_CurrentPremium;

                // Index from the menu's current position + VC list totals
                int index = -1, total = 0;
                try
                {
                    var vc = ScreenDetector.GetFocusVC()?.TryCast<DeckEditViewController2>();
                    if (vc != null)
                    {
                        if (__instance.fromDeck)
                        {
                            int mainCount = vc.m_MainDeckCards?.Count ?? 0;
                            int extraCount = vc.m_ExtraDeckCards?.Count ?? 0;
                            total = mainCount + extraCount;
                            index = __instance.m_CurrentIdx;
                        }
                        else
                        {
                            var collection = vc.m_CardCollection;
                            total = collection?.Count ?? 0;
                            index = DeckHandler.FindCollectionIndex(collection, cardId, premiumId);
                        }
                    }
                }
                catch { }

                // Dismantle check — construct minimal CardBaseData
                bool canDismantle = false;
                try
                {
                    var data = default(CardBaseData);
                    data._CardID_k__BackingField = cardId;
                    data._PremiumID_k__BackingField = premiumId;
                    data._Rarity_k__BackingField = rarityId;
                    canDismantle = CardCollectionInfo.IsDismantleable(data);
                }
                catch { }

                string text = DeckHandler.BuildCardSpeech(cardId, rarityId, premiumId,
                    owned: -1, index: index, total: total, canDismantle: canDismantle);
                if (string.IsNullOrEmpty(text)) return;

                Speech.ResetButtonDedup();
                Speech.SayQueued(text);
            }
            catch (Exception ex) { Log.Write($"[CardActionMenuClose] {ex.Message}"); }
        }
    }

    static class DeckPatchHelper
    {
        public static void AnnounceRemoved(int cardId)
        {
            string name = Content.s_instance?.GetName(cardId);
            if (string.IsNullOrEmpty(name)) return;

            // Count remaining copies in deck (after removal)
            var vc = ScreenDetector.GetFocusVC()?.TryCast<DeckEditViewController2>();
            int remaining = 0;
            if (vc != null)
            {
                var mainCards = vc.m_MainDeckCards;
                if (mainCards != null)
                    for (int i = 0; i < mainCards.Count; i++)
                        if (mainCards[i]._CardID_k__BackingField == cardId) remaining++;

                var extraCards = vc.m_ExtraDeckCards;
                if (extraCards != null)
                    for (int i = 0; i < extraCards.Count; i++)
                        if (extraCards[i]._CardID_k__BackingField == cardId) remaining++;
            }

            string text = $"Removed {name}, {remaining} in deck";
            Speech.SayImmediate(text);
        }
    }
}
