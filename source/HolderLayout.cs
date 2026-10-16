using System;

namespace Dressmaker.DeskMail
{
    internal static class HolderLayout
    {
        public const float DefaultWidth = 300f;
        public const float DefaultX = -240f;
        public const float BellGap = 16f;
        public const int Revision = 2;

        // Upgrade only untouched old defaults, once. Preserve user-tuned values.
        public static void Migrate(int revision, ref float x, ref float width)
        {
            if (revision >= Revision) return;
            if (revision < 1)
            {
                if (Math.Abs(x - (-110f)) < 0.01f) x = DefaultX;
                if (Math.Abs(width - 160f) < 0.01f) width = DefaultWidth;
                return;
            }
            if (Math.Abs(width - 220f) < 0.01f) width = DefaultWidth;
        }

        // All inputs use the bell's parent's coordinate system. This applies to
        // the full rectangles, so opaque pixels cannot overlap even after scaling.
        public static float ClearBell(float requestedCenterX, float bellLeft, float width) =>
            Math.Min(requestedCenterX, bellLeft - BellGap - width * 0.5f);
    }
}
