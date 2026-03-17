using System;
using System.Collections.Generic;
using System.Reflection;
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

            // Subscribe to the game's native field focus event.
            // This fires when the duel cursor moves to any card/zone.
            try
            {
                FieldFocusHandler.Subscribe(__instance);
            }
            catch (Exception ex) { Log.Write($"[DuelClientAwake] Focus subscribe failed: {ex.Message}"); }
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

    /// <summary>
    /// Handles field/hand/zone card reading via the game's native focus system.
    /// Subscribes to DuelClient.onFocusFieldHandler (delegate subscription, not Harmony patch,
    /// because InvokeFocusField is called from native code and bypasses managed wrappers).
    /// Fires when the duel cursor moves to any position (monsters, spells, hand,
    /// graveyard, extra deck, banished, etc.). Replaces SetDescriptionArea for
    /// all duel field card reading — no more spam from animations or summons.
    /// </summary>
    static class FieldFocusHandler
    {
        private static int _lastUniqueId;

        // Track last focus so dialogs can re-queue the interrupted item
        private static int _lastPlayer, _lastPosition, _lastViewIndex;
        private static bool _hasLastFocus;

        // Hold a reference to prevent GC of the managed delegate
        private static DuelClient.onFocusFieldDelegate _handler;

        public static void Subscribe(DuelClient client)
        {
            _handler = (Action<int, int, int>)OnFieldFocused;
            client.add_onFocusFieldHandler(_handler);
            Log.Write("[FieldFocus] Subscribed to onFocusFieldHandler");
        }

        static void OnFieldFocused(int player, int position, int viewIndex)
        {
            try
            {
                if (!NavigationState.IsInDuel || !DuelState.HasPhaseStarted) return;
                if (DuelState.IsShowingResult) return;

                // Player left the hand — reset hand dedup so re-entering reads correctly
                PatchCardInfoSetDescription.ResetHandDedup();

                // Selection list pending — defer field focus until title is spoken.
                if (DuelState.HasPendingSelection)
                {
                    DuelState.DeferredFieldFocus = (player, position, viewIndex);
                    return;
                }

                // Suppress while field input is blocked (animations, transitions).
                // fieldInputBlockCounter is the game's native interactivity gate.
                try
                {
                    var client = DuelClient.instance;
                    if (client != null && client.fieldInputBlockCounter > 0) return;
                }
                catch { }

                // After CardCommand closes, suppress this auto-focus silently.
                // The selection prompt message will speak first, then
                // MessageJustAnnounced will queue the next manual navigation.
                if (DuelState.SuppressNextFieldFocus)
                {
                    DuelState.SuppressNextFieldFocus = false;
                    // Store for re-queue, but skip pile zones (navigation artifacts)
                    if (position != Engine.PosExtra && position != Engine.PosDeck)
                    {
                        _lastPlayer = player;
                        _lastPosition = position;
                        _lastViewIndex = viewIndex;
                        _hasLastFocus = true;
                    }
                    return;
                }

                // Consume message flag — queue speech after a game event message
                // instead of interrupting it.
                bool queued = DuelState.MessageJustAnnounced;
                DuelState.MessageJustAnnounced = false;

                // Field focus means we're not in a selection list
                DuelState.InSelectionList = false;

                // Track for re-queue if a dialog interrupts this focus
                _lastPlayer = player;
                _lastPosition = position;
                _lastViewIndex = viewIndex;
                _hasLastFocus = true;

                string zone = GetZoneName(player, position, viewIndex);

                // Pile zones (Extra Deck, Deck) — just speak the zone name,
                // don't read individual cards from the pile.
                if (position == Engine.PosExtra || position == Engine.PosDeck)
                {
                    if (!string.IsNullOrEmpty(zone))
                        SpeakField(zone, queued);
                    _lastUniqueId = 0;
                    _hasLastFocus = false; // Don't re-queue pile zones after dialogs
                    return;
                }

                int mrk = 0;
                int uniqueId = 0;
                try
                {
                    mrk = Engine.GetCardID(player, position, viewIndex);
                    uniqueId = Engine.GetCardUniqueID(player, position, viewIndex);
                }
                catch (Exception ex)
                {
                    Log.Write($"[FocusField] Engine query failed: {ex.Message}");
                }

                // Opponent's face-down cards: game hides the card ID (mrk=0) but
                // the card still exists. Use GetCardNum to detect it on field zones.
                if (mrk <= 0 && player != 0 && (IsMonsterZone(position) || IsSpellTrapZone(position)))
                {
                    try
                    {
                        int count = Engine.GetCardNum(player, position);
                        if (count > 0)
                        {
                            string msg = !string.IsNullOrEmpty(zone)
                                ? $"Face-down card, {zone}"
                                : "Face-down card";
                            Log.Write($"[FocusField] {msg}");
                            SpeakField(msg, queued);
                            _lastUniqueId = 0;
                            return;
                        }
                    }
                    catch (Exception ex) { Log.Write($"[FocusField] Face-down check: {ex.Message}"); }
                }

                if (mrk <= 0)
                {
                    // No card at this position — speak zone name only
                    if (!string.IsNullOrEmpty(zone))
                    {
                        Log.Write($"[FocusField] Empty: {zone}");
                        SpeakField(zone, queued);
                    }
                    _lastUniqueId = 0;
                    return;
                }

                // Don't reveal opponent's face-down cards (when mrk is known
                // but card is still physically face-down on the field)
                if (player != 0)
                {
                    try
                    {
                        if (!Engine.GetCardFace(player, position, viewIndex))
                        {
                            string msg = !string.IsNullOrEmpty(zone)
                                ? $"Face-down card, {zone}"
                                : "Face-down card";
                            Log.Write($"[FocusField] {msg}");
                            SpeakField(msg, queued);
                            _lastUniqueId = uniqueId;
                            return;
                        }
                    }
                    catch { }
                }

                // Dedup — suppress re-reading the same card instance
                if (uniqueId > 0 && uniqueId == _lastUniqueId) return;
                if (uniqueId > 0) _lastUniqueId = uniqueId;

                // Battle position and live stats for monster zones
                string battlePos = null;
                int? liveAtk = null, liveDef = null;
                if (IsMonsterZone(position))
                {
                    try
                    {
                        bool face = Engine.GetCardFace(player, position, viewIndex);
                        bool turn = Engine.GetCardTurn(player, position, viewIndex);
                        battlePos = !face ? "Set" : turn ? "Defense Position" : "Attack Position";
                    }
                    catch (Exception ex) { Log.Write($"[FocusField] Position check: {ex.Message}"); }

                    try
                    {
                        var bv = new Engine.BasicVal();
                        Engine.GetCardBasicVal(player, position, viewIndex, ref bv);
                        liveAtk = bv.Atk;
                        liveDef = bv.Def;
                    }
                    catch (Exception ex) { Log.Write($"[FocusField] BasicVal: {ex.Message}"); }
                }

                CardReader.SpeakCardFromData(mrk, zone, liveAtk, liveDef, queued, battlePos);
            }
            catch (Exception ex) { Log.Write($"[PatchInvokeFocusField] {ex.Message}"); }
        }

        public static void ResetDedup() => _lastUniqueId = 0;

        /// <summary>
        /// Speak a deferred field focus queued after a selection title.
        /// Called from HandleTitle after the title is spoken.
        /// </summary>
        public static void SpeakDeferredFocus(int player, int position, int viewIndex)
        {
            // Skip pile zones — they're navigation artifacts, not selection targets
            if (position == Engine.PosExtra || position == Engine.PosDeck) return;

            string zone = GetZoneName(player, position, viewIndex);
            if (string.IsNullOrEmpty(zone)) return;

            int mrk = 0;
            try { mrk = Engine.GetCardID(player, position, viewIndex); }
            catch { }

            if (mrk > 0)
                CardReader.SpeakCardFromData(mrk, zone, queued: true);
            else if (!string.IsNullOrEmpty(zone))
                Speech.SayQueued(zone);
        }

        /// <summary>
        /// Re-queue the last focused field item. Called by dialog handlers
        /// when a dialog interrupts the auto-focused item.
        /// </summary>
        public static void RequeueLastFocus()
        {
            if (!_hasLastFocus) return;
            _hasLastFocus = false;
            SpeakDeferredFocus(_lastPlayer, _lastPosition, _lastViewIndex);
        }

        private static void SpeakField(string text, bool queued)
        {
            if (queued)
                Speech.SayQueued(text);
            else
                Speech.SayItem(text);
        }

        private static bool IsSpellTrapZone(int position)
        {
            return position == Engine.PosMagicLL || position == Engine.PosMagicL ||
                   position == Engine.PosMagicC || position == Engine.PosMagicR ||
                   position == Engine.PosMagicRR;
        }

        private static string GetZoneName(int player, int position, int viewIndex)
        {
            string side = player != 0 ? "Opponent's " : "";

            if (position == Engine.PosMonsterLL) return $"{side}Monster Zone 1";
            if (position == Engine.PosMonsterL) return $"{side}Monster Zone 2";
            if (position == Engine.PosMonsterC) return $"{side}Monster Zone 3";
            if (position == Engine.PosMonsterR) return $"{side}Monster Zone 4";
            if (position == Engine.PosMonsterRR) return $"{side}Monster Zone 5";
            if (position == Engine.PosMagicLL) return $"{side}Spell Trap Zone 1";
            if (position == Engine.PosMagicL) return $"{side}Spell Trap Zone 2";
            if (position == Engine.PosMagicC) return $"{side}Spell Trap Zone 3";
            if (position == Engine.PosMagicR) return $"{side}Spell Trap Zone 4";
            if (position == Engine.PosMagicRR) return $"{side}Spell Trap Zone 5";
            if (position == Engine.PosField) return $"{side}Field Spell Zone";
            if (position == Engine.PosPendulumLeft) return $"{side}Left Pendulum Zone";
            if (position == Engine.PosPendulumRight) return $"{side}Right Pendulum Zone";
            if (position == Engine.PosExLMonster) return $"{side}Extra Monster Zone Left";
            if (position == Engine.PosExRMonster) return $"{side}Extra Monster Zone Right";
            if (position == Engine.PosHand)
            {
                try
                {
                    int count = Engine.GetCardNum(player, Engine.PosHand);
                    if (count > 0 && viewIndex >= 0)
                        return $"{side}Hand, {viewIndex + 1} of {count}";
                }
                catch { }
                return $"{side}Hand";
            }
            if (position == Engine.PosExtra) return $"{side}Extra Deck";
            if (position == Engine.PosDeck) return $"{side}Deck";
            if (position == Engine.PosGrave) return $"{side}Graveyard";
            if (position == Engine.PosExclude) return $"{side}Banished";

            Log.Write($"[FocusField] Unknown position: player={player}, pos={position}, mapped to nothing");
            return null;
        }

        private static bool IsMonsterZone(int position)
        {
            return position == Engine.PosMonsterLL || position == Engine.PosMonsterL ||
                   position == Engine.PosMonsterC || position == Engine.PosMonsterR ||
                   position == Engine.PosMonsterRR || position == Engine.PosExLMonster ||
                   position == Engine.PosExRMonster;
        }
    }

    /// <summary>
    /// Card info panel reading — fires when any card gets focused in the duel.
    /// During duels:
    ///   - Hand cards: read directly from Engine database using CardInfoData.cardid
    ///     and speak immediately. No delayed UI read needed.
    ///   - Selection lists: use delayed ReadCardDelayed flow.
    ///   - Field cards: ignored here (handled by onFocusFieldHandler).
    /// Outside duels: fires normally for deck editor, card browser, etc.
    /// </summary>
    [HarmonyPatch(typeof(CardInfo), nameof(CardInfo.SetDescriptionArea))]
    class PatchCardInfoSetDescription
    {
        private const float ReadDelay = 0.15f;
        private static int _lastUniqueId;
        private static int _lastHandUniqueId;
        private static int _pendingMrk;
        private static int _pendingUniqueId;

        [HarmonyPostfix]
        static void Postfix(CardInfo __instance)
        {
            try
            {
                if (!__instance.gameObject.activeInHierarchy) return;

                if (NavigationState.IsInDuel)
                {
                    if (!DuelState.HasPhaseStarted) return;
                    if (DuelState.IsShowingResult) return;

                    try
                    {
                        var data = __instance.m_CardInfoData;

                        // Hand cards: read from Engine and speak immediately.
                        // Uses CardInfoData (position, cardid, index) as the trigger —
                        // the actual card data comes from the Engine database.
                        if (data.position == Engine.PosHand && data.player == 0)
                        {
                            int mrk = data.cardid;
                            if (mrk <= 0) return;

                            int uid = data.uniqueid;
                            if (uid > 0 && uid == _lastHandUniqueId) return;
                            if (uid > 0) _lastHandUniqueId = uid;

                            int handCount = Engine.GetCardNum(0, Engine.PosHand);
                            int idx = data.index;
                            string zone = handCount > 0
                                ? $"Hand, {idx + 1} of {handCount}"
                                : "Hand";

                            Log.Write($"[HandCard] mrk={mrk}, uid={uid}, {zone}");
                            CardReader.SpeakCardFromData(mrk, zone);
                            return;
                        }

                        // Selection lists (Extra Deck summon, material selection, etc.)
                        if (!DuelState.InSelectionList) return;

                        _pendingMrk = data.cardid;
                        _pendingUniqueId = data.uniqueid;
                    }
                    catch (Exception ex)
                    {
                        Log.Write($"[SetDescription] CardInfoData read failed: {ex.Message}");
                        if (!DuelState.InSelectionList) return;
                        _pendingMrk = 0;
                        _pendingUniqueId = 0;
                    }

                    BlindDuelCore.Instance.CancelInvoke(nameof(BlindDuelCore.ReadCardDelayed));
                    BlindDuelCore.Instance.Invoke(nameof(BlindDuelCore.ReadCardDelayed), ReadDelay);
                    return;
                }

                // Outside duels: read from UI panels (deck editor, card browser, etc.)
                try
                {
                    var data = __instance.m_CardInfoData;
                    _pendingMrk = data.cardid;
                    _pendingUniqueId = data.uniqueid;
                }
                catch (Exception ex)
                {
                    Log.Write($"[SetDescription] CardInfoData read failed: {ex.Message}");
                    _pendingMrk = 0;
                    _pendingUniqueId = 0;
                }

                BlindDuelCore.Instance.CancelInvoke(nameof(BlindDuelCore.ReadCardDelayed));
                BlindDuelCore.Instance.Invoke(nameof(BlindDuelCore.ReadCardDelayed), ReadDelay);
            }
            catch (Exception ex)
            {
                Log.Write($"[SetDescription] {ex.Message}");
            }
        }

        public static int PendingMrk => _pendingMrk;
        public static int PendingUniqueId => _pendingUniqueId;

        public static bool CheckAndUpdateDedup(int uniqueId)
        {
            if (uniqueId <= 0) return false;
            if (uniqueId == _lastUniqueId) return true;
            _lastUniqueId = uniqueId;
            return false;
        }

        public static void ResetDedup() => _lastUniqueId = 0;

        public static void ResetHandDedup() => _lastHandUniqueId = 0;
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
                if (NavigationState.IsInDuel)
                {
                    DuelState.MessageJustAnnounced = true;
                    Speech.SayImmediate(combined);
                }
                else
                {
                    Speech.SayQueued(combined);
                }
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
                if (NavigationState.IsInDuel)
                {
                    DuelState.MessageJustAnnounced = true;
                    Speech.SayImmediate(cleaned);
                }
                else
                {
                    Speech.SayQueued(cleaned);
                }
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
                if (NavigationState.IsInDuel)
                {
                    DuelState.MessageJustAnnounced = true;
                    Speech.SayImmediate(cleaned);
                }
                else
                {
                    Speech.SayQueued(cleaned);
                }
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
                if (NavigationState.IsInDuel)
                {
                    DuelState.MessageJustAnnounced = true;
                    Speech.SayImmediate(cleaned);
                }
                else
                {
                    Speech.SayQueued(cleaned);
                }
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

    /// <summary>
    /// Catches duel action notifications (summons, attacks, etc.) from the game's
    /// log system. AfterAddLog fires for every duel action with the formatted
    /// localized text. This catches opponent summon notifications that don't go
    /// through InstantMessage or TutorialNavigator.
    /// Dedup against InstantMessageDedup prevents double-speaking effects
    /// that are already caught by InstantMessage patches.
    /// </summary>
    [HarmonyPatch(typeof(DuelLogController), nameof(DuelLogController.AfterAddLog))]
    class PatchDuelLogAfterAddLog
    {
        [HarmonyPostfix]
        static void Postfix(string __0)
        {
            try
            {
                if (!NavigationState.IsInDuel) return;
                if (string.IsNullOrWhiteSpace(__0)) return;

                string cleaned = TextUtil.StripTags(__0);
                if (string.IsNullOrWhiteSpace(cleaned)) return;

                // Dedup against InstantMessage to avoid double-speaking effects
                if (cleaned == InstantMessageDedup.LastMessage) return;
                InstantMessageDedup.LastMessage = cleaned;

                Log.Write($"[DuelLog] {cleaned}");
                DuelState.MessageJustAnnounced = true;
                Speech.SayImmediate(cleaned);
            }
            catch (Exception ex) { Log.Write($"[PatchDuelLogAfterAddLog] {ex.Message}"); }
        }
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
                if (NavigationState.IsInDuel)
                {
                    DuelState.MessageJustAnnounced = true;
                    Speech.SayImmediate(cleaned);

                    // Re-queue the field item that was just interrupted.
                    // The field focus fires ~4ms before this dialog in the same frame,
                    // so the item spoke first and got cut off by SayImmediate above.
                    FieldFocusHandler.RequeueLastFocus();
                }
                else
                {
                    Speech.SayQueued(cleaned);
                }
            }
            catch (Exception ex) { Log.Write($"[PatchDuelInfoDialog] {ex.Message}"); }
        }
    }

    [HarmonyPatch]
    class PatchDuelConfirmDialogOpen
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            var methods = new List<MethodBase>();
            foreach (var m in typeof(DuelConfirmDialog).GetMethods())
            {
                if (m.Name == nameof(DuelConfirmDialog.Open))
                    methods.Add(m);
            }
            return methods;
        }

        [HarmonyPostfix]
        static void Postfix(string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message)) return;
                string cleaned = TextUtil.StripTags(message);
                if (string.IsNullOrWhiteSpace(cleaned)) return;

                Log.Write($"[DuelConfirmDialog] {cleaned}");
                Speech.SayImmediate(cleaned);
            }
            catch (Exception ex) { Log.Write($"[PatchDuelConfirmDialogOpen] {ex.Message}"); }
        }
    }

    [HarmonyPatch(typeof(DuelSelectDialog), nameof(DuelSelectDialog.Open))]
    class PatchDuelSelectDialogOpen
    {
        [HarmonyPostfix]
        static void Postfix(string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message)) return;
                string cleaned = TextUtil.StripTags(message);
                if (string.IsNullOrWhiteSpace(cleaned)) return;

                Log.Write($"[DuelSelectDialog] {cleaned}");
                Speech.SayImmediate(cleaned);
            }
            catch (Exception ex) { Log.Write($"[PatchDuelSelectDialogOpen] {ex.Message}"); }
        }
    }

    /// <summary>
    /// Speaks the selection prompt for card selection list mode.
    /// SetTitle is called internally during SetListImpl for list-based selections.
    /// Shared dedup/speech logic lives in HandleTitle().
    /// </summary>
    [HarmonyPatch(typeof(CardSelectionList), nameof(CardSelectionList.SetTitle))]
    class PatchCardSelectionListSetTitle
    {
        private static bool _nextReadQueued;
        private static string _lastTitle = "";

        /// <summary>
        /// Shared handler for selection prompt titles from both SetTitle and SetList patches.
        /// Deduplicates and speaks the title.
        /// </summary>
        public static void HandleTitle(string cleaned)
        {
            if (cleaned == _lastTitle) return;
            _lastTitle = cleaned;

            Log.Write($"[CardSelectionList] {cleaned}");
            DuelState.MessageJustAnnounced = true;
            Speech.SayImmediate(cleaned);
            _nextReadQueued = true;

            // Queue the deferred or interrupted item after the title.
            // This mirrors QueueFocusedItem for normal menus.
            DuelState.HasPendingSelection = false;
            QueueDeferredItem();

            // Re-queue what was interrupted by this title.
            FieldFocusHandler.RequeueLastFocus();

            // Re-queue button text that was queued but then interrupted by SayImmediate
            if (!string.IsNullOrEmpty(DuelState.LastQueuedButtonText))
            {
                Speech.SayQueued(DuelState.LastQueuedButtonText);
                DuelState.LastQueuedButtonText = null;
            }
        }

        /// <summary>
        /// Speak the item that was deferred during selection setup, queued after the title.
        /// </summary>
        private static void QueueDeferredItem()
        {
            // Deferred button (card list selection)
            var btn = DuelState.DeferredSelectionButton;
            if (btn != null)
            {
                DuelState.DeferredSelectionButton = null;
                try
                {
                    var handler = HandlerRegistry.Current;
                    if (handler != null)
                    {
                        string text = handler.OnButtonFocused(btn);
                        // Handler spoke the card directly (returned "")
                        // or returned null — try default text
                        if (text == null)
                        {
                            text = TextExtractor.ExtractFirst(btn.gameObject);
                        }
                        if (!string.IsNullOrWhiteSpace(text) && text != "")
                            Speech.SayQueued(text);
                    }
                }
                catch (Exception ex) { Log.Write($"[QueueDeferred] Button: {ex.Message}"); }
                return;
            }

            // Deferred field focus (field zone selection)
            var focus = DuelState.DeferredFieldFocus;
            if (focus.HasValue)
            {
                DuelState.DeferredFieldFocus = null;
                try
                {
                    var (player, position, viewIndex) = focus.Value;
                    FieldFocusHandler.SpeakDeferredFocus(player, position, viewIndex);
                }
                catch (Exception ex) { Log.Write($"[QueueDeferred] Field: {ex.Message}"); }
            }
        }

        [HarmonyPostfix]
        static void Postfix(string title)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(title)) return;
                string cleaned = TextUtil.StripTags(title);
                if (string.IsNullOrWhiteSpace(cleaned)) return;
                HandleTitle(cleaned);
            }
            catch (Exception ex) { Log.Write($"[PatchCardSelectionListSetTitle] {ex.Message}"); }
        }

        public static bool ConsumeQueuedFlag()
        {
            bool val = _nextReadQueued;
            _nextReadQueued = false;
            return val;
        }

        public static void ResetDedup() => _lastTitle = "";
    }

    /// <summary>
    /// Catches selection prompts that bypass SetTitle — e.g. field selection mode
    /// ("Select the card to send to the Graveyard"). SetList receives the title
    /// as a parameter and may not call SetTitle for all selection modes.
    /// </summary>
    [HarmonyPatch(typeof(CardSelectionList), nameof(CardSelectionList.SetList))]
    class PatchCardSelectionListSetList
    {
        /// <summary>
        /// PREFIX: Set pending flag BEFORE the game creates items and auto-focuses.
        /// Mirrors HasPendingScreen for normal menus.
        /// </summary>
        [HarmonyPrefix]
        static void Prefix()
        {
            if (NavigationState.IsInDuel)
            {
                DuelState.HasPendingSelection = true;
                DuelState.DeferredSelectionButton = null;
                DuelState.DeferredFieldFocus = null;
            }
        }

        [HarmonyPostfix]
        static void Postfix(string title)
        {
            try
            {
                // Always clear pending flag — even if title is empty.
                // Otherwise HasPendingSelection stays true and all buttons are deferred forever.
                if (NavigationState.IsInDuel)
                    DuelState.HasPendingSelection = false;

                if (string.IsNullOrWhiteSpace(title)) return;
                string cleaned = TextUtil.StripTags(title);
                if (string.IsNullOrWhiteSpace(cleaned)) return;
                PatchCardSelectionListSetTitle.HandleTitle(cleaned);
            }
            catch (Exception ex) { Log.Write($"[PatchCardSelectionListSetList] {ex.Message}"); }
        }
    }

    /// <summary>
    /// Catches field selection prompts that go through EffectTaskRunDialog
    /// (e.g. "Select the card to send to the Graveyard"). RunDialog sets up
    /// the selection UI and populates the text. Reading __instance.text here
    /// catches prompts that bypass CardSelectionList.SetTitle/SetList.
    /// </summary>
    [HarmonyPatch(typeof(EffectTaskRunDialog), nameof(EffectTaskRunDialog.RunDialog))]
    class PatchEffectTaskRunDialog
    {
        [HarmonyPostfix]
        static void Postfix(EffectTaskRunDialog __instance)
        {
            try
            {
                if (!NavigationState.IsInDuel) return;

                string text = __instance.text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    string cleaned = TextUtil.StripTags(text);
                    if (!string.IsNullOrWhiteSpace(cleaned))
                    {
                        Log.Write($"[EffectTaskRunDialog] {cleaned}");
                        PatchCardSelectionListSetTitle.HandleTitle(cleaned);
                        return;
                    }
                }

                // Fallback: read activateCardSelectionText static field
                string actText = EffectTaskRunDialog.activateCardSelectionText;
                if (!string.IsNullOrWhiteSpace(actText))
                {
                    string cleaned = TextUtil.StripTags(actText);
                    if (!string.IsNullOrWhiteSpace(cleaned))
                    {
                        Log.Write($"[EffectTaskRunDialog.activate] {cleaned}");
                        PatchCardSelectionListSetTitle.HandleTitle(cleaned);
                    }
                }
            }
            catch (Exception ex) { Log.Write($"[PatchEffectTaskRunDialog] {ex.Message}"); }
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

    [HarmonyPatch(typeof(CardCommand), nameof(CardCommand.Close))]
    class PatchCardCommandClose
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            try
            {
                if (!NavigationState.IsInDuel) return;
                // Suppress the next field focus — a selection prompt message
                // will follow shortly and should speak first.
                DuelState.SuppressNextFieldFocus = true;
            }
            catch (Exception ex) { Log.Write($"[PatchCardCommandClose] {ex.Message}"); }
        }
    }

    /// <summary>
    /// When battle position selection opens (Attack/Defense choice during summon),
    /// queue the first auto-focused button so it doesn't interrupt any preceding speech.
    /// SetDefaultPosition is called by the game when initializing position buttons.
    /// </summary>
    [HarmonyPatch(typeof(CardCommandEx), nameof(CardCommandEx.SetDefaultPosition))]
    class PatchPositionSelectOpen
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            try
            {
                if (!NavigationState.IsInDuel) return;
                Log.Write("[PositionSelect] Battle position selection opened");
                NavigationState.DialogJustAnnounced = true;
                DuelState.HasPendingSelection = true;
                DuelState.DeferredSelectionButton = null;
                DuelState.DeferredFieldFocus = null;
            }
            catch (Exception ex) { Log.Write($"[PatchPositionSelectOpen] {ex.Message}"); }
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
                DuelState.HasPhaseStarted = true;

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
