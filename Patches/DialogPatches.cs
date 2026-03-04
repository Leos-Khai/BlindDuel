using Il2CppYgomSystem.UI;
using Il2CppYgomGame.Menu;
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
                Speech.SayScreenHeader(title);
            Log.Write("[Download] DownloadViewController created, tracking progress");
        }
    }
}
