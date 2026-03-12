using Il2CppYgomGame.CardBrowser;
using Il2CppYgomSystem.UI;

namespace BlindDuel
{
    /// <summary>
    /// Tracks card browser state (ViewController + SnapContentManager for paging).
    /// </summary>
    public static class CardBrowserState
    {
        public static CardBrowserViewController ViewController { get; set; }
        public static SnapContentManager SnapContentManager { get; set; }

        public static bool IsOpen => SnapContentManager != null;
        public static int CurrentPage => SnapContentManager?.currentPage ?? 0;

        /// <summary>
        /// Get the card MRK (ID) for the currently displayed page.
        /// Uses the ViewController's CardContext list indexed by currentPage.
        /// </summary>
        public static int GetCurrentMrk()
        {
            var contexts = ViewController?.m_CardContexts;
            int page = CurrentPage;
            if (contexts != null && page >= 0 && page < contexts.Count)
                return contexts[page].mrk;
            return 0;
        }
    }
}
