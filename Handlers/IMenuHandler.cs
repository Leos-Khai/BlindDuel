using Il2CppYgomSystem.UI;

namespace BlindDuel
{
    /// <summary>
    /// Interface for per-menu speech handlers.
    /// Implement this to add speech support for a new screen.
    /// The HandlerRegistry auto-discovers all implementations at startup.
    /// </summary>
    public interface IMenuHandler
    {
        /// <summary>
        /// Returns true if this handler can handle the given ViewController name.
        /// </summary>
        bool CanHandle(string viewControllerName);

        /// <summary>
        /// Called when the screen this handler manages becomes focused.
        /// Return true to skip the default screen announcement.
        /// </summary>
        bool OnScreenEntered(string viewControllerName);

        /// <summary>
        /// Called when a button is focused/selected on a screen this handler manages.
        /// Return the text to speak, or null to fall back to default behavior.
        /// </summary>
        string OnButtonFocused(SelectionButton button);
    }
}
