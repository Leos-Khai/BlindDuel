using System;
using Il2CppYgomGame.Scenario;
using HarmonyLib;

namespace BlindDuel
{
    [HarmonyPatch(typeof(ScenarioBehaviour_Text), nameof(ScenarioBehaviour_Text.ProgressInit))]
    class PatchScenarioText
    {
        static string _lastText = "";

        [HarmonyPostfix]
        static void Postfix(ScenarioBehaviour_Text __instance)
        {
            try
            {
                string text = __instance.m_Text;
                if (string.IsNullOrWhiteSpace(text)) return;

                text = TextUtil.StripTags(text).Trim();
                if (string.IsNullOrWhiteSpace(text)) return;
                if (text == _lastText) return;

                _lastText = text;
                Log.Write($"[Scenario] Text: {text}");
                Speech.SayQueued(text);
            }
            catch { }
        }

        public static void Reset() => _lastText = "";
    }

    [HarmonyPatch(typeof(ScenarioBehaviour_Title), nameof(ScenarioBehaviour_Title.ProgressInit))]
    class PatchScenarioTitle
    {
        static string _lastTitle = "";

        [HarmonyPostfix]
        static void Postfix(ScenarioBehaviour_Title __instance)
        {
            try
            {
                string title = __instance.m_TitleText;
                if (string.IsNullOrWhiteSpace(title)) return;

                title = TextUtil.StripTags(title).Trim();
                if (string.IsNullOrWhiteSpace(title)) return;
                if (title == _lastTitle) return;

                _lastTitle = title;
                Log.Write($"[Scenario] Title: {title}");
                Speech.SayQueued(title);
            }
            catch { }
        }

        public static void Reset() => _lastTitle = "";
    }

    /// <summary>
    /// Hooks the movie subtitle marker callback directly.
    /// This closure method fires each time MovieWidget.onMarkerChanged triggers,
    /// which happens at each subtitle timestamp during movie playback.
    /// The closure holds the subtitle TMP reference that the native code updates.
    /// </summary>
    [HarmonyPatch(typeof(ScenarioBehaviour_MovieCreate.__c__DisplayClass10_1),
        nameof(ScenarioBehaviour_MovieCreate.__c__DisplayClass10_1._ProgressInit_b__7))]
    class PatchMovieMarkerCallback
    {
        static string _lastText = "";

        [HarmonyPostfix]
        static void Postfix(ScenarioBehaviour_MovieCreate.__c__DisplayClass10_1 __instance)
        {
            try
            {
                var tmp = __instance.subtitle;
                if (tmp == null)
                {
                    Log.Write("[Scenario] Marker fired but subtitle TMP is null");
                    return;
                }

                string text = tmp.text;
                if (string.IsNullOrWhiteSpace(text)) return;

                text = TextUtil.StripTags(text).Trim();
                if (string.IsNullOrWhiteSpace(text)) return;
                if (text == _lastText) return;

                _lastText = text;
                Log.Write($"[Scenario] Movie subtitle: {text}");
                Speech.SayQueued(text);
            }
            catch (Exception ex)
            {
                Log.Write($"[Scenario] Marker callback error: {ex.Message}");
            }
        }

        public static void Reset() => _lastText = "";
    }

    /// <summary>
    /// Resets subtitle dedup when movie finishes so replayed movies still speak.
    /// </summary>
    [HarmonyPatch(typeof(ScenarioBehaviour_MovieCreate), nameof(ScenarioBehaviour_MovieCreate.ProgressFinish))]
    class PatchMovieFinish
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            PatchScenarioText.Reset();
            PatchScenarioTitle.Reset();
            PatchMovieMarkerCallback.Reset();
        }
    }

}
