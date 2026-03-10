using System;
using System.Collections.Generic;
using Il2CppYgomSystem.UI;
using Il2CppYgomGame.Menu;
using Il2CppYgomGame.MDMarkup;
using HarmonyLib;

namespace BlindDuel
{
    [HarmonyPatch(typeof(ActionSheetViewController), "OnCreatedView")]
    class PatchActionSheetCreated
    {
        [HarmonyPostfix]
        static void Postfix(ActionSheetViewController __instance) => DialogDetector.AnnounceDialog(__instance);
    }

    [HarmonyPatch(typeof(CommonDialogViewController), "OnCreatedView")]
    class PatchCommonDialogCreated
    {
        [HarmonyPostfix]
        static void Postfix(CommonDialogViewController __instance) => DialogDetector.AnnounceDialog(__instance);
    }

    [HarmonyPatch(typeof(TitleDataLinkDialogViewController), "OnCreatedView")]
    class PatchTitleDataLinkDialogCreated
    {
        [HarmonyPostfix]
        static void Postfix(TitleDataLinkDialogViewController __instance) => DialogDetector.AnnounceDialog(__instance);
    }

    [HarmonyPatch(typeof(DownloadViewController), "OnCreatedView")]
    class PatchDownloadViewControllerCreated
    {
        [HarmonyPostfix]
        static void Postfix(DownloadViewController __instance)
        {
            ScreenDetector.TrackDownload(__instance);
            string title = ScreenDetector.FindScreenTitle(__instance);
            if (!string.IsNullOrEmpty(title))
                Speech.AnnounceScreen(title);
            Log.Write("[Download] DownloadViewController created, tracking progress");
        }
    }

    [HarmonyPatch(typeof(MDMarkupBoardContainerWidget), nameof(MDMarkupBoardContainerWidget.OnStart))]
    class PatchMDMarkupBoardOnStart
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            // Debounce: multiple OnStart calls fire for cached pages.
            // Only the last one (after all settle) triggers the actual read.
            BlindDuelCore.Instance.CancelInvoke(nameof(BlindDuelCore.ReadMDMarkupContent));
            BlindDuelCore.Instance.Invoke(nameof(BlindDuelCore.ReadMDMarkupContent), 0.3f);
        }
    }
}
