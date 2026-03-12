using System;
using Il2CppYgomGame.Duel;
using Il2CppYgomSystem.UI;

namespace BlindDuel
{
    public class DuelHandler : IMenuHandler
    {
        // Suppress the first anchor focus after duel start (game auto-focuses during setup)
        private static bool _firstAnchorSuppressed;

        // Pending zone name to speak after card info via SayQueued
        private static string _pendingZone;

        public bool CanHandle(string viewControllerName) =>
            viewControllerName is "DuelClient" or "DuelLive";

        public bool OnScreenEntered(string viewControllerName)
        {
            _firstAnchorSuppressed = false;
            Speech.AnnounceScreen("Duel");
            return true;
        }

        public string OnButtonFocused(SelectionButton button)
        {
            // Suppress button speech during duel end animation (before result message appears)
            if (DuelState.IsShowingResult) return "";
            if (!NavigationState.IsInDuel) return null;

            string name = button.name;

            // Hand cards — card details read via SetDescriptionArea patch
            if (name.Contains("HandCard"))
            {
                _pendingZone = null;
                PatchCardInfoSetDescription.ResetDedup();
                BlindDuelCore.Preview.Card.CardObject = button.gameObject;
                BlindDuelCore.Preview.Card.IsInHand = true;
                return null;
            }

            // Field anchors — zone name speaks after card (or immediately if empty)
            if (name.Contains("Anchor_"))
            {
                // Suppress the first auto-focus during duel setup transition
                if (!_firstAnchorSuppressed)
                {
                    _firstAnchorSuppressed = true;
                    Log.Write($"[DuelHandler] Suppressed initial auto-focus: {name}");
                    return "";
                }

                PatchCardInfoSetDescription.ResetDedup();
                BlindDuelCore.Preview.Card.CardObject = button.gameObject;
                BlindDuelCore.Preview.Card.IsInHand = false;

                bool isOpponent = name.Contains("_Far_");
                string zone = ExtractZonePart(name);
                string zoneName = FormatZoneName(zone, isOpponent);

                // Check if this zone has a card using the game's native API
                int player = isOpponent ? 1 : 0;
                int enginePos = GetEnginePosition(zone);
                bool hasCard = enginePos >= 0 && HasCardAt(player, enginePos);

                if (hasCard)
                {
                    // Card exists — let SetDescriptionArea read it, queue zone name after
                    _pendingZone = zoneName;
                    return ""; // suppress — card speaks via SayItem, zone via SayQueued
                }

                // Empty zone or container zone — speak zone name immediately
                _pendingZone = null;
                return zoneName;
            }

            // Action buttons (Summon, Set, etc.) — do NOT reset card dedup
            return null;
        }

        /// <summary>
        /// Consume and return the pending zone name, clearing it.
        /// Called by ReadCardDelayed after card speech to queue zone announcement.
        /// </summary>
        public static string ConsumePendingZone()
        {
            string zone = _pendingZone;
            _pendingZone = null;
            return zone;
        }

        /// <summary>
        /// Check if a card exists at the given engine position for the player.
        /// </summary>
        private static bool HasCardAt(int player, int locate)
        {
            try
            {
                return Engine.IsThisCardExist(player, locate);
            }
            catch (Exception ex)
            {
                Log.Write($"[DuelHandler] IsThisCardExist failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Map anchor zone string to Engine position constant.
        /// Returns -1 for container zones (Grave, Extra, Deck, Exclude) that don't need card checks.
        /// </summary>
        private static int GetEnginePosition(string zone)
        {
            if (zone.StartsWith("Monster", StringComparison.OrdinalIgnoreCase))
            {
                int idx = ExtractIndex(zone, "Monster");
                return idx switch
                {
                    0 => Engine.PosMonsterLL,
                    1 => Engine.PosMonsterL,
                    2 => Engine.PosMonsterC,
                    3 => Engine.PosMonsterR,
                    4 => Engine.PosMonsterRR,
                    _ => -1
                };
            }

            // Check FieldMagic BEFORE Magic
            if (zone.StartsWith("FieldMagic", StringComparison.OrdinalIgnoreCase))
                return Engine.PosField;

            if (zone.StartsWith("Magic", StringComparison.OrdinalIgnoreCase))
            {
                int idx = ExtractIndex(zone, "Magic");
                return idx switch
                {
                    0 => Engine.PosMagicLL,
                    1 => Engine.PosMagicL,
                    2 => Engine.PosMagicC,
                    3 => Engine.PosMagicR,
                    4 => Engine.PosMagicRR,
                    _ => -1
                };
            }

            if (zone.StartsWith("PendulumL", StringComparison.OrdinalIgnoreCase))
                return Engine.PosPendulumLeft;
            if (zone.StartsWith("PendulumR", StringComparison.OrdinalIgnoreCase))
                return Engine.PosPendulumRight;
            if (zone.StartsWith("ExMonsterL", StringComparison.OrdinalIgnoreCase))
                return Engine.PosExLMonster;
            if (zone.StartsWith("ExMonsterR", StringComparison.OrdinalIgnoreCase))
                return Engine.PosExRMonster;

            // Container zones — no card-at-position check needed
            return -1;
        }

        /// <summary>
        /// Extract the zone part from anchor name (e.g. "Monster2", "FieldMagic", "Grave").
        /// </summary>
        private static string ExtractZonePart(string name)
        {
            int sideStart = name.IndexOf("_Near_", StringComparison.Ordinal);
            if (sideStart < 0)
                sideStart = name.IndexOf("_Far_", StringComparison.Ordinal);
            if (sideStart >= 0)
            {
                int zoneStart = name.IndexOf('_', sideStart + 1) + 1;
                return name[zoneStart..];
            }
            return name;
        }

        /// <summary>
        /// Format zone part into readable zone name with side prefix.
        /// </summary>
        private static string FormatZoneName(string zone, bool isOpponent)
        {
            string side = isOpponent ? "Opponent's " : "";

            if (zone.StartsWith("FieldMagic", StringComparison.OrdinalIgnoreCase))
                return $"{side}Field Spell Zone";
            if (zone.StartsWith("Monster", StringComparison.OrdinalIgnoreCase))
                return $"{side}Monster Zone {ExtractZoneNumber(zone, "Monster")}";
            if (zone.StartsWith("Magic", StringComparison.OrdinalIgnoreCase))
                return $"{side}Spell Trap Zone {ExtractZoneNumber(zone, "Magic")}";
            if (zone.StartsWith("Extra", StringComparison.OrdinalIgnoreCase))
                return $"{side}Extra Deck";
            if (zone.StartsWith("Grave", StringComparison.OrdinalIgnoreCase))
                return $"{side}Graveyard";
            if (zone.StartsWith("Exclude", StringComparison.OrdinalIgnoreCase))
                return $"{side}Banished";
            if (zone.StartsWith("MainDeck", StringComparison.OrdinalIgnoreCase))
                return $"{side}Deck";
            if (zone.StartsWith("PendulumL", StringComparison.OrdinalIgnoreCase))
                return $"{side}Left Pendulum Zone";
            if (zone.StartsWith("PendulumR", StringComparison.OrdinalIgnoreCase))
                return $"{side}Right Pendulum Zone";
            if (zone.StartsWith("ExMonsterR", StringComparison.OrdinalIgnoreCase))
                return $"{side}Extra Monster Zone Right";
            if (zone.StartsWith("ExMonsterL", StringComparison.OrdinalIgnoreCase))
                return $"{side}Extra Monster Zone Left";

            Log.Write($"[DuelHandler] Unknown anchor zone: {zone}");
            return $"{side}{zone}";
        }

        private static string ExtractZoneNumber(string zone, string prefix)
        {
            string suffix = zone[prefix.Length..];
            if (int.TryParse(suffix, out int index))
                return (index + 1).ToString();
            return "";
        }

        private static int ExtractIndex(string zone, string prefix)
        {
            string suffix = zone[prefix.Length..];
            if (int.TryParse(suffix, out int index))
                return index;
            return -1;
        }
    }
}
