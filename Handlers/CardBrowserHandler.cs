using Il2CppYgomSystem.UI;

namespace BlindDuel
{
    public class CardBrowserHandler : IMenuHandler
    {
        public bool CanHandle(string viewControllerName) =>
            viewControllerName is "CardBrowser";

        public bool OnScreenEntered(string viewControllerName) => false;

        public string OnButtonFocused(SelectionButton button) => null;
    }
}
