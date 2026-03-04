using Il2CppYgomSystem.UI;

namespace BlindDuel
{
    public class TitleHandler : IMenuHandler
    {
        public bool CanHandle(string viewControllerName) => viewControllerName == "Title";

        public void OnScreenEntered(string viewControllerName) { }

        public string OnButtonFocused(SelectionButton button) => null;
    }
}
