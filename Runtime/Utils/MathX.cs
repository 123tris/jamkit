using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>Misc math helpers commonly needed in jam code.</summary>
    public static class MathX
    {
        public static float Remap(float v, float a, float b, float c, float d)
            => c + (v - a) * (d - c) / (b - a);

        public static float Remap01(float v, float a, float b)
            => Mathf.Clamp01((v - a) / (b - a));

        public static int WrapIndex(int i, int length)
        {
            if (length <= 0) return 0;
            int m = i % length;
            return m < 0 ? m + length : m;
        }

        public static bool Approximately(float a, float b, float epsilon = 1e-4f)
            => Mathf.Abs(a - b) <= epsilon;
    }
}
