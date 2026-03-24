using System;
using Il2CppYgomSystem.UI;
using Il2CppYgomGame.Card;
using Il2CppYgomGame.CardPack.Open.Actor;
using UnityEngine;

namespace BlindDuel
{
    /// <summary>
    /// Handler for card pack opening and result screens.
    /// Announces card rarity when face-down, full card name when revealed.
    /// </summary>
    public class CardPackOpenHandler : IMenuHandler
    {
        public bool CanHandle(string viewControllerName) =>
            viewControllerName is "CardPackOpen" or "CardPackOpenResult";

        public bool OnScreenEntered(string viewControllerName)
        {
            if (viewControllerName == "CardPackOpenResult")
                Speech.AnnounceScreen("Pack Results");
            else
                Speech.AnnounceScreen("Pack Opening");
            return true;
        }

        public string OnButtonFocused(SelectionButton button)
        {
            try
            {
                // Find the CardPackCardActor for this button
                var actor = FindActor(button);
                if (actor != null)
                {
                    int mrk = actor._mrk_k__BackingField;
                    bool isFaceUp = IsFaceUp(actor);

                    if (isFaceUp && mrk > 0)
                    {
                        // Card is revealed - speak full name + rarity
                        var card = CardReader.ReadCardFromData(mrk);
                        string name = card?.Name;
                        if (string.IsNullOrEmpty(name)) return null;

                        string result = name;

                        try
                        {
                            string rarityText = CardCollectionInfo.GetCardRarityText(
                                (int)CardCollectionInfo.GetCardRarity(mrk));
                            if (!string.IsNullOrEmpty(rarityText))
                                result += $", {rarityText}";
                        }
                        catch { }

                        return result;
                    }
                    else
                    {
                        // Card is face-down - speak rarity from back glow
                        string rarity = "Unknown";
                        try
                        {
                            // Try to get rarity from DrawCardData.backSideRarity
                            // or from the card database if mrk is available
                            if (mrk > 0)
                            {
                                string rarityText = CardCollectionInfo.GetCardRarityText(
                                    (int)CardCollectionInfo.GetCardRarity(mrk));
                                if (!string.IsNullOrEmpty(rarityText))
                                    rarity = rarityText;
                            }
                        }
                        catch { }

                        return $"Face-down card, {rarity}";
                    }
                }

                // Result screen: try CardWidget
                int resultMrk = FindResultMrk(button);
                if (resultMrk > 0)
                {
                    var card = CardReader.ReadCardFromData(resultMrk);
                    string name = card?.Name;
                    if (!string.IsNullOrEmpty(name))
                    {
                        string result = name;
                        try
                        {
                            string rarityText = CardCollectionInfo.GetCardRarityText(
                                (int)CardCollectionInfo.GetCardRarity(resultMrk));
                            if (!string.IsNullOrEmpty(rarityText))
                                result += $", {rarityText}";
                        }
                        catch { }
                        return result;
                    }
                }
            }
            catch (Exception ex) { Log.Write($"[CardPackOpen] {ex.Message}"); }

            return null;
        }

        private static CardPackCardActor FindActor(SelectionButton button)
        {
            var transform = button.transform;
            for (int i = 0; i < 5 && transform != null; i++)
            {
                try
                {
                    var actor = transform.GetComponent<CardPackCardActor>();
                    if (actor != null) return actor;
                }
                catch { }
                transform = transform.parent;
            }
            return null;
        }

        /// <summary>
        /// Check if the card has been flipped face-up by checking
        /// if the front renderer's GameObject is active.
        /// </summary>
        private static bool IsFaceUp(CardPackCardActor actor)
        {
            try
            {
                var frontRenderer = actor.m_FrontRenderer;
                if (frontRenderer != null)
                    return frontRenderer.gameObject.activeInHierarchy;
            }
            catch { }
            return false;
        }

        private static int FindResultMrk(SelectionButton button)
        {
            var transform = button.transform;
            for (int i = 0; i < 5 && transform != null; i++)
            {
                try
                {
                    var widget = transform.GetComponent<Il2CppYgomGame.CardPack.CardWidget>();
                    if (widget != null) return widget.m_Mrk;
                }
                catch { }
                try
                {
                    var resultWidget = transform.GetComponent<Il2CppYgomGame.CardPack.OpenResult.CardWidget>();
                    if (resultWidget != null) return resultWidget.mrk;
                }
                catch { }
                transform = transform.parent;
            }
            return 0;
        }
    }
}
