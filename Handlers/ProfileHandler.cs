using Il2CppYgomSystem.UI;

namespace BlindDuel
{
    public class ProfileHandler : IMenuHandler
    {
        public bool CanHandle(string viewControllerName) =>
            viewControllerName is "Profile" or "PlayerProfile";

        public bool OnScreenEntered(string viewControllerName) => false;

        public string OnButtonFocused(SelectionButton button) => null;
    }
}
