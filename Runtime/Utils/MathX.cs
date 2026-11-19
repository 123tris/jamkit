using UnityEngine;

namespace Metz.JamKit
{
    public static class MathX
    {
        public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
            => Mathf.Approximately(fromMin, fromMax)
                ? toMin
                : toMin + (value - fromMin) * (toMax - toMin) / (fromMax - fromMin);

        public static float Remap01(float value, float min, float max)
            => Mathf.Approximately(min, max) ? 0f : Mathf.Clamp01((value - min) / (max - min));
    }
}
