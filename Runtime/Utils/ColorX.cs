using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>Color extension helpers.</summary>
    public static class ColorX
    {
        public static Color WithA(this Color c, float a) => new(c.r, c.g, c.b, a);
        public static Color WithR(this Color c, float r) => new(r, c.g, c.b, c.a);
        public static Color WithG(this Color c, float g) => new(c.r, g, c.b, c.a);
        public static Color WithB(this Color c, float b) => new(c.r, c.g, b, c.a);

        public static Color FromHex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Color.white;
            if (hex.StartsWith("#")) hex = hex.Substring(1);
            if (hex.Length == 6) hex += "FF";
            if (hex.Length != 8) return Color.white;
            int r = System.Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = System.Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = System.Convert.ToInt32(hex.Substring(4, 2), 16);
            int a = System.Convert.ToInt32(hex.Substring(6, 2), 16);
            return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
        }

        public static string ToHex(this Color c)
        {
            var i = (Color32)c;
            return $"#{i.r:X2}{i.g:X2}{i.b:X2}{i.a:X2}";
        }
    }
}
