using System;
using System.Linq;

namespace BlindDuel
{
    /// <summary>
    /// Utility for parsing game enum values from sprite/asset names.
    /// The game encodes attribute/rarity as the last character of sprite names.
    /// </summary>
    public static class EnumUtil
    {
        public static string ParseAttribute(string spriteName)
        {
            return ParseFromLastChar<CardAttribute>(spriteName);
        }

        public static string ParseRarity(string spriteName)
        {
            return ParseFromLastChar<CardRarity>(spriteName);
        }

        private static string ParseFromLastChar<T>(string text) where T : struct, Enum
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (int.TryParse(text.Last().ToString(), out int num) && Enum.IsDefined(typeof(T), num))
                return ((T)(object)num).ToString();
            return "";
        }
    }
}
