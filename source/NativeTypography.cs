using UnityEngine;

namespace Dressmaker.DeskMail
{
    internal static class NativeTypography
    {
        // Read just the font. A targetGraphic can be only one transparent layer
        // of a native button, so it cannot safely stand in for the complete skin.
        public static object MenuFont(Component pigeon)
        {
            if (!pigeon) return null;
            try
            {
                var prefab = pigeon.GetType().GetField("personButtonPrefab", Reflect.Instance)?.GetValue(pigeon) as Component;
                var label = prefab ? prefab.GetType().GetField("text", Reflect.Instance)?.GetValue(prefab) as Component : null;
                return label ? Reflect.Get(label, "font") : null;
            }
            catch { return null; } // Optional typography; handwriting is the fallback.
        }
    }
}
