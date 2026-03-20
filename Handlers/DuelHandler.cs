using System;
using Il2CppYgomGame.Duel;
using Il2CppYgomSystem.UI;

namespace BlindDuel
{
    public class DuelHandler : IMenuHandler
    {
        // Pending selection list index to queue after card speech
        private static string _pendingSelectionIndex;

        // Dedup: OnSelected fires 3x during list setup for the same button.
        // Track by button reference so two copies of the same card (different
        // buttons, same cardid) both read, but the 3 setup fires are caught.
        private static SelectionButton _lastSelButton;

        /// <summary>Reset selection dedup when a new list opens.</summary>
        public static void ResetSelectionDedup() => _lastSelButton = null;

        public bool CanHandle(string viewControllerName) =>
            viewControllerName is "DuelClient" or "DuelLive";

        public bool OnScreenEntered(string viewControllerName)
        {
            Speech.AnnounceScreen("Duel");
            return true;
        }

        public string OnButtonFocused(SelectionButton button)
        {
            // Suppress button speech during duel end animation
            if (DuelState.IsShowingResult) return "";
            if (!NavigationState.IsInDuel) return null;

            // Card selection list (graveyard, chain, extra deck summon, etc.)
            // Read card data directly from ListCard.m_CardData — no SetDescriptionArea needed.
            try
            {
                var csl = button.GetComponentInParent<CardSelectionList>();
                if (csl != null)
                {
                    var listCard = button.GetComponent<ListCard>();
                    if (listCard == null)
                        listCard = button.GetComponentInParent<ListCard>();

                    if (listCard != null)
                    {
                        try
                        {
                            var data = listCard.m_CardData;
                            if (data != null && data.cardid > 0)
                            {
                                // Dedup: OnSelected fires 3 times during list setup
                                // (initial focus, priority recalc, delayed settle).
                                // All fire on the same button — skip if same instance.
                                // Two copies of the same card are different buttons,
                                // so both read correctly.
                                if (button == _lastSelButton)
                                    return "";
                                _lastSelButton = button;

                                // Don't reveal cards the player doesn't know about
                                // (opponent's face-down cards in target selection lists)
                                try
                                {
                                    if (!data.isknown)
                                    {
                                        string index = GetSelectionIndex(button);
                                        string msg = index != null ? $"Face-down card, {index}" : "Face-down card";
                                        bool queued = PatchCardSelectionListSetTitle.ConsumeQueuedFlag();
                                        if (queued) Speech.SayQueued(msg);
                                        else Speech.SayItem(msg);
                                        return "";
                                    }
                                }
                                catch { }

                                string index2 = GetSelectionIndex(button);
                                bool queued2 = PatchCardSelectionListSetTitle.ConsumeQueuedFlag();
                                CardReader.SpeakCardFromData(data.cardid, index2, queued: queued2);
                                return "";
                            }
                        }
                        catch (Exception ex) { Log.Write($"[DuelHandler] ListCard read: {ex.Message}"); }
                    }

                    // No ListCard (cancel/confirm button) — fall through to default text
                    return null;
                }
            }
            catch (Exception ex) { Log.Write($"[DuelHandler] CardSelection: {ex.Message}"); }

            string name = button.name;

            // Field anchors — onFocusFieldHandler handles card + zone reading
            if (name.Contains("Anchor_")) return "";

            // Suppress card speech when the button isn't interactable.
            // The game sets interactable=false during animations/transitions
            // and true when the player can actually interact.
            try { if (!button.interactable) return ""; }
            catch { }

            // Hand cards are handled by PatchHandCardSelect (SelectByViewIndex patch).
            // Action buttons (Summon, Set, etc.) — default behavior.
            return null;
        }

        /// <summary>
        /// Consume and return the pending selection index, clearing it.
        /// Called by ReadCardDelayed after card speech to queue index.
        /// </summary>
        public static string ConsumeSelectionIndex()
        {
            string idx = _pendingSelectionIndex;
            _pendingSelectionIndex = null;
            return idx;
        }

        /// <summary>
        /// Get the position of a button among its active siblings with SelectionButton components.
        /// Returns "X of Y" string, or null if it can't be determined.
        /// </summary>
        private static string GetSelectionIndex(SelectionButton button)
        {
            try
            {
                var parent = button.transform.parent;
                if (parent == null) return null;

                int current = -1, total = 0;
                for (int i = 0; i < parent.childCount; i++)
                {
                    var child = parent.GetChild(i);
                    if (!child.gameObject.activeInHierarchy) continue;
                    if (child.GetComponent<SelectionButton>() == null) continue;
                    if (child == button.transform)
                        current = total;
                    total++;
                }

                if (current >= 0 && total > 0)
                    return $"{current + 1} of {total}";
            }
            catch (Exception ex) { Log.Write($"[DuelHandler] SelectionIndex: {ex.Message}"); }
            return null;
        }
    }
}
