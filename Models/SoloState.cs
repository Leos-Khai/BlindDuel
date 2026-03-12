using System;
using System.Collections.Generic;
using Il2CppYgomGame.Solo;

namespace BlindDuel
{
    /// <summary>
    /// Tracks solo mode gate data captured from game events.
    /// Stores full gate info (name, overview, status) keyed by gateID.
    /// </summary>
    public static class SoloState
    {
        public struct GateInfo
        {
            public int GateID;
            public string Name;
            public string Overview;
            public bool IsClear;
            public bool IsComplete;
            public bool IsUnlocked;
        }

        public static Dictionary<int, GateInfo> Gates { get; } = new();
        public static Dictionary<string, int> GateNameToId { get; } = new();

        public static GateInfo? FindGateByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (GateNameToId.TryGetValue(name, out int id) && Gates.TryGetValue(id, out var info))
                return info;
            return null;
        }

        public static void CaptureGateData(SoloGateUtil.GateManager gateManager, string source)
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
                    if (string.IsNullOrEmpty(name)) continue;

                    var info = new GateInfo
                    {
                        GateID = data.gateID,
                        Name = name,
                        Overview = data.strOverview,
                        IsClear = data.IsClear,
                        IsComplete = data.IsComplete,
                        IsUnlocked = data.isUnlocked,
                    };

                    Gates[info.GateID] = info;
                    GateNameToId[name] = info.GateID;
                    count++;
                }
                Log.Write($"[SoloGate] {source}: captured {count} gate overviews");
            }
            catch (Exception ex) { Log.Write($"[SoloGate] {source} error: {ex.Message}"); }
        }
    }
}
