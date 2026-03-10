using System;
using Il2CppYgomGame.Shop;
using Il2CppYgomSystem.UI;

namespace BlindDuel
{
    public class ShopBuyHandler : IMenuHandler
    {
        public bool CanHandle(string viewControllerName) =>
            viewControllerName == "ShopBuy";

        public bool OnScreenEntered(string viewControllerName)
        {
            try
            {
                var focusVC = ScreenDetector.GetFocusVC();
                var buyVC = focusVC?.TryCast<ShopBuyViewController>();
                if (buyVC == null) return false;

                var ctx = buyVC.m_ProductContext;
                string name = ctx?.productName;

                // Fallback to EOM if product context unavailable
                if (string.IsNullOrEmpty(name))
                {
                    var eom = ScreenDetector.GetView(focusVC);
                    if (eom != null)
                        name = ElementReader.GetElementText(eom, buyVC.k_ELabelProductNameText ?? "ProductNameText");
                }

                string announcement = name ?? "Product Details";

                // Description
                string desc = ctx?.descTextShort;
                if (string.IsNullOrEmpty(desc))
                    desc = ctx?.listDescText;
                if (!string.IsNullOrEmpty(desc))
                    announcement += $"\n{desc}";

                Speech.AnnounceScreen(announcement);
                return true;
            }
            catch (Exception ex)
            {
                Log.Write($"[ShopBuyHandler] {ex.Message}");
                return false;
            }
        }

        public string OnButtonFocused(SelectionButton button)
        {
            // Try to read buy button details (pack selector: "1 Pack", "10 Packs", etc.)
            try
            {
                var focusVC = ScreenDetector.GetFocusVC();
                var buyVC = focusVC?.TryCast<ShopBuyViewController>();
                var buyGroup = buyVC?.m_BuyButtonGroupWidget;
                var buttons = buyGroup?.m_ButtonWidgets;

                if (buttons != null)
                {
                    for (int i = 0; i < buttons.Count; i++)
                    {
                        var bw = buttons[i];
                        if (bw?.button == button)
                        {
                            // Try to cast to BuyButtonWidget for price/num fields
                            var buyBtn = bw.TryCast<BuyButtonGroupWidget.BuyButtonWidget>();
                            if (buyBtn != null)
                            {
                                string num = buyBtn.numText?.text?.Trim();
                                string price = buyBtn.priceText?.text?.Trim();
                                string info = buyBtn.infoText?.text?.Trim();

                                string result = "";
                                if (!string.IsNullOrEmpty(num))
                                    result += num;
                                if (!string.IsNullOrEmpty(info))
                                    result += string.IsNullOrEmpty(result) ? info : $" {info}";
                                if (!string.IsNullOrEmpty(price))
                                    result += $"\nPrice: {price}";

                                result += $"\n{i + 1} of {buttons.Count}";
                                return string.IsNullOrEmpty(result) ? null : result;
                            }

                            // ActionButtonWidget fallback
                            var actionBtn = bw.TryCast<BuyButtonGroupWidget.ActionButtonWidget>();
                            if (actionBtn != null)
                            {
                                string text = actionBtn.actionText?.text?.Trim();
                                if (!string.IsNullOrEmpty(text))
                                    return text;
                            }
                        }
                    }
                }
            }
            catch { }

            return TextExtractor.ExtractFirst(button.gameObject);
        }
    }
}
