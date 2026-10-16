using System;

namespace Dressmaker.DeskMail
{
    // No coroutine or delayed callback survives closing the reader.
    internal sealed class OpeningMotion
    {
        public const float Duration = 0.95f;
        private float elapsed;
        public bool Active { get; private set; }
        public float Progress => Math.Min(1f, elapsed / Duration);
        public void Start() { elapsed = 0; Active = true; }
        public void Cancel() { elapsed = 0; Active = false; }
        public bool Advance(float unscaledDeltaTime)
        {
            if (!Active) return false;
            elapsed = Math.Min(Duration, elapsed + Math.Max(0, unscaledDeltaTime));
            if (elapsed < Duration) return false;
            Active = false;
            return true;
        }
        public static float Ease(float value, float from, float to)
        {
            float t = Math.Max(0, Math.Min(1, (value - from) / (to - from)));
            return t * t * (3f - 2f * t);
        }
    }
}
