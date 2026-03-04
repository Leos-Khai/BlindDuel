using System;
using System.Runtime.InteropServices;
using MelonLoader;

namespace BlindDuel
{
    /// <summary>
    /// Screen reader communication via Tolk library.
    /// Tolk.dll must be in the game's Mods directory.
    /// </summary>
    public static class ScreenReader
    {
        [DllImport("Tolk", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Tolk_Load();

        [DllImport("Tolk", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Tolk_Unload();

        [DllImport("Tolk", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool Tolk_IsLoaded();

        [DllImport("Tolk", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool Tolk_HasSpeech();

        [DllImport("Tolk", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Tolk_TrySAPI(bool trySAPI);

        [DllImport("Tolk", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        private static extern IntPtr Tolk_DetectScreenReader();

        [DllImport("Tolk", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        private static extern bool Tolk_Output([MarshalAs(UnmanagedType.LPWStr)] string text, bool interrupt);

        [DllImport("Tolk", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool Tolk_Silence();

        private static bool _initialized;
        private static string _lastMessage = "";

        public static bool IsAvailable => _initialized;

        /// <summary>
        /// Initialize the screen reader library. Enables SAPI fallback.
        /// </summary>
        public static bool Initialize()
        {
            if (_initialized) return true;

            try
            {
                Tolk_TrySAPI(true);
                Tolk_Load();
                _initialized = Tolk_IsLoaded() && Tolk_HasSpeech();

                if (_initialized)
                {
                    var readerPtr = Tolk_DetectScreenReader();
                    var name = readerPtr != IntPtr.Zero ? Marshal.PtrToStringUni(readerPtr) : "SAPI fallback";
                    MelonLogger.Msg($"Screen reader detected: {name}");
                    Log.Write($"[Init] Screen reader: {name}");
                }
                else
                {
                    MelonLogger.Warning("No screen reader detected and SAPI unavailable");
                    Log.Write("[Init] No screen reader available");
                }

                return _initialized;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to initialize Tolk: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Shut down the screen reader library.
        /// </summary>
        public static void Shutdown()
        {
            if (!_initialized) return;
            try { Tolk_Unload(); }
            catch (Exception ex) { Log.Write($"[ScreenReader] Shutdown error: {ex.Message}"); }
            _initialized = false;
        }

        /// <summary>
        /// Speak text, interrupting any current speech.
        /// Use for screen/menu announcements and navigation.
        /// </summary>
        public static void Say(string text)
        {
            if (string.IsNullOrEmpty(text) || !_initialized) return;
            _lastMessage = text;
            try { Tolk_Output(text, true); }
            catch (Exception ex) { Log.Write($"[ScreenReader] Say error: {ex.Message}"); }
        }

        /// <summary>
        /// Speak text without interrupting current speech.
        /// Queued and spoken after current speech finishes.
        /// Use for menu item details, descriptions, index positions.
        /// </summary>
        public static void SayQueued(string text)
        {
            if (string.IsNullOrEmpty(text) || !_initialized) return;
            _lastMessage = text;
            try { Tolk_Output(text, false); }
            catch (Exception ex) { Log.Write($"[ScreenReader] SayQueued error: {ex.Message}"); }
        }

        /// <summary>
        /// Stop current speech immediately.
        /// </summary>
        public static void Silence()
        {
            if (!_initialized) return;
            try { Tolk_Silence(); }
            catch (Exception ex) { Log.Write($"[ScreenReader] Silence error: {ex.Message}"); }
        }

        /// <summary>
        /// Repeat the last spoken message.
        /// </summary>
        public static void RepeatLast()
        {
            if (!string.IsNullOrEmpty(_lastMessage))
                Say(_lastMessage);
        }
    }
}
