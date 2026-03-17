using System.Collections.Generic;
using Il2CppYgomGame.Duel;
using Il2CppYgomSystem.UI;

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

        /// <summary>
        /// True briefly after a game event message (summon, effect, phase banner, etc.)
        /// fires during a duel. The next field focus or button read consumes this flag
        /// and queues its speech after the message instead of interrupting it.
        /// </summary>
        public static bool MessageJustAnnounced { get; set; }

        /// <summary>
        /// True while a CardSelectionList is being set up (between SetList prefix
        /// and HandleTitle postfix). Buttons/field focus that fire during this
        /// window are deferred instead of spoken, mirroring HasPendingScreen
        /// for normal menus.
        /// </summary>
        public static bool HasPendingSelection { get; set; }

        /// <summary>
        /// Text of the last button that was deferred during a duel.
        /// Used by HandleTitle to re-queue after a dialog interrupts it.
        /// If not consumed by HandleTitle, Update() speaks it on the next frame.
        /// </summary>
        public static string LastQueuedButtonText { get; set; }
        public static int LastQueuedButtonFrame { get; set; }

        /// <summary>
        /// True after CardCommand closes (player selected Summon/Set/etc.).
        /// Suppresses the next field focus so the selection prompt message
        /// speaks first without a brief blip of the auto-focused zone.
        /// Consumed by the next OnFieldFocused or message handler.
        /// </summary>
        public static bool SuppressNextFieldFocus { get; set; }

        /// <summary>
        /// Button deferred during selection setup. Queued after the title speaks.
        /// </summary>
        public static SelectionButton DeferredSelectionButton { get; set; }

        /// <summary>
        /// Field focus deferred during selection setup. Queued after the title speaks.
        /// (player, position, viewIndex) stored as a tuple, null if none deferred.
        /// </summary>
        public static (int player, int position, int viewIndex)? DeferredFieldFocus { get; set; }

        public static void Clear()
        {
            Cards.Clear();
            IsShowingResult = false;
            HasPhaseStarted = false;
            InSelectionList = false;
            MessageJustAnnounced = false;
            HasPendingSelection = false;
            SuppressNextFieldFocus = false;
            LastQueuedButtonText = null;
            DeferredSelectionButton = null;
            DeferredFieldFocus = null;
        }

        public static CardRoot FindCardAtPosition(UnityEngine.Vector3 position)
        {
            return Cards.Find(c => c.cardLocator.pos == position);
        }
    }
}
