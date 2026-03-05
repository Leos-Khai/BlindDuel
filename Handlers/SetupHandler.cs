using Il2CppYgomSystem.UI;

namespace BlindDuel
{
    public class SetupHandler : IMenuHandler
    {
        public bool CanHandle(string viewControllerName) =>
            viewControllerName is "GameEntryV1" or "GameEntrySequenceV2" or "Enquete";

        public bool OnScreenEntered(string viewControllerName) => false;

        public string OnButtonFocused(SelectionButton button) => null;
    }
}
