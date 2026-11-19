using UnityEngine;

namespace Metz.JamKit
{
    public static class ColorX
    {
        public static Color WithA(this Color color, float alpha) => new(color.r, color.g, color.b, alpha);
    }
}
