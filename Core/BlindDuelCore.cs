using System;
using System.Collections.Generic;
using UnityEngine;
using Il2CppYgomGame.Duel;

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
            ScreenReader.Initialize();
            HandlerRegistry.Init();
        }

        public void OnApplicationQuit()
        {
            ScreenReader.Shutdown();
        }

        public void Update()
        {
            // Duel hotkeys
            if (NavigationState.IsInDuel)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    int myLP = DuelClient.GetLP(0);
                    int oppLP = DuelClient.GetLP(1);
                    Speech.SayImmediate($"Your life points: {myLP}\nOpponent's life points: {oppLP}");
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

            // Detection: screen/dialog changes
            DialogDetector.Poll();
            ScreenDetector.Poll();
        }

        /// <summary>
        /// Invokable card reading method — used with MonoBehaviour.Invoke() for delayed reads.
        /// Also handles pending zone announcements from DuelHandler:
        /// - If a card was spoken, zone queues after it (SayQueued)
        /// - If no card (empty zone), zone speaks immediately (SayItem)
        /// </summary>
        public void ReadCardDelayed()
        {
            bool cardSpoken = CardReader.ReadAndSpeak();

            string zone = DuelHandler.ConsumePendingZone();
            if (!string.IsNullOrEmpty(zone))
            {
                if (cardSpoken)
                    Speech.SayQueued(zone);
                else
                    Speech.SayItem(zone);
            }
        }

        /// <summary>
        /// Invokable method for reading MDMarkup article content (notifications, news).
        /// Called via debounced Invoke() from the MDMarkupBoardContainerWidget.OnStart patch.
        /// Reads from the focused VC's hierarchy after content has settled.
        /// </summary>
        public void ReadMDMarkupContent()
        {
            try
            {
                var focusVC = ScreenDetector.GetFocusVC();
                if (focusVC == null)
                {
                    Log.Write("[MDMarkup] No focused VC");
                    return;
                }

                var texts = TextExtractor.ExtractAll(focusVC.gameObject, new TextSearchOptions
                {
                    ActiveOnly = true,
                    FilterBanned = false,
                    ExcludePathContaining = new List<string> { "SnapContent" },
                    LogPrefix = "[MDMarkup]"
                });

                if (texts.Count == 0)
                {
                    Log.Write("[MDMarkup] No text found in focused VC");
                    return;
                }

                var parts = new List<string>();
                foreach (var r in texts)
                    parts.Add(r.Text);

                string announcement = string.Join(". ", parts);
                Log.Write($"[MDMarkup] {announcement}");
                Speech.AnnounceScreen(announcement);
            }
            catch (Exception ex) { Log.Write($"[MDMarkup] Error: {ex.Message}"); }
        }

    }
}
