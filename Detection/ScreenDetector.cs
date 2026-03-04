using System;
using System.Collections.Generic;
using UnityEngine;
using Il2CppTMPro;
using Il2CppYgomSystem.UI;
using Il2CppYgomSystem.ElementSystem;
using Il2CppYgomGame.Menu;
using Il2CppYgomGame.Enquete;

namespace BlindDuel
{
    /// <summary>
    /// Detects ViewController changes, announces screen headers,
    /// and queues the currently focused button text.
    /// </summary>
    public static class ScreenDetector
    {
        // VC name → Menu mapping for automatic context detection
        private static readonly Dictionary<string, Menu> VCToMenu = new(StringComparer.OrdinalIgnoreCase)
        {
            { "SoloMode", Menu.Solo },
            { "SoloGate", Menu.Solo },
            { "SoloSelectChapter", Menu.Solo },
            { "DuelClient", Menu.Duel },
            { "DuelLive", Menu.Duel },
            { "DeckEdit", Menu.Deck },
            { "DeckBrowser", Menu.Deck },
            { "Shop", Menu.Shop },
            { "SettingMenuViewController", Menu.Settings },
        };

        // Async polling state
        private static bool _pendingEnqueteCheck;
        private static string _lastEnquetePage = "";
        private static DownloadViewController _activeDownloadVC;
        private static int _lastDownloadPercent = -1;

        /// <summary>
        /// Called each frame. Checks for screen changes, polls async screens.
        /// </summary>
        public static void Poll()
        {
            CheckScreenChange();
            CheckEnqueteScreen();
            CheckDownloadProgress();
        }

        /// <summary>
        /// Register a download VC for progress polling.
        /// Called from DialogPatches when DownloadViewController.OnCreatedView fires.
        /// </summary>
        public static void TrackDownload(DownloadViewController vc)
        {
            _activeDownloadVC = vc;
            _lastDownloadPercent = -1;
        }

        internal static ViewController GetFocusVC()
        {
            var contentManager = GameObject.Find("UI/ContentCanvas/ContentManager");
            if (contentManager == null) return null;
            var vcm = contentManager.GetComponent<ViewControllerManager>();
            return vcm?.GetFocusViewController();
        }

        internal static ElementObjectManager GetView(ViewController vc)
        {
            try
            {
                var baseVC = vc.TryCast<BaseMenuViewController>();
                if (baseVC == null)
                {
                    Log.Write($"[GetView] TryCast failed for: {vc.name} ({vc.GetIl2CppType().Name})");
                    return null;
                }
                return baseVC.m_View;
            }
            catch (Exception ex)
            {
                Log.Write($"[GetView] Error for {vc.name}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Find screen title from a ViewController using EOM elements, then fallback scans.
        /// </summary>
        internal static string FindScreenTitle(ViewController vc)
        {
            if (vc == null) return null;

            var eom = GetView(vc);

            // Try EOM serializedElements first (most reliable)
            List<(string label, string text)> elements = null;
            if (eom != null)
            {
                elements = ElementReader.ReadElements(eom, $"[FindTitle] {vc.name} |");

                // Fallback: scan all TMP in EOM hierarchy
                if (elements.Count == 0)
                {
                    var textResults = TextExtractor.ExtractAll(eom.gameObject, new TextSearchOptions
                    {
                        ActiveOnly = false,
                        FilterBanned = false,
                        LogPrefix = $"[FindTitle-fallback] {vc.name} |"
                    });
                    elements = new List<(string, string)>();
                    foreach (var r in textResults)
                        elements.Add((r.Path, r.Text));
                }
            }

            // Last resort: scan VC's own hierarchy
            if (elements == null || elements.Count == 0)
            {
                var textResults = TextExtractor.ExtractAll(vc.gameObject, new TextSearchOptions
                {
                    ActiveOnly = false,
                    FilterBanned = false,
                    LogPrefix = $"[FindTitle-vc] {vc.name} |"
                });
                elements = new List<(string, string)>();
                foreach (var r in textResults)
                    elements.Add((r.Path, r.Text));
            }

            if (elements.Count == 0) return null;

            var (title, message) = ElementReader.ExtractTitleAndBody(elements);

            if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(message))
                return $"{title}. {message}";
            return title ?? message;
        }

        /// <summary>
        /// Read the game's header bar text (the localized screen name).
        /// </summary>
        internal static string ReadGameHeaderText()
        {
            try
            {
                var headerVC = HeaderViewController.instance;
                if (headerVC == null || !headerVC.gameObject.activeInHierarchy) return null;

                var eom = headerVC.ui;
                if (eom == null) return null;

                string labelKey = headerVC.TXT_LABEL;
                if (string.IsNullOrEmpty(labelKey)) return null;

                // Try the labeled element first
                string text = ElementReader.GetElementText(eom, labelKey);
                if (!string.IsNullOrWhiteSpace(text)) return text;

                // Fallback: first active text in the header
                return TextExtractor.ExtractFirst(eom.gameObject, new TextSearchOptions { ActiveOnly = true, FilterBanned = false });
            }
            catch (Exception ex)
            {
                Log.Write($"[HeaderVC] Error: {ex.Message}");
                return null;
            }
        }

        private static void UpdateMenuContext(string cleanName)
        {
            if (VCToMenu.TryGetValue(cleanName, out Menu menu))
            {
                NavigationState.CurrentMenu = menu;
                Log.Write($"[MenuContext] Set to {menu} from VC: {cleanName}");
            }
            else if (NavigationState.CurrentMenu != Menu.None)
            {
                Log.Write($"[MenuContext] Reset from {NavigationState.CurrentMenu} to None for VC: {cleanName}");
                NavigationState.CurrentMenu = Menu.None;
            }
        }

        private static void CheckScreenChange()
        {
            try
            {
                var focusVC = GetFocusVC();
                if (focusVC == null) return;

                string vcName = focusVC.name;
                if (vcName == NavigationState.LastFocusVCName) return;
                NavigationState.LastFocusVCName = vcName;

                string cleanName = vcName.EndsWith("(Clone)") ? vcName[..^7] : vcName;
                UpdateMenuContext(cleanName);

                // Setup screens — skip announcement
                if (cleanName is "GameEntryV1" or "GameEntrySequenceV2") return;

                // Enquete loads async — poll in Update instead
                if (cleanName == "Enquete")
                {
                    _pendingEnqueteCheck = true;
                    _lastEnquetePage = "";
                    return;
                }

                // Title screen — just read version
                if (cleanName == "Title")
                {
                    string version = ElementReader.GetElementText(GetView(focusVC), "CodeVer");
                    if (!string.IsNullOrWhiteSpace(version))
                        Speech.AnnounceScreen(version);
                    return;
                }

                // Standard screen: combine header + title
                string headerText = ReadGameHeaderText();
                string titleText = FindScreenTitle(focusVC);

                string announcement = (headerText, titleText) switch
                {
                    (not null, not null) => $"{headerText}. {titleText}",
                    (not null, _) => headerText,
                    (_, not null) => titleText,
                    _ => null
                };

                if (announcement == null)
                {
                    Log.Write($"[ScreenChange] No text found for: {cleanName}");
                    return;
                }

                Log.Write($"[ScreenChange] {cleanName} | header='{headerText}', title='{titleText}'");
                Speech.AnnounceScreen(announcement);

                // Queue the currently focused button so user knows where they are
                var contentCanvas = GameObject.Find("UI/ContentCanvas");
                QueueFocusedItem(contentCanvas?.gameObject ?? focusVC.gameObject);
            }
            catch (Exception ex) { Log.Write($"[CheckScreenChange] {ex.Message}"); }
        }

        private static void CheckEnqueteScreen()
        {
            if (!_pendingEnqueteCheck) return;

            try
            {
                var focusVC = GetFocusVC();
                if (focusVC == null) return;

                string cleanName = focusVC.name.EndsWith("(Clone)") ? focusVC.name[..^7] : focusVC.name;
                if (cleanName != "Enquete")
                {
                    _pendingEnqueteCheck = false;
                    _lastEnquetePage = "";
                    return;
                }

                var enqueteVC = focusVC.TryCast<EnqueteViewController>();
                if (enqueteVC == null) return;

                var pageTextComp = enqueteVC.m_PageText;
                string pageText = pageTextComp?.text?.Trim();
                if (string.IsNullOrEmpty(pageText)) return; // Still loading

                if (pageText == _lastEnquetePage) return;
                _lastEnquetePage = pageText;

                // Collect question text, filtering out buttons and option controls
                var collected = TextExtractor.ExtractAll(focusVC.gameObject, new TextSearchOptions
                {
                    ActiveOnly = true,
                    FilterBanned = false,
                    ExcludePathContaining = new() { "button", "shortcut", "toggle", "entity", "checkbox" }
                });

                var texts = new List<string>();
                foreach (var r in collected)
                {
                    if (r.Text != pageText)
                        texts.Add(r.Text);
                }

                if (texts.Count == 0) return;

                string announcement = string.Join(". ", texts) + $". {pageText}";
                Speech.AnnounceScreen(announcement);
                QueueFocusedItem(focusVC.gameObject);
            }
            catch (Exception ex) { Log.Write($"[Enquete] Error: {ex.Message}"); }
        }

        private static void CheckDownloadProgress()
        {
            try
            {
                if (_activeDownloadVC == null) return;

                if (_activeDownloadVC.WasCollected || !_activeDownloadVC.gameObject.activeInHierarchy)
                {
                    _activeDownloadVC = null;
                    _lastDownloadPercent = -1;
                    return;
                }

                var controller = _activeDownloadVC.downloadController;
                if (controller == null) return;

                int percent = (int)(controller.TotalProgress * 100f);
                if (percent == _lastDownloadPercent) return;
                _lastDownloadPercent = percent;

                if (percent >= 100)
                {
                    var stateText = _activeDownloadVC.DownloadingStateText;
                    string msg = stateText?.text?.Trim();
                    if (string.IsNullOrEmpty(msg))
                        msg = _activeDownloadVC.DownloadingText?.text?.Trim();

                    Speech.SayQueued(msg ?? $"{percent} percent");
                    _activeDownloadVC = null;
                    _lastDownloadPercent = -1;
                }
                else
                {
                    Speech.SayQueued($"{percent} percent");
                }
            }
            catch (Exception ex) { Log.Write($"[Download] {ex.Message}"); }
        }

        /// <summary>
        /// After a screen header, queue the focused button text so the user knows where they are.
        /// </summary>
        internal static void QueueFocusedItem(GameObject root)
        {
            try
            {
                var buttons = root.GetComponentsInChildren<SelectionButton>();
                if (buttons == null) return;

                foreach (var btn in buttons)
                {
                    if (btn == null || !btn.gameObject.activeInHierarchy) continue;

                    var colorContainer = btn.GetComponentInChildren<ColorContainerGraphic>();
                    if (colorContainer != null && colorContainer.currentStatusMode == ColorContainer.StatusMode.Enter)
                    {
                        string btnText = TextExtractor.ExtractFirst(btn.gameObject);
                        if (!string.IsNullOrWhiteSpace(btnText))
                        {
                            Speech.SayQueued(btnText);
                            var (index, total) = TransformSearch.GetButtonIndex(btn);
                            if (total > 1)
                                Speech.SayIndex(index, total);
                        }
                        return;
                    }
                }
            }
            catch (Exception ex) { Log.Write($"[QueueFocusedItem] {ex.Message}"); }
        }
    }
}
