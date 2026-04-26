using HarmonyLib;
using Il2CppYgomGame.SubMenu;

namespace BlindDuel
{
    internal static class HomeSubMenuState
    {
        internal static HomeSubMenuViewController CurrentInstance;
        internal static string LastSpokenSection;
    }

    [HarmonyPatch(typeof(HomeSubMenuViewController), "OnCreatedView")]
    class PatchHomeSubMenuCreated
    {
        [HarmonyPostfix]
        static void Postfix(HomeSubMenuViewController __instance)
        {
            HomeSubMenuState.CurrentInstance = __instance;
            HomeSubMenuState.LastSpokenSection = null;
            HandlerRegistry.SetCurrentFromVC("HomeSubMenu");
            Log.Write("[HomeSubMenu] Opened - handler installed");
        }
    }

    [HarmonyPatch(typeof(HomeSubMenuViewController), "OnBack")]
    class PatchHomeSubMenuBack
    {
        [HarmonyPostfix]
        static void Postfix(bool __result)
        {
            if (!__result) return;
            HomeSubMenuState.CurrentInstance = null;
            HandlerRegistry.SetCurrentFromVC("Home");
            Log.Write("[HomeSubMenu] Closed - reverted to Home");
        }
    }
}
