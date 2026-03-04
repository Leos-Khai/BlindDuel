using Il2CppYgomSystem.UI;

namespace BlindDuel
{
    public class ProfileHandler : IMenuHandler
    {
        public bool CanHandle(string viewControllerName) =>
            viewControllerName is "Profile" or "PlayerProfile";

        public void OnScreenEntered(string viewControllerName) { }

        public string OnButtonFocused(SelectionButton button) => null;
    }
}
