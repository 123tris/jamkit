using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    public static class JamKitUI
    {
        public static ThemeStyleSheet DefaultTheme => Resources.Load<ThemeStyleSheet>("JamKitDefaultTheme");

        public static PanelSettings CreatePanelSettings(PanelScaleMode scaleMode, int sortingOrder)
        {
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = scaleMode;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.sortingOrder = sortingOrder;
            settings.themeStyleSheet = DefaultTheme;
            return settings;
        }
    }
}
