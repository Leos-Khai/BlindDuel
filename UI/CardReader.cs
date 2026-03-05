using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BlindDuel
{
    /// <summary>
    /// Extracts card data from the game's card info UI panels.
    /// Supports multiple contexts: Duel, DeckEdit, DeckBrowser, CardBrowser.
    /// </summary>
    public static class CardReader
    {
        // Card info UI paths for each context — tried in order, first match wins
        private static readonly (string path, Func<bool> condition, string attributePath)[] CardInfoPaths =
        {
            (
                "UI/OverlayCanvas/DialogManager/CardBrowser/CardBrowserUI(Clone)/Scroll View/Viewport/Content/Template(Clone){0}/CardInfoDetail_Browser(Clone)/Root/Window/StatusArea",
                () => CardBrowserState.IsOpen,
                "TitleAreaGroup/TitleArea/IconAttribute"
            ),
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
        /// Read card data from the currently visible card info panel.
        /// Tries each known UI path until one returns results.
        /// </summary>
        public static CardData ReadCurrentCard()
        {
            var card = BlindDuelCore.Preview.Card;

            foreach (var (pathTemplate, condition, attrRelPath) in CardInfoPaths)
            {
                try
                {
                    if (!condition()) continue;

                    string path = pathTemplate;

                    // CardBrowser uses a paged template — substitute current page
                    if (pathTemplate.Contains("{0}"))
                        path = string.Format(pathTemplate, CardBrowserState.CurrentPage % 3);

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
        /// </summary>
        public static void ReadAndSpeak()
        {
            BlindDuelCore.Preview.Clear();
            var card = ReadCurrentCard();
            string formatted = card.Format(
                isDuel: NavigationState.IsInDuel,
                trimAttributes: NavigationState.CurrentMenu == Menu.Deck
            );

            if (!string.IsNullOrEmpty(formatted))
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
