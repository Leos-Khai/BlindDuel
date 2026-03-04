using Il2CppYgomSystem.UI;

namespace BlindDuel
{
    public class SetupHandler : IMenuHandler
    {
        public bool CanHandle(string viewControllerName) =>
            viewControllerName is "GameEntryV1" or "GameEntrySequenceV2" or "Enquete";

        public void OnScreenEntered(string viewControllerName) { }

        public string OnButtonFocused(SelectionButton button) => null;
    }
}
