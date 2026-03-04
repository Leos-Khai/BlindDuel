using Il2CppYgomGame.CardBrowser;
using HarmonyLib;

namespace BlindDuel
{
    [HarmonyPatch(typeof(CardBrowserViewController), nameof(CardBrowserViewController.Start))]
    class PatchBrowserStart
    {
        [HarmonyPostfix]
        static void Postfix(CardBrowserViewController __instance)
        {
            var snapContent = __instance.GetComponentInChildren<Il2CppYgomSystem.UI.SnapContentManager>();
            CardBrowserState.SnapContentManager = snapContent;
        }
    }
}
