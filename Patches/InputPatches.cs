using System;
using System.Collections.Generic;
using UnityEngine;
using Il2CppYgomSystem;
using HarmonyLib;

namespace BlindDuel
{
    /// <summary>
    /// Injects keyboard input into the game's gamepad system.
    /// Arrow keys → d-pad, Enter → confirm, Backspace → cancel.
    /// Ctrl+Up/Down is suppressed during duels (handled by BlindDuelCore instead).
    /// </summary>
    [HarmonyPatch(typeof(GamePad_PC), nameof(GamePad_PC.GetKeyDown))]
    class PatchGamePadGetKeyDown
    {
        private static Dictionary<int, KeyCode> _map;

        static void Postfix(int Type, ref bool __result)
        {
            // Suppress right stick up/down from game during duels (SDL3 reads it directly)
            if (__result && InputMap.IsRightStickDuelSuppress(Type)) { __result = false; return; }

            if (__result) return;
            if (!Application.isFocused) return;

            var map = GetMap();
            if (map == null) return;

            if (!map.TryGetValue(Type, out var keyCode)) return;

            // Suppress Up/Down when Ctrl is held during a duel (mod handles those)
            if (IsCtrlDuelSuppress(Type)) return;

            __result = Input.GetKeyDown(keyCode);
        }

        static Dictionary<int, KeyCode> GetMap() => _map ??= InputMap.Build();
        static bool IsCtrlDuelSuppress(int type) => InputMap.IsCtrlDuelSuppress(type);
    }

    [HarmonyPatch(typeof(GamePad_PC), nameof(GamePad_PC.GetKey))]
    class PatchGamePadGetKey
    {
        private static Dictionary<int, KeyCode> _map;

        static void Postfix(int Type, ref bool __result)
        {
            if (__result && InputMap.IsRightStickDuelSuppress(Type)) { __result = false; return; }

            if (__result) return;
            if (!Application.isFocused) return;

            var map = GetMap();
            if (map == null) return;

            if (!map.TryGetValue(Type, out var keyCode)) return;

            if (IsCtrlDuelSuppress(Type)) return;

            __result = Input.GetKey(keyCode);
        }

        static Dictionary<int, KeyCode> GetMap() => _map ??= InputMap.Build();
        static bool IsCtrlDuelSuppress(int type) => InputMap.IsCtrlDuelSuppress(type);
    }

    /// <summary>
    /// Shared mapping logic for keyboard → gamepad button injection.
    /// </summary>
    static class InputMap
    {
        private static int _btnUp, _btnDown, _btnLeft, _btnRight, _btnDecision, _btnCancel;
        private static int _btnRUp, _btnRDown;

        public static Dictionary<int, KeyCode> Build()
        {
            try
            {
                _btnUp = GamePad.BUTTON_UP;
                _btnDown = GamePad.BUTTON_DOWN;
                _btnLeft = GamePad.BUTTON_LEFT;
                _btnRight = GamePad.BUTTON_RIGHT;
                _btnDecision = GamePad.BUTTON_FUNC_DECISION;
                _btnCancel = GamePad.BUTTON_FUNC_CANCEL;
                _btnRUp = GamePad.BUTTON_RUP;
                _btnRDown = GamePad.BUTTON_RDOWN;

                return new Dictionary<int, KeyCode>
                {
                    { _btnUp, KeyCode.UpArrow },
                    { _btnDown, KeyCode.DownArrow },
                    { _btnLeft, KeyCode.LeftArrow },
                    { _btnRight, KeyCode.RightArrow },
                    { _btnDecision, KeyCode.Return },
                    { _btnCancel, KeyCode.Backspace },
                };
            }
            catch (Exception ex)
            {
                Log.Write($"[InputMap] Failed to read GamePad button constants: {ex.Message}");
                return null;
            }
        }

        public static bool IsCtrlDuelSuppress(int type)
        {
            if (!NavigationState.IsInDuel) return false;
            if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl)) return false;
            return type == _btnUp || type == _btnDown;
        }

        /// <summary>
        /// Suppress right stick up/down from game navigation during duels
        /// (used for card detail line-by-line reading instead).
        /// </summary>
        public static bool IsRightStickDuelSuppress(int type)
        {
            if (!NavigationState.IsInDuel) return false;
            return type == _btnRUp || type == _btnRDown;
        }

    }
}
