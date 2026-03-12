using System.Collections.Generic;
using Il2CppYgomGame.Duel;

namespace BlindDuel
{
    /// <summary>
    /// Tracks duel-specific state: cards on field, LP, etc.
    /// </summary>
    public static class DuelState
    {
        public static List<CardRoot> Cards { get; } = new();

        /// <summary>
        /// True between DuelEndOperation.Setup and DuelEndMessage.Setup —
        /// the duel result animation is playing and buttons should not speak yet.
        /// </summary>
        public static bool IsShowingResult { get; set; }

        public static void Clear()
        {
            Cards.Clear();
            IsShowingResult = false;
        }

        public static CardRoot FindCardAtPosition(UnityEngine.Vector3 position)
        {
            return Cards.Find(c => c.cardLocator.pos == position);
        }
    }
}
