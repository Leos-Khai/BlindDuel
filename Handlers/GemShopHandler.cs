using System;
using Il2CppYgomSystem.UI;
using Il2CppYgomGame.GemShop;
using Il2CppYgomGame.Menu;
using UnityEngine;

namespace BlindDuel
{
    public class GemShopHandler : IMenuHandler
    {
        public bool CanHandle(string viewControllerName) => viewControllerName == "GemShop";

        public bool OnScreenEntered(string viewControllerName)
        {
            string header = ScreenDetector.ReadGameHeaderText();
            Speech.AnnounceScreen(header ?? "Gem Shop");
            return true;
        }

        public string OnButtonFocused(SelectionButton button)
        {
            try
            {
                var widget = FindProductWidget(button);
                if (widget == null) return null;

                // Pack name
                string name = widget.productName?.text?.Trim();
                if (string.IsNullOrEmpty(name)) return null;

                string result = name;

                // Paid/Free gem breakdown from item widgets
                try
                {
                    var itemMap = widget.m_ItemWidgetMap;
                    if (itemMap != null)
                    {
                        var enumerator = itemMap.GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            var itemWidget = enumerator.Current.Value;
                            string itemNum = itemWidget?.itemNumText?.text?.Trim();
                            if (!string.IsNullOrEmpty(itemNum))
                                result += $"\n{itemNum}";
                        }
                    }
                }
                catch { }

                // Purchase limit (e.g., "3 times remaining", "One-time opportunity!")
                string limitText = widget.limitCountText?.text?.Trim();
                if (!string.IsNullOrEmpty(limitText))
                    result += $"\n{limitText}";

                // Pop icon label (e.g., "One-time opportunity!")
                try
                {
                    var popRoot = widget.popIconRoot;
                    if (popRoot != null && popRoot.activeInHierarchy)
                    {
                        string popText = widget.popIconLabel?.text?.Trim();
                        if (!string.IsNullOrEmpty(popText))
                            result += $"\n{popText}";
                    }
                }
                catch { }

                // Time limit (e.g., "29 day(s) left")
                try
                {
                    var dateRoot = widget.limitDateRoot;
                    if (dateRoot != null && dateRoot.activeInHierarchy)
                    {
                        string dateText = widget.limitDateText?.text?.Trim();
                        if (!string.IsNullOrEmpty(dateText))
                            result += $"\n{dateText}";
                    }
                }
                catch { }

                // Price
                string price = widget.priceLabel?.text?.Trim();
                if (!string.IsNullOrEmpty(price))
                    result += $"\nPrice: {price}";

                // Index from game data
                int idx = widget.idx + 1; // 0-based → 1-based
                int total = GetProductCount();
                if (total > 1)
                    result += $"\n{idx} of {total}";

                return result;
            }
            catch (Exception ex)
            {
                Log.Write($"[GemShopHandler] {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Find the ProductWidget for a button by looking up the GemShopViewController's entity map.
        /// </summary>
        private static ProductWidget FindProductWidget(SelectionButton button)
        {
            try
            {
                var focusVC = ScreenDetector.GetFocusVC();
                if (focusVC == null) return null;

                var gemShopVC = focusVC.TryCast<GemShopViewController>();
                if (gemShopVC == null) return null;

                var entityMap = gemShopVC.m_EntityWidgetMap;
                if (entityMap == null) return null;

                // Walk up from button to find the mapped GameObject
                Transform current = button.transform;
                for (int i = 0; i < 5 && current != null; i++)
                {
                    ProductWidget widget;
                    if (entityMap.TryGetValue(current.gameObject, out widget))
                        return widget;
                    current = current.parent;
                }
            }
            catch { }
            return null;
        }

        private static int GetProductCount()
        {
            try
            {
                var focusVC = ScreenDetector.GetFocusVC();
                var gemShopVC = focusVC?.TryCast<GemShopViewController>();
                return gemShopVC?.m_ProductContexts?.Count ?? 0;
            }
            catch { return 0; }
        }
    }
}
