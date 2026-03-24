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
        public string Level { get; set; } = "";
        public string Rank { get; set; } = "";
        public string Atk { get; set; } = "";
        public string Def { get; set; } = "";
        public string PendulumScale { get; set; } = "";
        public string Link { get; set; } = "";
        public string LinkArrows { get; set; } = "";
        public string Element { get; set; } = "";
        public string Attributes { get; set; } = "";
        public string SpellType { get; set; } = "";
        public string Owned { get; set; } = "";
        public bool IsInHand { get; set; } = true;
        public GameObject CardObject { get; set; }

        public bool IsMonster => !string.IsNullOrEmpty(Atk);
        public bool IsLink => !string.IsNullOrEmpty(Link);

        public void Clear()
        {
            Name = Description = Level = Rank = Atk = Def = PendulumScale =
                Link = LinkArrows = Element = Attributes = SpellType = Owned = "";
            IsInHand = true;
            CardObject = null;
        }

        /// <summary>
        /// Format card data into a readable speech string.
        /// </summary>
        public string Format(bool isDuel = false, bool trimAttributes = false, string battlePosition = null)
        {
            if (string.IsNullOrWhiteSpace(Name)) return "";

            var parts = new List<string> { Name };

            if (!string.IsNullOrEmpty(Atk)) parts.Add($"Attack: {Atk}");
            if (!string.IsNullOrEmpty(Link))
            {
                string linkText = $"Link rating: {Link}";
                if (!string.IsNullOrEmpty(LinkArrows))
                    linkText += $", {LinkArrows}";
                parts.Add(linkText);
            }
            if (!IsLink && !string.IsNullOrEmpty(Def)) parts.Add($"Defense: {Def}");
            if (!string.IsNullOrEmpty(battlePosition)) parts.Add(battlePosition);
            if (!string.IsNullOrEmpty(Rank)) parts.Add($"Rank: {Rank}");
            if (!string.IsNullOrEmpty(Level)) parts.Add($"Level: {Level}");
            if (!string.IsNullOrEmpty(Element)) parts.Add($"Element: {Element}");
            if (!string.IsNullOrEmpty(PendulumScale)) parts.Add($"Pendulum scale: {PendulumScale}");
            if (!string.IsNullOrEmpty(Attributes))
            {
                string attrs = trimAttributes && Attributes.Length > 2 ? Attributes[1..^1] : Attributes;
                parts.Add($"Attributes: {attrs}");
            }
            if (!IsMonster && !string.IsNullOrEmpty(SpellType)) parts.Add($"Spell type: {SpellType}");
            if (!string.IsNullOrEmpty(Owned)) parts.Add($"Owned: {Owned}");
            if (!string.IsNullOrEmpty(Description)) parts.Add($"Description: {Description}");

            return string.Join("\n", parts);
        }

        /// <summary>
        /// Build structured detail lines for Ctrl+Up/Down navigation in duels.
        /// [0] = Name, [1+] = detail lines. Summary is returned via out param.
        /// </summary>
        public List<string> GetDetailLines(out string summary, string battlePosition = null, string zone = null)
        {
            // Summary line: "Name, Attack Mode, Attack: 2500, Zone"
            // Include the relevant stat for the current mode
            var summaryParts = new List<string>();
            if (!string.IsNullOrEmpty(Name)) summaryParts.Add(Name);
            if (!string.IsNullOrEmpty(battlePosition)) summaryParts.Add(battlePosition);
            if (battlePosition == "Attack Mode" && !string.IsNullOrEmpty(Atk))
                summaryParts.Add(Atk);
            else if (battlePosition == "Defense Mode" && !string.IsNullOrEmpty(Def))
                summaryParts.Add(Def);
            if (!string.IsNullOrEmpty(zone)) summaryParts.Add(zone);
            summary = string.Join(", ", summaryParts);

            // Detail lines: [0]=Name, [1+]=stats/description
            var lines = new List<string>();
            if (!string.IsNullOrEmpty(Name)) lines.Add(Name);

            // Combine ATK and DEF on one line (Link monsters have no DEF)
            if (!string.IsNullOrEmpty(Atk) && !IsLink && !string.IsNullOrEmpty(Def))
                lines.Add($"Attack: {Atk}, Defense: {Def}");
            else if (!string.IsNullOrEmpty(Atk))
                lines.Add($"Attack: {Atk}");

            if (!string.IsNullOrEmpty(Link))
            {
                string linkText = $"Link rating: {Link}";
                if (!string.IsNullOrEmpty(LinkArrows))
                    linkText += $", {LinkArrows}";
                lines.Add(linkText);
            }
            if (!string.IsNullOrEmpty(Rank)) lines.Add($"Rank: {Rank}");
            if (!string.IsNullOrEmpty(Level)) lines.Add($"Level: {Level}");
            if (!string.IsNullOrEmpty(Element)) lines.Add($"Element: {Element}");
            if (!string.IsNullOrEmpty(PendulumScale)) lines.Add($"Pendulum scale: {PendulumScale}");
            if (!string.IsNullOrEmpty(Attributes)) lines.Add($"Attributes: {Attributes}");
            if (!IsMonster && !string.IsNullOrEmpty(SpellType)) lines.Add($"Spell type: {SpellType}");
            if (!string.IsNullOrEmpty(Description)) lines.Add($"Description: {Description}");

            return lines;
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
