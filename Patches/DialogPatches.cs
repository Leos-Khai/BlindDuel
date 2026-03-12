using System;
using System.Collections.Generic;
using Il2CppYgomSystem.UI;
using Il2CppYgomSystem.UI.ElementWidget;
using Il2CppYgomGame.Menu;
using Il2CppYgomGame.MDMarkup;
using HarmonyLib;

namespace BlindDuel
{
    [HarmonyPatch(typeof(ActionSheetViewController), "OnCreatedView")]
    class PatchActionSheetCreated
    {
        [HarmonyPostfix]
        static void Postfix(ActionSheetViewController __instance) => DialogDetector.AnnounceDialog(__instance);
    }

    [HarmonyPatch(typeof(CommonDialogViewController), "OnCreatedView")]
    class PatchCommonDialogCreated
    {
        [HarmonyPostfix]
        static void Postfix(CommonDialogViewController __instance) => DialogDetector.AnnounceDialog(__instance);
    }

    [HarmonyPatch(typeof(TitleDataLinkDialogViewController), "OnCreatedView")]
    class PatchTitleDataLinkDialogCreated
    {
        [HarmonyPostfix]
        static void Postfix(TitleDataLinkDialogViewController __instance) => DialogDetector.AnnounceDialog(__instance);
    }

    [HarmonyPatch(typeof(DownloadViewController), "OnCreatedView")]
    class PatchDownloadViewControllerCreated
    {
        [HarmonyPostfix]
        static void Postfix(DownloadViewController __instance)
        {
            ScreenDetector.TrackDownload(__instance);
            string title = ScreenDetector.FindScreenTitle(__instance);
            if (!string.IsNullOrEmpty(title))
                Speech.AnnounceScreen(title);
            Log.Write("[Download] DownloadViewController created, tracking progress");
        }
    }

    [HarmonyPatch(typeof(InputFieldWidget), nameof(InputFieldWidget.ActivateInputField))]
    class PatchInputFieldActivate
    {
        [HarmonyPrefix]
        static void Prefix(InputFieldWidget __instance, out bool __state)
        {
            __state = __instance.m_inputFieldActivated;
        }

        [HarmonyPostfix]
        static void Postfix(InputFieldWidget __instance, bool __state)
        {
            try
            {
                // Only announce on false → true transition of m_inputFieldActivated.
                // During setup, ActivateInputField is called but m_inputFieldActivated
                // stays false (preconditions not met), so this won't fire.
                if (__state || !__instance.m_inputFieldActivated) return;

                PatchInputFieldValueChanged.IsEditing = true;
                PatchInputFieldValueChanged.PreviousText = __instance.text ?? "";

                string label = null;
                try
                {
                    var placeholder = __instance.inputField?.placeholder;
                    if (placeholder != null)
                        label = TextExtractor.ExtractFirst(placeholder.gameObject);
                }
                catch { }

                string announcement = !string.IsNullOrEmpty(label) ? label : "Text input";
                string currentText = __instance.text;
                if (!string.IsNullOrEmpty(currentText))
                    announcement += $": {currentText}";
                announcement += ", editing";

                Speech.SayImmediate(announcement);
            }
            catch (Exception ex) { Log.Write($"[InputField] ActivateInputField: {ex.Message}"); }
        }
    }

    [HarmonyPatch(typeof(InputFieldWidget), nameof(InputFieldWidget.OnValueChanged))]
    class PatchInputFieldValueChanged
    {
        internal static string PreviousText = "";
        internal static bool IsEditing;

        [HarmonyPostfix]
        static void Postfix(InputFieldWidget __instance, string input)
        {
            try
            {
                // Guard: skip text changes when the field isn't in active editing mode.
                if (!__instance.m_inputFieldActivated) return;
                if (!IsEditing) return;

                string prev = PreviousText;
                PreviousText = input ?? "";

                if (string.IsNullOrEmpty(input))
                {
                    Speech.SayTyping("no text");
                    return;
                }

                if (string.IsNullOrEmpty(prev))
                {
                    Speech.SayTyping(input[input.Length - 1].ToString());
                    return;
                }

                if (input.Length == prev.Length + 1)
                {
                    for (int i = 0; i < prev.Length; i++)
                    {
                        if (input[i] != prev[i])
                        {
                            Speech.SayTyping(input[i].ToString());
                            return;
                        }
                    }
                    Speech.SayTyping(input[input.Length - 1].ToString());
                }
                else if (input.Length == prev.Length - 1)
                {
                    for (int i = 0; i < input.Length; i++)
                    {
                        if (input[i] != prev[i])
                        {
                            Speech.SayTyping(prev[i].ToString());
                            return;
                        }
                    }
                    Speech.SayTyping(prev[prev.Length - 1].ToString());
                }
                else if (input.Length > prev.Length)
                {
                    Speech.SayTyping(input);
                }
                else if (input.Length < prev.Length)
                {
                    Speech.SayTyping("no text");
                }
            }
            catch (Exception ex) { Log.Write($"[InputField] OnValueChanged: {ex.Message}"); }
        }
    }

    [HarmonyPatch(typeof(InputFieldWidget), nameof(InputFieldWidget.OnEndEdit))]
    class PatchInputFieldEndEdit
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            if (!PatchInputFieldValueChanged.IsEditing) return;
            PatchInputFieldValueChanged.IsEditing = false;
            PatchInputFieldValueChanged.PreviousText = "";
            Speech.SayImmediate("confirmed");
        }
    }

    [HarmonyPatch(typeof(MDMarkupBoardContainerWidget), nameof(MDMarkupBoardContainerWidget.OnStart))]
    class PatchMDMarkupBoardOnStart
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            // Debounce: multiple OnStart calls fire for cached pages.
            // Only the last one (after all settle) triggers the actual read.
            BlindDuelCore.Instance.CancelInvoke(nameof(BlindDuelCore.ReadMDMarkupContent));
            BlindDuelCore.Instance.Invoke(nameof(BlindDuelCore.ReadMDMarkupContent), 0.3f);
        }
    }
}
