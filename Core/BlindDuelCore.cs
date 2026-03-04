using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Il2CppYgomGame.Duel;
using MelonLoader;

namespace BlindDuel
{
    public class BlindDuelCore : MonoBehaviour
    {
        public static BlindDuelCore Instance { get; private set; }

        // Current preview data for card/item reading
        public static PreviewData Preview { get; } = new();

        public void Awake()
        {
            Instance = this;
            Log.Init();
            ScreenReader.TrySAPI(true);
            ScreenReader.Load();

            HandlerRegistry.Init();

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
            // Duel hotkeys
            if (NavigationState.IsInDuel)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    var duelLPs = FindObjectsOfType<DuelLP>().ToList();
                    var near = duelLPs.Find(e => e.m_IsNear);
                    var far = duelLPs.Find(e => !e.m_IsNear);
                    Speech.Say($"Your life points: {near?.currentLP}\nOpponent's life points: {far?.currentLP}", SpeechPriority.Info);
                }

                if (Input.GetKeyDown(KeyCode.LeftAlt))
                {
                    Preview.Clear();
                    var cardInfo = FindObjectOfType<CardInfo>();
                    if (cardInfo != null)
                    {
                        if (!cardInfo.gameObject.activeInHierarchy)
                            cardInfo.gameObject.SetActive(true);
                        // TODO: Read card info via CardData extraction
                    }
                }
            }

            // Detection runs first so headers/dialogs queue before button text
            DialogDetector.Poll();
            ScreenDetector.Poll();

            // Then flush all queued speech in priority order
            Speech.FlushPending();
        }
    }
}
