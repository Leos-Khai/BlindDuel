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

        /// <summary>
        /// True once the first phase change fires (Draw Phase, etc.).
        /// Before this, the duel is still in its opening setup (dealing hands,
        /// showing match tips) and card reading should be suppressed.
        /// </summary>
        public static bool HasPhaseStarted { get; set; }

        /// <summary>
        /// True when the player is navigating a CardSelectionList popup
        /// (Extra Deck summon, material selection, effect targets).
        /// SetDescriptionArea only processes card reads when this is true during duels;
        /// all other field card reading goes through InvokeFocusField.
        /// </summary>
        public static bool InSelectionList { get; set; }

        public static void Clear()
        {
            Cards.Clear();
            IsShowingResult = false;
            HasPhaseStarted = false;
            InSelectionList = false;
        }

        public static CardRoot FindCardAtPosition(UnityEngine.Vector3 position)
        {
            return Cards.Find(c => c.cardLocator.pos == position);
        }
    }
}
