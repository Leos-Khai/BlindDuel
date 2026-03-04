using System;
using System.Collections.Generic;
using MelonLoader;

namespace BlindDuel
{
    public enum SpeechPriority
    {
        Info = 0,
        Button = 1,
        Dialog = 2,
        ScreenHeader = 3
    }

    public static class Speech
    {
        private static readonly List<string> _history = new();
        private static string _lastSpoken = "";
        private static string _lastHeader = "";
        private static DateTime _lastSpeechTime;
        private static readonly TimeSpan _cooldown = TimeSpan.FromSeconds(0.1);

        // Pending messages queued this frame, processed in Update
        private static readonly List<SpeechMessage> _pending = new();

        public static IReadOnlyList<string> History => _history;

        public static void Say(string text, SpeechPriority priority = SpeechPriority.Button)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            text = TextUtil.StripTags(text);
            if (string.IsNullOrWhiteSpace(text)) return;

            _pending.Add(new SpeechMessage(text, priority));
        }

        public static void SayScreenHeader(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            text = TextUtil.StripTags(text);
            if (string.IsNullOrWhiteSpace(text) || text == _lastHeader) return;

            _lastHeader = text;
            _lastSpoken = "";

            Log.Write($"[Header] {text}");
            MelonLogger.Msg($"screen header: {text}");
            ScreenReader.Output(text, interrupt: true);
            _lastSpeechTime = DateTime.Now;

            // Next speech queues instead of interrupting
            _queueNext = true;
        }

        private static bool _queueNext;

        /// <summary>
        /// Called each frame from BlindDuelCore.Update to flush pending messages.
        /// Higher priority messages speak first. Same-frame messages at the same
        /// priority are spoken in order they were queued.
        /// </summary>
        public static void FlushPending()
        {
            if (_pending.Count == 0) return;

            // Sort by priority descending (highest first)
            _pending.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            foreach (var msg in _pending)
            {
                Speak(msg.Text);
            }
            _pending.Clear();
        }

        private static void Speak(string text)
        {
            bool canSpeak = _queueNext || DateTime.Now - _lastSpeechTime >= _cooldown;
            if (!canSpeak) return;

            if (text == _lastSpoken) return;

            Log.Write($"[Speech] {text}");
            MelonLogger.Msg($"text to speak: {text}");

            ScreenReader.Output(text, interrupt: !_queueNext);
            _queueNext = false;

            _history.Add(text);
            _lastSpoken = text;
            _lastSpeechTime = DateTime.Now;
        }

        public static void ResetDedup()
        {
            _lastSpoken = "";
        }

        private readonly record struct SpeechMessage(string Text, SpeechPriority Priority);
    }
}
