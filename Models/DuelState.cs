using System.Collections.Generic;
using Il2CppYgomGame.Duel;

namespace BlindDuel
{
    /// <summary>
    /// Tracks duel-specific state: cards on field, LP, etc.
    /// </summary>
    public static class DuelState
    {
        public static List<CardRoot> Cards { get; } = new();

        public static void Clear()
        {
            Cards.Clear();
        }

        public static CardRoot FindCardAtPosition(UnityEngine.Vector3 position)
        {
            return Cards.Find(c => c.cardLocator.pos == position);
        }
    }
}
