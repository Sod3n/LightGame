using System;

namespace LightGame.Core
{
    public static class EnumExtensions
    {
        public static string? KeyToString<T>(this T value) where T : Enum
            => Enum.GetName(typeof(T), value);
    }
}