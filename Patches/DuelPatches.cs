using System;
using Il2CppYgomGame.Duel;
using Il2CppYgomGame.MDMarkup;
using Il2CppYgomGame.Tutorial;
using HarmonyLib;

namespace BlindDuel
{
    [HarmonyPatch(typeof(DuelLP), nameof(DuelLP.SetLP))]
    class PatchSetLP
    {
        [HarmonyPostfix]
        static void Postfix(DuelLP __instance, int lp, bool initialSet)
        {
            try
            {
                if (!initialSet) return;
                string who = __instance.m_IsNear ? "Your" : "Opponent's";
                Speech.SayQueued($"{who} starting life points: {lp}");
            }
            catch (Exception ex) { Log.Write($"[PatchSetLP] {ex.Message}"); }
        }
    }

    [HarmonyPatch(typeof(DuelLP), nameof(DuelLP.ChangeLP), MethodType.Normal)]
    class PatchChangeLP
    {
        [HarmonyPostfix]
        static void Postfix(DuelLP __instance, int afterLP, int damage, Engine.DamageType type)
        {
            try
            {
                string who = __instance.m_IsNear ? "Your" : "Opponent's";

                string reason = type switch
                {
                    Engine.DamageType.ByBattle => "battle damage",
                    Engine.DamageType.ByEffect => "effect damage",
                    Engine.DamageType.ByCost => "cost",
                    Engine.DamageType.ByPay => "payment",
                    Engine.DamageType.ByLost => "lost",
                    Engine.DamageType.Recover => "recovery",
                    _ => ""
                };

                if (type == Engine.DamageType.Recover)
                    Speech.SayImmediate($"{who} life points: {afterLP}, gained {damage} from {reason}");
                else if (damage > 0 && reason.Length > 0)
                    Speech.SayImmediate($"{who} life points: {afterLP}, took {damage} {reason}");
                else
                    Speech.SayImmediate($"{who} life points: {afterLP}");

                if (afterLP < 1)
                {
                    NavigationState.IsInDuel = false;
                    DuelState.Clear();
                }
            }
            catch (Exception ex) { Log.Write($"[PatchChangeLP] {ex.Message}"); }
        }
    }

    [HarmonyPatch(typeof(DuelClient), nameof(DuelClient.Awake))]
    class PatchDuelClientAwake
    {
        [HarmonyPostfix]
        static void Postfix(DuelClient __instance)
        {
            NavigationState.CurrentMenu = Menu.Duel;
            NavigationState.IsInDuel = true;
        }
    }

    [HarmonyPatch(typeof(CardRoot), nameof(CardRoot.Initialize), MethodType.Normal)]
    class PatchCardRootInit
    {
        [HarmonyPostfix]
        static void Postfix(CardRoot __instance)
        {
            DuelState.Cards.Add(__instance);
        }
    }

    [HarmonyPatch(typeof(CardInfo), nameof(CardInfo.SetDescriptionArea))]
    class PatchCardInfoSetDescription
    {
        private const float ReadDelay = 0.15f;
        private static string _lastCardName = "";

        [HarmonyPostfix]
        static void Postfix(CardInfo __instance)
        {
            try
            {
                if (!__instance.gameObject.activeInHierarchy) return;

                // Debounce: cancel any pending read and schedule a new one.
                // SetDescriptionArea fires multiple times per card — only the last one reads.
                BlindDuelCore.Instance.CancelInvoke(nameof(BlindDuelCore.ReadCardDelayed));
                BlindDuelCore.Instance.Invoke(nameof(BlindDuelCore.ReadCardDelayed), ReadDelay);
            }
            catch (Exception ex)
            {
                Log.Write($"[PatchCardInfoSetDescription] {ex.Message}");
            }
        }

        /// <summary>
        /// Called by ReadCardDelayed to check and suppress duplicate reads.
        /// Returns true if this card was already just read (suppress speech).
        /// </summary>
        public static bool CheckAndUpdateDedup(string cardName)
        {
            if (string.IsNullOrEmpty(cardName)) return false;
            if (cardName == _lastCardName) return true;
            _lastCardName = cardName;
            return false;
        }

        public static void ResetDedup() => _lastCardName = "";
    }

    // --- Tutorial & Instant Message patches ---

    [HarmonyPatch(typeof(TutorialNavigator), nameof(TutorialNavigator.PlayCenterMsg),
        new Type[] { typeof(Il2CppSystem.Collections.Generic.IList<string>), typeof(UnityEngine.Events.UnityAction), typeof(float) })]
    class PatchTutorialCenterMsg
    {
        private static string _lastMessage = "";

        [HarmonyPostfix]
        static void Postfix(Il2CppSystem.Collections.Generic.IList<string> messages)
        {
            try
            {
                if (messages == null) return;

                var parts = new System.Collections.Generic.List<string>();
                // Il2Cpp IList doesn't expose Count directly — iterate by index with bounds check
                for (int i = 0; ; i++)
                {
                    string msg;
                    try { msg = messages[i]; }
                    catch { break; }
                    if (!string.IsNullOrWhiteSpace(msg))
                        parts.Add(TextUtil.StripTags(msg));
                }

                string combined = string.Join(". ", parts);
                if (string.IsNullOrWhiteSpace(combined) || combined == _lastMessage) return;

                _lastMessage = combined;
                Log.Write($"[TutorialCenter] {combined}");
                Speech.SayQueued(combined);
            }
            catch (Exception ex) { Log.Write($"[PatchTutorialCenterMsg] {ex.Message}"); }
        }
    }

    [HarmonyPatch(typeof(TutorialNavigator), nameof(TutorialNavigator.PlayTopMsg),
        new Type[] { typeof(string), typeof(float) })]
    class PatchTutorialTopMsg
    {
        private static string _lastMessage = "";

        [HarmonyPostfix]
        static void Postfix(string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message)) return;

                string cleaned = TextUtil.StripTags(message);
                if (string.IsNullOrWhiteSpace(cleaned) || cleaned == _lastMessage) return;

                _lastMessage = cleaned;
                Log.Write($"[TutorialTop] {cleaned}");
                Speech.SayQueued(cleaned);
            }
            catch (Exception ex) { Log.Write($"[PatchTutorialTopMsg] {ex.Message}"); }
        }
    }

    [HarmonyPatch(typeof(InstantMessage), nameof(InstantMessage.Open))]
    class PatchInstantMessageOpen
    {
        [HarmonyPostfix]
        static void Postfix(string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message)) return;

                string cleaned = TextUtil.StripTags(message);
                if (string.IsNullOrWhiteSpace(cleaned)) return;
                if (cleaned == InstantMessageDedup.LastMessage) return;
                InstantMessageDedup.LastMessage = cleaned;

                Log.Write($"[InstantMessage] {cleaned}");
                Speech.SayQueued(cleaned);
            }
            catch (Exception ex) { Log.Write($"[PatchInstantMessageOpen] {ex.Message}"); }
        }
    }

    [HarmonyPatch(typeof(InstantMessage), nameof(InstantMessage.ReqOpen))]
    class PatchInstantMessageReqOpen
    {
        [HarmonyPostfix]
        static void Postfix(string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message)) return;

                string cleaned = TextUtil.StripTags(message);
                if (string.IsNullOrWhiteSpace(cleaned)) return;
                if (cleaned == InstantMessageDedup.LastMessage) return;
                InstantMessageDedup.LastMessage = cleaned;

                Log.Write($"[InstantMessageReq] {cleaned}");
                Speech.SayQueued(cleaned);
            }
            catch (Exception ex) { Log.Write($"[PatchInstantMessageReqOpen] {ex.Message}"); }
        }
    }

    /// <summary>
    /// Shared dedup between InstantMessage.Open and InstantMessage.ReqOpen
    /// to prevent the same message speaking twice (ReqOpen queues, Open displays).
    /// </summary>
    static class InstantMessageDedup
    {
        public static string LastMessage = "";
    }

    [HarmonyPatch(typeof(DuelInfoDialogBase), nameof(DuelInfoDialogBase.Open))]
    class PatchDuelInfoDialogOpen
    {
        [HarmonyPostfix]
        static void Postfix(string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message)) return;
                string cleaned = TextUtil.StripTags(message);
                if (string.IsNullOrWhiteSpace(cleaned)) return;

                Log.Write($"[DuelInfoDialog] {cleaned}");
                Speech.SayQueued(cleaned);
            }
            catch (Exception ex) { Log.Write($"[PatchDuelInfoDialog] {ex.Message}"); }
        }
    }

    [HarmonyPatch(typeof(DuelEndOperation), nameof(DuelEndOperation.Setup))]
    class PatchDuelEndOperationSetup
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            try
            {
                // Fires early when duel end sequence begins (before animation).
                // Suppress button speech until DuelEndMessage.Setup populates the result text.
                DuelState.IsShowingResult = true;
                Log.Write("[DuelEndOp] Duel end sequence started, suppressing buttons");
            }
            catch (Exception ex) { Log.Write($"[PatchDuelEndOperationSetup] {ex.Message}"); }
        }
    }

    [HarmonyPatch(typeof(DuelEndMessage), nameof(DuelEndMessage.Setup))]
    class PatchDuelEndMessageSetup
    {
        [HarmonyPostfix]
        static void Postfix(string message, bool winMyself, bool winRival)
        {
            try
            {
                string result = winMyself ? "Victory" : winRival ? "Defeat" : "Draw";

                string cleaned = !string.IsNullOrWhiteSpace(message)
                    ? TextUtil.StripTags(message)
                    : "";

                string announcement = !string.IsNullOrEmpty(cleaned)
                    ? $"{result}. {cleaned}"
                    : result;

                Log.Write($"[DuelEnd] {announcement}");
                Speech.SayImmediate(announcement);

                // Result has been spoken — allow buttons and mark duel as over
                NavigationState.IsInDuel = false;
                DuelState.Clear();
            }
            catch (Exception ex) { Log.Write($"[PatchDuelEndMessageSetup] {ex.Message}"); }
        }
    }

    /// <summary>
    /// Read match tips pages (MDMarkup content shown at the start of solo practice duels).
    /// Only speaks during duels to avoid interfering with menu MDMarkup reading.
    /// </summary>
    [HarmonyPatch(typeof(MDMarkupPageWidgetBase), nameof(MDMarkupPageWidgetBase.BindContentData))]
    class PatchMDMarkupBindContent
    {
        [HarmonyPostfix]
        static void Postfix(MDMarkupPageWidgetBase __instance)
        {
            try
            {
                if (!NavigationState.IsInDuel) return;

                string title = __instance.m_CaptionText?.text;
                string body = __instance.m_Text?.text;

                title = TextUtil.StripTags(title);
                body = TextUtil.StripTags(body);

                if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body)) return;

                string announcement = !string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(body)
                    ? $"{title}. {body}"
                    : title ?? body;

                Log.Write($"[MatchTips] {announcement}");
                Speech.SayQueued(announcement);
            }
            catch (Exception ex) { Log.Write($"[PatchMDMarkupBindContent] {ex.Message}"); }
        }
    }

    [HarmonyPatch(typeof(CardCommand), nameof(CardCommand.Open), new Type[0])]
    class PatchCardCommandOpen
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            try
            {
                if (!NavigationState.IsInDuel) return;
                Log.Write("[CardCommand] Action menu opened — silencing card speech");
                Speech.Silence();
            }
            catch (Exception ex) { Log.Write($"[PatchCardCommandOpen] {ex.Message}"); }
        }
    }

    [HarmonyPatch(typeof(EffectTaskPhaseChange), nameof(EffectTaskPhaseChange.PlayPhaseChangeEffect))]
    class PatchPhaseChange
    {
        private static Engine.Phase _lastPhase = Engine.Phase.Null;

        [HarmonyPostfix]
        static void Postfix(EffectTaskPhaseChange __instance)
        {
            try
            {
                var phase = __instance.phase;
                if (phase == _lastPhase) return;
                _lastPhase = phase;

                string phaseName = phase switch
                {
                    Engine.Phase.Draw => "Draw Phase",
                    Engine.Phase.Standby => "Standby Phase",
                    Engine.Phase.Main1 => "Main Phase 1",
                    Engine.Phase.Battle => "Battle Phase",
                    Engine.Phase.Main2 => "Main Phase 2",
                    Engine.Phase.End => "End Phase",
                    _ => ""
                };

                if (string.IsNullOrEmpty(phaseName)) return;

                Log.Write($"[PhaseChange] {phaseName}");
                Speech.SayImmediate(phaseName);
            }
            catch (Exception ex) { Log.Write($"[PatchPhaseChange] {ex.Message}"); }
        }
    }
}
