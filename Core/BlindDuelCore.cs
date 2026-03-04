using System.Collections.Generic;
using UnityEngine;
using MelonLoader;

namespace BlindDuel
{
    public class BlindDuelCore : MonoBehaviour
    {
        public static BlindDuelCore Instance { get; private set; }

        public void Awake()
        {
            Instance = this;
            Log.Init();
            ScreenReader.TrySAPI(true);
            ScreenReader.Load();

            string sr = ScreenReader.DetectScreenReader();
            MelonLogger.Msg(sr != null
                ? $"Screen reader detected: {sr}"
                : "No screen reader detected, using SAPI fallback");
            Log.Write($"[Init] Screen reader: {sr ?? "SAPI fallback"}");
        }

        public void OnApplicationQuit()
        {
            ScreenReader.Unload();
        }

        public void Update()
        {
            // Detection runs first so headers/dialogs queue before button text
            DialogDetector.Poll();
            ScreenDetector.Poll();

            // Then flush all queued speech in priority order
            Speech.FlushPending();
        }
    }
}
