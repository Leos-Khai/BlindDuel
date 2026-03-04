using System;
using System.Collections.Generic;
using Il2CppYgomGame.Solo;

namespace BlindDuel
{
    /// <summary>
    /// Tracks solo mode gate overview data captured from game events.
    /// </summary>
    public static class SoloState
    {
        public static Dictionary<string, string> GateOverviews { get; } = new();

        public static void CaptureGateOverviews(SoloGateUtil.GateManager gateManager, string source)
        {
            try
            {
                if (gateManager?.masterDataDic == null)
                {
                    Log.Write($"[SoloGate] {source}: masterDataDic is null");
                    return;
                }

                int count = 0;
                foreach (var entry in gateManager.masterDataDic)
                {
                    var data = entry.Value;
                    string name = data.StrName;
                    string overview = data.strOverview;
                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(overview))
                    {
                        GateOverviews[name] = overview;
                        count++;
                    }
                }
                Log.Write($"[SoloGate] {source}: captured {count} gate overviews");
            }
            catch (Exception ex) { Log.Write($"[SoloGate] {source} error: {ex.Message}"); }
        }
    }
}
