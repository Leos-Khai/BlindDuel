using System;
using System.IO;
using MelonLoader;

namespace BlindDuel
{
    public static class Log
    {
        private static string _logPath;
        private static readonly object _lock = new();

        public static void Init()
        {
            try
            {
                string modsDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                _logPath = Path.Combine(modsDir, "BlindDuel_debug.log");

                lock (_lock)
                {
                    File.WriteAllText(_logPath, $"[{DateTime.Now:HH:mm:ss.fff}] === BlindDuel Debug Log Started ===\n");
                }
            }
            catch (Exception ex) { MelonLogger.Warning($"[Log.Init] {ex.Message}"); }
        }

        public static void Write(string message)
        {
            if (string.IsNullOrEmpty(_logPath)) return;

            try
            {
                lock (_lock)
                {
                    File.AppendAllText(_logPath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
                }
            }
            catch (Exception ex) { MelonLogger.Warning($"[Log.Write] {ex.Message}"); }
        }
    }
}
