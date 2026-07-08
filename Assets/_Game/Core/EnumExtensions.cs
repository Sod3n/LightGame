using System;

namespace Light_and_controller.Scripts
{
    public static class EnumExtensions
    {
        public static string? KeyToString<T>(this T value) where T : Enum
            => Enum.GetName(typeof(T), value);
    }
}