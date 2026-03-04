using System;
using Il2CppYgomSystem.UI;
using HarmonyLib;

namespace BlindDuel
{
    [HarmonyPatch(typeof(ViewController), nameof(ViewController.OnBack))]
    class PatchOnBack
    {
        [HarmonyPostfix]
        public static void Postfix(ViewController __instance)
        {
            try
            {
                if (__instance.manager == null) return;
                var focusVC = __instance.manager.GetFocusViewController();
                if (focusVC?.name == "Home")
                    NavigationState.CurrentMenu = Menu.None;
            }
            catch (Exception ex) { Log.Write($"[OnBack] {ex.Message}"); }
        }
    }
}
