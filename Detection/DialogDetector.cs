namespace BlindDuel
{
    /// <summary>
    /// Detects dialog changes via polling (fallback for unpatched dialog types).
    /// Patched dialogs are handled immediately in DialogPatches.
    /// </summary>
    public static class DialogDetector
    {
        /// <summary>
        /// Called each frame from BlindDuelCore.Update().
        /// Scans DialogManager children for unknown dialog types.
        /// </summary>
        public static void Poll()
        {
            // TODO: Phase 2 — implement dialog polling fallback
        }
    }
}
