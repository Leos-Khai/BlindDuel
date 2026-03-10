using System;
using Il2CppYgomGame.Menu;
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

    [HarmonyPatch(typeof(TitleViewController), nameof(TitleViewController.OnClickStart))]
    class PatchTitleOnClickStart
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            try
            {
                var progress = SystemProgress.Instance;
                if (progress?.connectingProgress != null)
                {
                    string text = TextExtractor.ExtractFirst(progress.connectingProgress);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        Speech.SayImmediate(text);
                        return;
                    }
                }

                // Fallback: use localized "Now Loading" text
                string fallback = Il2CppYgomSystem.Utility.TextData.GetText<Il2CppYgomGame.TextIDs.IDS_SYS>(
                    Il2CppYgomGame.TextIDs.IDS_SYS.NOWLOADING);
                if (!string.IsNullOrWhiteSpace(fallback))
                    Speech.SayImmediate(fallback);
            }
            catch (Exception ex) { Log.Write($"[TitleOnClickStart] {ex.Message}"); }
        }
    }
}
