using System.Collections.Generic;

namespace BlindDuel
{
    /// <summary>
    /// Tracks current navigation context: which screen is focused, current menu, last dialog.
    /// </summary>
    public static class NavigationState
    {
        public static Menu CurrentMenu { get; set; } = Menu.None;
        public static string LastFocusVCName { get; set; } = "";
        public static string LastDialogTitle { get; set; } = "";
        public static bool IsInDuel { get; set; }
        public static bool DialogJustAnnounced { get; set; }

        public static readonly Dictionary<string, Menu> VCNameToMenu = new()
        {
            { "DuelMenu", Menu.Duel },
            { "DeckMenu", Menu.Deck },
            { "SoloMenu", Menu.Solo },
            { "ShopMenu", Menu.Shop },
            { "MissionMenu", Menu.Missions },
            { "NotificationMenu", Menu.Notifications },
            { "GameSettingMenu", Menu.Settings },
            { "DuelPassMenu", Menu.DuelPass }
        };

        public static readonly Dictionary<string, string> ScreenHeaderToMenu = new()
        {
            { "DUEL", "Duel" },
            { "DECK", "Deck" },
            { "SOLO", "Solo" },
            { "SHOP", "Shop" },
            { "MISSION", "Missions" },
            { "Notifications", "Notifications" },
            { "Game Settings", "Settings" },
            { "Duel Pass", "Duel Pass" }
        };
    }
}
