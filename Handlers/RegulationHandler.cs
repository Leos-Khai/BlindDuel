using System;
using Il2CppYgomGame.Card;
using Il2CppYgomGame.Regulation;
using Il2CppYgomSystem.UI;
using UnityEngine;

namespace BlindDuel
{
    public class RegulationHandler : IMenuHandler
    {
        public bool CanHandle(string viewControllerName) =>
            viewControllerName is "CardListBrowserRegulationFilter"
                              or "CardListBrowserRegulationFilterViewController";

        public bool OnScreenEntered(string viewControllerName)
        {
            string header = ScreenDetector.ReadGameHeaderText();
            if (!string.IsNullOrEmpty(header))
            {
                Speech.AnnounceScreen(header);
                return true;
            }
            return false;
        }

        public string OnButtonFocused(SelectionButton button)
        {
            try
            {
                var focusVC = ScreenDetector.GetFocusVC();
                if (focusVC == null) return null;

                var regVC = focusVC.TryCast<CardListBrowserRegulationFilterViewController>();
                if (regVC == null) return null;

                var widgetDic = regVC.m_CardWidgetDic;
                if (widgetDic == null) return null;

                // Walk up from button to find the CardWidget in the dictionary
                CardListBrowserRegulationFilterViewController.CardWidget cardWidget = null;
                Transform current = button.transform;
                for (int i = 0; i < 5 && current != null; i++)
                {
                    if (widgetDic.TryGetValue(current.gameObject, out var widget))
                    {
                        cardWidget = widget;
                        break;
                    }
                    current = current.parent;
                }

                if (cardWidget == null) return null;

                int mrk = cardWidget.m_Mrk;
                if (mrk <= 0) return null;

                string name = Content.s_instance?.GetName(mrk);
                if (string.IsNullOrEmpty(name)) return null;

                string result = name;

                // Index from the displayed card list
                int total = regVC.m_DisplayCardMrks?.Count ?? 0;
                int idx = cardWidget.m_Idx + 1;
                if (total > 1)
                    result += $"\n{idx} of {total}";

                return result;
            }
            catch (Exception ex)
            {
                Log.Write($"[RegulationHandler] {ex.Message}");
            }
            return null;
        }
    }
}
