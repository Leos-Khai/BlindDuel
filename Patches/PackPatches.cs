using System;
using System.Collections.Generic;
using Il2CppYgomGame.CardPack.Open.Actor;
using Il2CppYgomGame.Card;
using HarmonyLib;
using UnityEngine.Playables;

namespace BlindDuel
{
    /// <summary>
    /// Tracks which pack cards have been flipped face-up.
    /// Used by CardPackOpenHandler to distinguish face-down vs revealed.
    /// </summary>
    static class PackFlipTracker
    {
        private static readonly HashSet<int> _flippedMrks = new();

        public static bool IsFlipped(int mrk) => mrk > 0 && _flippedMrks.Contains(mrk);

        public static void MarkFlipped(int mrk)
        {
            if (mrk > 0) _flippedMrks.Add(mrk);
        }

        public static void Reset() => _flippedMrks.Clear();
    }

    /// <summary>
    /// Announces card name + rarity when a card finishes its flip animation.
    /// Also tracks flipped state for the handler's face-down detection.
    /// </summary>
    [HarmonyPatch(typeof(CardPackCardActor), nameof(CardPackCardActor.OnEndPlayable))]
    class PatchCardFlipComplete
    {
        [HarmonyPostfix]
        static void Postfix(CardPackCardActor __instance)
        {
            try
            {
                int mrk = __instance._mrk_k__BackingField;
                if (mrk <= 0) return;

                PackFlipTracker.MarkFlipped(mrk);

                var card = CardReader.ReadCardFromData(mrk);
                string name = card?.Name;
                if (string.IsNullOrEmpty(name)) return;

                string result = name;

                try
                {
                    string rarityText = CardCollectionInfo.GetCardRarityText(
                        (int)CardCollectionInfo.GetCardRarity(mrk));
                    if (!string.IsNullOrEmpty(rarityText))
                        result += $", {rarityText}";
                }
                catch { }

                Speech.SayImmediate(result);
            }
            catch (Exception ex) { Log.Write($"[PackFlip] {ex.Message}"); }
        }
    }
}
