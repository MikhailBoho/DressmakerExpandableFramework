using System;
using System.Reflection;

namespace Dressmaker.DeskMail
{
    internal static class TextLayoutBinding
    {
        // TMP defaults vary between game/package versions. Never inherit the default:
        // newer TMP uses a named enum; older TMP exposes the boolean property.
        public static void EnableWrapping(object text)
        {
            var type = text.GetType();
            var mode = type.GetProperty("textWrappingMode", BindingFlags.Public | BindingFlags.Instance);
            if (mode != null && mode.CanWrite && mode.PropertyType.IsEnum &&
                Enum.IsDefined(mode.PropertyType, "Normal"))
            {
                mode.SetValue(text, Enum.Parse(mode.PropertyType, "Normal"), null);
                return;
            }
            var legacy = type.GetProperty("enableWordWrapping", BindingFlags.Public | BindingFlags.Instance);
            if (legacy != null && legacy.CanWrite && legacy.PropertyType == typeof(bool))
            {
                legacy.SetValue(text, true, null);
                return;
            }
            throw new MissingMemberException(type.FullName, "textWrappingMode / enableWordWrapping");
        }
    }
}
