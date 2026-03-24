using System;
using System.Collections.Generic;
using UnityEngine;
using Il2CppYgomSystem.UI;
using Il2CppYgomSystem.ElementSystem;

namespace BlindDuel
{
    /// <summary>
    /// Dialog detection: patched dialogs are handled immediately via AnnounceDialog(),
    /// while this class polls for unpatched dialog types as a fallback.
    /// </summary>
    public static class DialogDetector
    {
        /// <summary>
        /// Called each frame. Scans DialogManager children for active dialogs
        /// that weren't caught by OnCreatedView patches.
        /// </summary>
        public static void Poll()
        {
            try
            {
                var dialogManager = GameObject.Find("UI/OverlayCanvas/DialogManager");
                if (dialogManager == null) return;

                bool foundActive = false;

                for (int i = 0; i < dialogManager.transform.childCount; i++)
                {
                    var dialogRoot = dialogManager.transform.GetChild(i);
                    if (!dialogRoot.gameObject.activeInHierarchy) continue;

                    for (int j = 0; j < dialogRoot.childCount; j++)
                    {
                        var dialogUI = dialogRoot.GetChild(j);
                        if (!dialogUI.gameObject.activeInHierarchy) continue;
                        if (!dialogUI.name.Contains("(Clone)")) continue;

                        foundActive = true;
                        string dialogKey = dialogUI.name.EndsWith("(Clone)") ? dialogUI.name[..^7] : dialogUI.name;
                        if (dialogKey == NavigationState.LastDialogTitle) return;

                        // CardBrowserUI is the card detail overlay — CardReader handles it
                        if (dialogKey == "CardBrowserUI") continue;

                        var texts = TextExtractor.ExtractAll(dialogUI.gameObject, new TextSearchOptions
                        {
                            ActiveOnly = true,
                            FilterBanned = false,
                            LogPrefix = $"[Dialog-scan] {dialogUI.name} |"
                        });

                        // Convert to labeled tuples for ExtractTitleAndBody
                        var labeled = new List<(string, string)>();
                        foreach (var r in texts)
                            labeled.Add((r.Path, r.Text));

                        var (title, body) = ElementReader.ExtractTitleAndBody(labeled);

                        if (ElementReader.IsPlaceholder(title))
                        {
                            Log.Write($"[Dialog] Skipping placeholder for {dialogUI.name}");
                            return; // Will retry next frame
                        }

                        if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(body)) return;

                        // Build item list body for reward/item dialogs
                        string itemBody = BuildItemListBody(texts);
                        if (!string.IsNullOrEmpty(itemBody))
                            body = itemBody;

                        NavigationState.LastDialogTitle = dialogKey;
                        string announcement = ElementReader.FormatAnnouncement(title, body);

                        Log.Write($"[Dialog] Detected: {dialogRoot.name}/{dialogUI.name}");
                        NavigationState.DialogJustAnnounced = true;
                        Speech.AnnounceScreen(announcement);
                        return;
                    }
                }

                // Reset when no active dialog exists (dialog truly closed)
                if (!foundActive)
                    NavigationState.LastDialogTitle = "";
            }
            catch (Exception ex) { Log.Write($"[DialogDetector] {ex.Message}"); }
        }

        /// <summary>
        /// Build a body string from ItemNameText/ItemNumText pairs found in dialog scans.
        /// Returns null if no item list pattern is found.
        /// </summary>
        private static string BuildItemListBody(List<TextResult> texts)
        {
            // Collect ItemNameText and ItemNumText entries in order
            var names = new List<string>();
            var nums = new List<string>();

            foreach (var r in texts)
            {
                if (r.Path.EndsWith("ItemNameText"))
                    names.Add(r.Text);
                else if (r.Path.EndsWith("ItemNumText"))
                    nums.Add(r.Text?.TrimStart('×', 'x', 'X', ' ') ?? "");
            }

            if (names.Count == 0) return null;

            var items = new List<string>();
            for (int i = 0; i < names.Count; i++)
            {
                string name = names[i];
                string num = i < nums.Count ? nums[i] : "";
                items.Add(!string.IsNullOrEmpty(num) ? $"{name} x{num}" : name);
            }

            return string.Join(", ", items);
        }

        /// <summary>
        /// Announce a dialog from an OnCreatedView patch.
        /// Called immediately when the dialog VC is created, before auto-focus.
        /// </summary>
        public static void AnnounceDialog(ViewController vc)
        {
            try
            {
                var eom = ScreenDetector.GetView(vc);
                if (eom == null) return;

                var texts = TextExtractor.ExtractAll(eom.gameObject, TextSearchOptions.ActiveFiltered);
                if (texts.Count == 0) return;

                var labeled = new List<(string, string)>();
                foreach (var r in texts)
                    labeled.Add((r.Path, r.Text));

                var (title, body) = ElementReader.ExtractTitleAndBody(labeled);
                if (string.IsNullOrEmpty(title)) return;

                // Mark as announced so Poll() won't re-announce
                // Strip "(Clone)" to match Poll()'s key format
                string eomKey = eom.name;
                if (eomKey.EndsWith("(Clone)"))
                    eomKey = eomKey[..^7];
                NavigationState.LastDialogTitle = eomKey;

                string announcement = ElementReader.FormatAnnouncement(title, body);
                Log.Write($"[Dialog-VC] {vc.name}: title='{title}', body='{body}'");
                NavigationState.DialogJustAnnounced = true;
                Speech.AnnounceScreen(announcement);
            }
            catch (Exception ex) { Log.Write($"[Dialog-VC] Error: {ex.Message}"); }
        }
    }
}
