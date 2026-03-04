using Il2CppYgomSystem.UI;

namespace BlindDuel
{
    public class CardBrowserHandler : IMenuHandler
    {
        public bool CanHandle(string viewControllerName) =>
            viewControllerName is "CardBrowser";

        public void OnScreenEntered(string viewControllerName) { }

        public string OnButtonFocused(SelectionButton button) => null;
    }
}
