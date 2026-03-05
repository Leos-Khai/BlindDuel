using System;
using Il2CppTMPro;
using Il2CppYgomSystem.UI;
using UnityEngine;

namespace BlindDuel
{
    public class DuelHandler : IMenuHandler
    {
        public bool CanHandle(string viewControllerName) =>
            viewControllerName is "DuelClient" or "DuelLive";

        public bool OnScreenEntered(string viewControllerName) => false;

        public string OnButtonFocused(SelectionButton button)
        {
            if (!NavigationState.IsInDuel) return null;

            // Hand cards and field anchors
            if (button.name.Contains("HandCard") || button.name.Contains("Anchor_"))
            {
                BlindDuelCore.Preview.Card.CardObject = button.gameObject;

                if (button.name.Contains("Anchor_"))
                {
                    BlindDuelCore.Preview.Card.IsInHand = false;

                    try
                    {
                        var cardRoot = DuelState.FindCardAtPosition(button.transform.position);
                        if (cardRoot != null && !cardRoot.isFace && cardRoot.team != 0)
                            return "Opponent's face down card!";
                    }
                    catch (Exception ex) { Log.Write($"[DuelHandler] {ex.Message}"); }
                }
                else
                {
                    BlindDuelCore.Preview.Card.IsInHand = true;
                }
            }

            // Settings overlay during duel
            try
            {
                var ancestor = button.transform.parent?.parent?.parent?.parent?.parent?.parent;
                if (ancestor != null && ancestor.name == "SettingMenuArea")
                {
                    // Delegate to settings handler logic
                    return null; // Let default text handle, settings handler will enhance
                }
            }
            catch { }

            return null;
        }
    }
}
