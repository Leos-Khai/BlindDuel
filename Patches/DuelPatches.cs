using System;
using Il2CppYgomGame.Duel;
using HarmonyLib;

namespace BlindDuel
{
    [HarmonyPatch(typeof(DuelLP), nameof(DuelLP.ChangeLP), MethodType.Normal)]
    class PatchChangeLP
    {
        [HarmonyPostfix]
        static void Postfix(DuelLP __instance)
        {
            string who = __instance.name.Contains("Far") ? "Opponent's" : "Your";
            Speech.Say($"{who} current life points: {__instance.currentLP}", SpeechPriority.Info);

            if (__instance.currentLP < 1)
            {
                NavigationState.IsInDuel = false;
                DuelState.Clear();
            }
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
        [HarmonyPostfix]
        static void Postfix(CardInfo __instance)
        {
            // TODO: Schedule card info reading via DuelHandler
        }
    }
}
