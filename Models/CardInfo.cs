using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlindDuel
{
    /// <summary>
    /// Holds extracted card information for speech output.
    /// </summary>
    public class CardData
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Stars { get; set; } = "";
        public string Atk { get; set; } = "";
        public string Def { get; set; } = "";
        public string PendulumScale { get; set; } = "";
        public string Link { get; set; } = "";
        public string Element { get; set; } = "";
        public string Attributes { get; set; } = "";
        public string SpellType { get; set; } = "";
        public string Owned { get; set; } = "";
        public bool IsInHand { get; set; } = true;
        public GameObject CardObject { get; set; }

        public void Clear()
        {
            Name = Description = Stars = Atk = Def = PendulumScale =
                Link = Element = Attributes = SpellType = Owned = "";
            IsInHand = true;
            CardObject = null;
        }

        /// <summary>
        /// Format card data into a readable speech string.
        /// </summary>
        public string Format(bool isDuel = false, bool trimAttributes = false)
        {
            if (string.IsNullOrWhiteSpace(Name)) return "";

            var parts = new List<string> { $"Name: {Name}" };

            if (!string.IsNullOrEmpty(Atk)) parts.Add($"Attack: {Atk}");
            if (!string.IsNullOrEmpty(Link)) parts.Add($"Link level: {Link}");
            if (!string.IsNullOrEmpty(Def)) parts.Add($"Defense: {Def}");
            if (!string.IsNullOrEmpty(Stars)) parts.Add($"Stars: {Stars}");
            if (!string.IsNullOrEmpty(Element)) parts.Add($"Element: {Element}");
            if (!string.IsNullOrEmpty(PendulumScale)) parts.Add($"Pendulum scale: {PendulumScale}");
            if (!string.IsNullOrEmpty(Attributes))
            {
                string attrs = trimAttributes && Attributes.Length > 2 ? Attributes[1..^1] : Attributes;
                parts.Add($"Attributes: {attrs}");
            }
            if (!string.IsNullOrEmpty(SpellType)) parts.Add($"Spell type: {SpellType}");
            if (!string.IsNullOrEmpty(Owned)) parts.Add($"Owned: {Owned}");
            if (!string.IsNullOrEmpty(Description)) parts.Add($"Description: {Description}");

            return string.Join("\n", parts);
        }
    }

    /// <summary>
    /// Holds preview element data (items, shop products, etc.).
    /// </summary>
    public class PreviewData
    {
        public CardData Card { get; set; } = new();
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string TimeLeft { get; set; } = "";
        public string Price { get; set; } = "";

        public void Clear()
        {
            Card.Clear();
            Name = Description = TimeLeft = Price = "";
        }
    }
}
