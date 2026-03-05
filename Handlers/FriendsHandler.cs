using Il2CppYgomSystem.UI;

namespace BlindDuel
{
    public class FriendsHandler : IMenuHandler
    {
        public bool CanHandle(string viewControllerName) =>
            viewControllerName is "FriendsMenu" or "Friends";

        public bool OnScreenEntered(string viewControllerName) => false;

        public string OnButtonFocused(SelectionButton button)
        {
            if (button.name == "SearchButton")
                return "Add friend button";
            if (button.name == "OpenToggle")
                return TextExtractor.ExtractFirst(button.transform.parent?.gameObject);

            return null;
        }
    }
}
