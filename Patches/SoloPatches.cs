using System;
using Il2CppYgomGame.Solo;
using HarmonyLib;

namespace BlindDuel
{
    [HarmonyPatch(typeof(SoloGateUtil), nameof(SoloGateUtil.InitGateManagerData))]
    class PatchSoloGateInitData
    {
        [HarmonyPostfix]
        static void Postfix(SoloGateUtil.GateManager gateManager)
        {
            SoloState.CaptureGateData(gateManager, "InitData");
        }
    }

    [HarmonyPatch(typeof(SoloGateUtil), nameof(SoloGateUtil.UpdateData))]
    class PatchSoloGateUpdateData
    {
        [HarmonyPostfix]
        static void Postfix(SoloGateUtil.GateManager gateManager)
        {
            SoloState.CaptureGateData(gateManager, "UpdateData");
        }
    }

    /// <summary>
    /// When the Solo chapter access dialog (Play/Skip/Deck overlay) closes,
    /// re-announce the focused chapter node since it's the same VC — no screen change fires.
    /// </summary>
    [HarmonyPatch(typeof(SoloSelectChapterViewController.AccessDialogManager),
        nameof(SoloSelectChapterViewController.AccessDialogManager.CloseAccessDialog))]
    class PatchSoloAccessDialogClose
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            try { ScreenDetector.QueueFocusedItem(); }
            catch (Exception ex) { Log.Write($"[SoloDialogClose] {ex.Message}"); }
        }
    }
}
