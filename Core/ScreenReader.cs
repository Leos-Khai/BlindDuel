using System;
using System.Runtime.InteropServices;

namespace BlindDuel
{
    public static class ScreenReader
    {
        [DllImport("Tolk", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern void Tolk_Load();

        [DllImport("Tolk", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern void Tolk_Unload();

        [DllImport("Tolk", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern void Tolk_TrySAPI(bool trySAPI);

        [DllImport("Tolk", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr Tolk_DetectScreenReader();

        [DllImport("Tolk", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool Tolk_Output([MarshalAs(UnmanagedType.LPWStr)] string str, bool interrupt);

        public static void Load() => Tolk_Load();
        public static void Unload() => Tolk_Unload();
        public static void TrySAPI(bool trySAPI) => Tolk_TrySAPI(trySAPI);

        public static string DetectScreenReader()
        {
            IntPtr ptr = Tolk_DetectScreenReader();
            return ptr != IntPtr.Zero ? Marshal.PtrToStringUni(ptr) : null;
        }

        public static bool Output(string text, bool interrupt = false) => Tolk_Output(text, interrupt);
    }
}
