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
            SoloState.CaptureGateOverviews(gateManager, "InitData");
        }
    }

    [HarmonyPatch(typeof(SoloGateUtil), nameof(SoloGateUtil.UpdateData))]
    class PatchSoloGateUpdateData
    {
        [HarmonyPostfix]
        static void Postfix(SoloGateUtil.GateManager gateManager)
        {
            SoloState.CaptureGateOverviews(gateManager, "UpdateData");
        }
    }
}
