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
            SDLController.Initialize();
            HandlerRegistry.Init();
        }

        public void OnApplicationQuit()
        {
            SDLController.Shutdown();
            ScreenReader.Shutdown();
        }

        public void Update()
        {
            // Poll SDL3 controller state
            SDLController.Update();

            // Duel hotkeys
            if (NavigationState.IsInDuel)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    int myLP = DuelClient.GetLP(0);
                    int oppLP = DuelClient.GetLP(1);
                    Speech.SayImmediate($"Your life points: {myLP}\nOpponent's life points: {oppLP}");
                }

                // Card detail line-by-line navigation (Ctrl+Up/Down or Right Stick Up/Down)
                // Index starts at 0 (Name, already spoken). Down goes to 1+.
                // Up from 0 or Down past last line = silence.
                bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                bool detailDown = ctrlHeld && Input.GetKeyDown(KeyCode.DownArrow);
                bool detailUp = ctrlHeld && Input.GetKeyDown(KeyCode.UpArrow);

                // Right stick on controller via SDL3
                if (!detailDown && !detailUp)
                {
                    if (SDLController.RightStickDownTriggered) detailDown = true;
                    else if (SDLController.RightStickUpTriggered) detailUp = true;
                }

                if (detailDown)
                {
                    var lines = DuelState.CardDetailLines;
                    if (lines != null)
                    {
                        int idx = DuelState.CardDetailIndex + 1;
                        if (idx < lines.Count)
                        {
                            DuelState.CardDetailIndex = idx;
                            Speech.SayImmediate(lines[idx]);
                        }
                    }
                }
                else if (detailUp)
                {
                    var lines = DuelState.CardDetailLines;
                    if (lines != null)
                    {
                        int idx = DuelState.CardDetailIndex - 1;
                        if (idx >= 0)
                        {
                            DuelState.CardDetailIndex = idx;
                            Speech.SayImmediate(lines[idx]);
                        }
                    }
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

            // Duel log selection tracking
            if (DuelState.IsDuelLogOpen)
                DuelLogReader.PollSelection();

            // Speak deferred button text if no dialog consumed it within the same frame.
            // This mirrors the menu system's deferred button processing in Poll().
            if (!string.IsNullOrEmpty(DuelState.LastQueuedButtonText)
                && UnityEngine.Time.frameCount > DuelState.LastQueuedButtonFrame)
            {
                if (DuelState.LastQueuedButtonInterrupt)
                    Speech.SayItem(DuelState.LastQueuedButtonText);
                else
                    Speech.SayQueued(DuelState.LastQueuedButtonText);
                DuelState.LastQueuedButtonText = null;
            }

            // Detection: screen/dialog changes
            DialogDetector.Poll();
            ScreenDetector.Poll();
        }

        /// <summary>
        /// Invokable card reading method — used with MonoBehaviour.Invoke() for delayed reads.
        /// Only used for selection list cards during duels (field/hand/zone reads go
        /// through InvokeFocusField instead). Also used outside duels for deck editor etc.
        /// </summary>
        public void ReadCardDelayed()
        {
            string selIdx = DuelHandler.ConsumeSelectionIndex();
            bool useQueued = PatchCardSelectionListSetTitle.ConsumeQueuedFlag();

            string suffix = !string.IsNullOrEmpty(selIdx) ? $"\n{selIdx}" : null;

            CardReader.ReadAndSpeak(suffix: suffix, queued: useQueued);
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
