using System;
using UnityEngine;
using MelonLoader;
using Il2CppInterop.Runtime.Injection;

[assembly: MelonInfo(typeof(BlindDuel.BlindDuelMod), "BlindDuel", "1.0.0", "RealAmethyst")]
[assembly: MelonGame("Konami Digital Entertainment Co., Ltd.", "masterduel")]

namespace BlindDuel
{
    public class BlindDuelMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<BlindDuelCore>();
                var go = new GameObject("BlindDuelCore");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<BlindDuelCore>();
                go.hideFlags = HideFlags.HideAndDontSave;

                MelonLogger.Msg("BlindDuel loaded!");
            }
            catch (Exception e)
            {
                MelonLogger.Error("Error loading BlindDuel!");
                MelonLogger.Error(e.ToString());
            }
        }
    }
}
