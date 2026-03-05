using Il2CppYgomSystem.UI;

namespace BlindDuel
{
    /// <summary>
    /// Tracks card browser state (SnapContentManager for paging).
    /// </summary>
    public static class CardBrowserState
    {
        public static SnapContentManager SnapContentManager { get; set; }

        public static bool IsOpen => SnapContentManager != null;
        public static int CurrentPage => SnapContentManager?.currentPage ?? 0;
    }
}
