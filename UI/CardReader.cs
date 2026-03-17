using System;
using System.Collections.Generic;
using Il2CppYgomGame.Card;
using UnityEngine;
using UnityEngine.UI;

namespace BlindDuel
{
    /// <summary>
    /// Extracts card data from the game's card database or UI panels.
    /// Prefers native data API (Content) when card MRK is known.
    /// Falls back to UI text extraction for contexts where MRK isn't available.
    /// </summary>
    public static class CardReader
    {
        // UI paths for contexts where we must read from the panel (no MRK available)
        private static readonly (string path, Func<bool> condition, string attributePath)[] UiFallbackPaths =
        {
            (
                "UI/ContentCanvas/ContentManager/DeckEdit/DeckEditUI(Clone)/CardDetail/Root/Window",
                () => GameObject.Find("UI/ContentCanvas/ContentManager/DeckEdit/") != null,
                "TitleArea/PlateTitle/IconAttribute"
            ),
            (
                "UI/ContentCanvas/ContentManager/DeckBrowser/DeckBrowserUI(Clone)/Root/CardDetail/Root/Window",
                () => GameObject.Find("UI/ContentCanvas/ContentManager/DeckBrowser/") != null,
                "TitleArea/AttributeRoot/IconAttribute"
            ),
            (
                "UI/ContentCanvas/ContentManager/DuelClient/CardInfo/CardInfo(Clone)/Root/Window",
                () => true, // fallback — always try
                "TitleArea/AttributeRoot/IconAttribute"
            ),
        };

        // Item preview path (shop items, rewards, etc.)
        private const string ItemPreviewPath = "UI/OverlayCanvas/DialogManager/ItemPreview/ItemPreviewUI(Clone)/Root/RootMainArea/DescArea/RootDesc/";

        private static readonly TextSearchOptions CardTextOptions = new()
        {
            ActiveOnly = true,
            FilterBanned = false,
        };

        /// <summary>
        /// Read card data from the game's native card database using MRK (card ID).
        /// This is the preferred method — always correct, no template recycling issues.
        /// </summary>
        public static CardData ReadCardFromData(int mrk)
        {
            var card = BlindDuelCore.Preview.Card;
            var content = Content.s_instance;
            if (content == null || mrk <= 0) return card;

            card.Name = content.GetName(mrk);

            var attr = content.GetAttr(mrk);
            card.Element = content.GetAttributeText(attr);

            if (attr == Content.Attribute.Magic || attr == Content.Attribute.Trap)
            {
                // Spell/Trap card
                card.SpellType = content.GetIconFullText(mrk);
            }
            else
            {
                // Monster card
                int atk = content.GetAtk(mrk);
                card.Atk = atk >= 0 ? atk.ToString() : "?";

                var frame = content.GetFrame(mrk);
                if (frame == Content.Frame.Link)
                {
                    card.Link = content.GetLinkNum(mrk).ToString();
                    card.LinkArrows = DeckHandler.FormatLinkArrows(content.GetLinkMask(mrk));
                }
                else
                {
                    int def = content.GetDef(mrk);
                    card.Def = def >= 0 ? def.ToString() : "?";
                }

                // Link monsters have no Level or Rank
                if (frame != Content.Frame.Link)
                {
                    int rank = content.GetRank(mrk);
                    int star = content.GetStar(mrk);
                    if (rank > 0)
                        card.Rank = rank.ToString();
                    else if (star > 0)
                        card.Level = star.ToString();
                }

                int scaleL = content.GetScaleL(mrk);
                if (scaleL > 0)
                    card.PendulumScale = scaleL.ToString();

                // Build type line from native data
                var type = content.GetType(mrk);
                var kind = content.GetKind(mrk);
                string typeText = content.GetTypeText(type);
                string kindText = content.GetKindText(kind);
                if (!string.IsNullOrEmpty(typeText) && !string.IsNullOrEmpty(kindText))
                    card.Attributes = $"[{typeText}/{kindText}]";
                else if (!string.IsNullOrEmpty(typeText))
                    card.Attributes = $"[{typeText}]";
                else if (!string.IsNullOrEmpty(kindText))
                    card.Attributes = $"[{kindText}]";
            }

            card.Description = content.GetDesc(mrk);

            Log.Write($"[CardReader] Read from data: {card.Name} (mrk={mrk})");
            return card;
        }

        /// <summary>
        /// Read card data from the currently visible card info panel.
        /// Uses native data for CardBrowser, falls back to UI text for other contexts.
        /// </summary>
        public static CardData ReadCurrentCard()
        {
            var card = BlindDuelCore.Preview.Card;

            // CardBrowser: read directly from game data — no template guessing needed
            if (CardBrowserState.IsOpen)
            {
                try
                {
                    int mrk = CardBrowserState.GetCurrentMrk();
                    if (mrk > 0)
                        return ReadCardFromData(mrk);
                }
                catch (Exception ex)
                {
                    Log.Write($"[CardReader] CardBrowser data read failed: {ex.Message}");
                }
            }

            // Other contexts: read from UI panels
            foreach (var (path, condition, attrRelPath) in UiFallbackPaths)
            {
                try
                {
                    if (!condition()) continue;

                    var root = GameObject.Find(path);
                    if (root == null) continue;

                    var texts = TextExtractor.ExtractAll(root, CardTextOptions);
                    if (texts.Count == 0) continue;

                    MapTextsToCard(card, texts);

                    // Extract element attribute from sprite
                    string fullAttrPath = $"{path}/{attrRelPath}";
                    card.Element = ReadAttributeFromSprite(fullAttrPath);

                    Log.Write($"[CardReader] Read card: {card.Name} from {path}");
                    return card;
                }
                catch (Exception ex)
                {
                    Log.Write($"[CardReader] Error trying path: {ex.Message}");
                }
            }

            Log.Write("[CardReader] No card info panel found");
            return card;
        }

        /// <summary>
        /// Read item preview data (non-card items like shop products).
        /// </summary>
        public static PreviewData ReadItemPreview()
        {
            var preview = BlindDuelCore.Preview;
            try
            {
                var root = GameObject.Find(ItemPreviewPath);
                if (root == null) return preview;

                var texts = TextExtractor.ExtractAll(root, CardTextOptions);
                if (texts.Count == 0) return preview;

                // Name is first text (or combo of first + second-to-last if many elements)
                if (texts.Count > 2)
                    preview.Name = $"{texts[0].Text} - {texts[^2].Text}";
                else if (texts.Count >= 1)
                    preview.Name = texts[^2].Text;

                // Description is always last
                if (texts.Count >= 2)
                    preview.Description = texts[^1].Text;
            }
            catch (Exception ex)
            {
                Log.Write($"[CardReader] ItemPreview error: {ex.Message}");
            }
            return preview;
        }

        /// <summary>
        /// Map extracted text results to CardData fields by matching path keywords.
        /// </summary>
        private static void MapTextsToCard(CardData card, List<TextResult> texts)
        {
            // First text is always the card name
            if (texts.Count > 0)
                card.Name = texts[0].Text;

            foreach (var (path, text) in texts)
            {
                string p = path;
                if (p.Contains("DescriptionValue", StringComparison.OrdinalIgnoreCase))
                    card.Description = text;
                else if (p.Contains("Rank", StringComparison.OrdinalIgnoreCase))
                    card.Rank = text;
                else if (p.Contains("Level", StringComparison.OrdinalIgnoreCase))
                    card.Level = text;
                else if (p.Contains("Atk", StringComparison.OrdinalIgnoreCase))
                    card.Atk = text;
                else if (p.Contains("Def", StringComparison.OrdinalIgnoreCase))
                    card.Def = text;
                else if (p.Contains("Pendulum", StringComparison.OrdinalIgnoreCase))
                    card.PendulumScale = text;
                else if (p.Contains("Link", StringComparison.OrdinalIgnoreCase))
                    card.Link = text;
                else if (p.Contains("DescriptionItem", StringComparison.OrdinalIgnoreCase))
                    card.Attributes = text;
                else if (p.Contains("SpellTrap", StringComparison.OrdinalIgnoreCase))
                    card.SpellType = text;
                else if (p.Contains("CardNum", StringComparison.OrdinalIgnoreCase))
                    card.Owned = text;
            }
        }

        /// <summary>
        /// Extract card attribute (Light, Dark, etc.) from the attribute icon sprite name.
        /// </summary>
        private static string ReadAttributeFromSprite(string gameObjectPath)
        {
            try
            {
                var go = GameObject.Find(gameObjectPath);
                if (go == null) return "";

                var image = go.GetComponent<Image>();
                if (image == null || image.sprite == null) return "";

                return EnumUtil.ParseAttribute(image.sprite.name);
            }
            catch (Exception ex)
            {
                Log.Write($"[CardReader] Sprite read error: {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// Read card and speak it. Used by hotkey and patches.
        /// Returns true if a card was actually spoken.
        /// </summary>
        public static bool ReadAndSpeak(string suffix = null, bool queued = false)
        {
            BlindDuelCore.Preview.Clear();

            CardData card;

            if (NavigationState.IsInDuel)
            {
                // During duels, read card data directly from the game's card database
                // using the MRK captured by the SetDescriptionArea patch.
                // This is more reliable than UI text extraction: correct effects,
                // proper link ratings/arrows, and no template recycling issues.
                int mrk = PatchCardInfoSetDescription.PendingMrk;
                if (mrk > 0)
                    card = ReadCardFromData(mrk);
                else
                    card = ReadCurrentCard();

                // Suppress re-reading the same card instance (e.g. summon/set triggers
                // SetDescriptionArea again). Uses unique instance ID so multiple copies
                // of the same card are handled correctly.
                int uniqueId = PatchCardInfoSetDescription.PendingUniqueId;
                if (uniqueId > 0 && PatchCardInfoSetDescription.CheckAndUpdateDedup(uniqueId))
                {
                    Log.Write($"[CardReader] Suppressed duplicate read: {card.Name} (uid={uniqueId})");
                    return false;
                }
            }
            else
            {
                card = ReadCurrentCard();
            }

            string formatted = card.Format(
                isDuel: NavigationState.IsInDuel,
                trimAttributes: NavigationState.CurrentMenu == Menu.Deck
            );

            if (string.IsNullOrEmpty(formatted)) return false;

            if (!string.IsNullOrEmpty(suffix))
                formatted += suffix;

            if (queued)
                Speech.SayQueued(formatted);
            else
                Speech.SayItem(formatted);
            return true;
        }

        /// <summary>
        /// Read card directly from the database and speak it with an optional zone suffix.
        /// Used by InvokeFocusField for all duel field/hand/zone card reading.
        /// </summary>
        public static void SpeakCardFromData(int mrk, string zone, int? liveAtk = null, int? liveDef = null, bool queued = false, string battlePosition = null)
        {
            BlindDuelCore.Preview.Clear();
            var card = ReadCardFromData(mrk);

            // Override with live stats from Engine (reflects effect modifications)
            if (liveAtk.HasValue && !string.IsNullOrEmpty(card.Atk))
                card.Atk = liveAtk.Value >= 0 ? liveAtk.Value.ToString() : "?";
            if (liveDef.HasValue && !string.IsNullOrEmpty(card.Def))
                card.Def = liveDef.Value >= 0 ? liveDef.Value.ToString() : "?";

            string formatted = card.Format(isDuel: true, trimAttributes: false, battlePosition: battlePosition);
            if (string.IsNullOrEmpty(formatted))
            {
                if (!string.IsNullOrEmpty(zone))
                {
                    if (queued) Speech.SayQueued(zone);
                    else Speech.SayItem(zone);
                }
                return;
            }

            if (!string.IsNullOrEmpty(zone))
                formatted += $", {zone}";

            if (queued)
                Speech.SayQueued(formatted);
            else
                Speech.SayItem(formatted);
        }

        /// <summary>
        /// Read item preview and speak it. For shop/reward contexts.
        /// </summary>
        public static void ReadPreviewAndSpeak()
        {
            BlindDuelCore.Preview.Clear();
            var preview = ReadItemPreview();

            if (!string.IsNullOrEmpty(preview.Name))
            {
                string text = $"Name: {preview.Name}";
                if (!string.IsNullOrEmpty(preview.Description))
                    text += $"\nDescription: {preview.Description}";
                Speech.SayItem(text);
            }
        }
    }
}
