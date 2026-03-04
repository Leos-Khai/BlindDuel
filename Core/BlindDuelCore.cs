using System.Linq;
using UnityEngine;
using Il2CppYgomGame.Duel;
using MelonLoader;

namespace BlindDuel
{
    public class BlindDuelCore : MonoBehaviour
    {
        public static BlindDuelCore Instance { get; private set; }

        // Current preview data for card/item reading
        public static PreviewData Preview { get; } = new();

        public void Awake()
        {
            Instance = this;
            Log.Init();
            ScreenReader.Initialize();
            HandlerRegistry.Init();
        }

        public void OnApplicationQuit()
        {
            ScreenReader.Shutdown();
        }

        public void Update()
        {
            // Duel hotkeys
            if (NavigationState.IsInDuel)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    var duelLPs = FindObjectsOfType<DuelLP>().ToList();
                    var near = duelLPs.Find(e => e.m_IsNear);
                    var far = duelLPs.Find(e => !e.m_IsNear);
                    Speech.SayImmediate($"Your life points: {near?.currentLP}\nOpponent's life points: {far?.currentLP}");
                }

                if (Input.GetKeyDown(KeyCode.LeftAlt))
                {
                    Preview.Clear();
                    var cardInfo = FindObjectOfType<CardInfo>();
                    if (cardInfo != null)
                    {
                        if (!cardInfo.gameObject.activeInHierarchy)
                            cardInfo.gameObject.SetActive(true);
                        // TODO: Read card info via CardData extraction
                    }
                }
            }

            // Detection: screen/dialog changes
            DialogDetector.Poll();
            ScreenDetector.Poll();
        }
    }
}
