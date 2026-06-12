using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    /// <summary>
    /// Shared UI Toolkit plumbing. The important bit: every PanelSettings needs a
    /// <see cref="ThemeStyleSheet"/> or default controls render unstyled — JamKit ships
    /// <c>JamKitDefaultTheme.tss</c> (an import of Unity's built-in default theme) and this class
    /// hands it to every PanelSettings JamKit creates or loads.
    /// </summary>
    public static class JamKitUI
    {
        public const string ThemeResourceName = "JamKitDefaultTheme";
        public const string MenuPanelSettingsResourceName = "JamKitPanelSettings";

        static ThemeStyleSheet _theme;
        static PanelSettings _menuFallback;

        /// <summary>The bundled default theme (cached). Null only if the package Resources are missing.</summary>
        public static ThemeStyleSheet DefaultTheme
        {
            get
            {
                if (_theme == null) _theme = Resources.Load<ThemeStyleSheet>(ThemeResourceName);
                return _theme;
            }
        }

        /// <summary>Assign the bundled theme if the PanelSettings has none. Safe on assets (in-memory only at runtime).</summary>
        public static void ApplyDefaultTheme(PanelSettings ps)
        {
            if (ps != null && ps.themeStyleSheet == null && DefaultTheme != null)
                ps.themeStyleSheet = DefaultTheme;
        }

        /// <summary>Create a themed runtime PanelSettings. Caller owns the instance.</summary>
        public static PanelSettings CreatePanelSettings(PanelScaleMode scaleMode, int sortingOrder)
        {
            var ps = ScriptableObject.CreateInstance<PanelSettings>();
            ps.scaleMode = scaleMode;
            if (scaleMode == PanelScaleMode.ScaleWithScreenSize)
                ps.referenceResolution = new Vector2Int(1920, 1080);
            ps.sortingOrder = sortingOrder;
            ApplyDefaultTheme(ps);
            return ps;
        }

        /// <summary>
        /// The PanelSettings menus should use: the saved JamKitPanelSettings asset if it exists
        /// (theme-patched in memory in case it predates the theme fix), otherwise a shared themed
        /// runtime instance.
        /// </summary>
        public static PanelSettings LoadOrCreateMenuPanelSettings()
        {
            var loaded = Resources.Load<PanelSettings>(MenuPanelSettingsResourceName);
            if (loaded != null)
            {
                ApplyDefaultTheme(loaded);
                return loaded;
            }
            if (_menuFallback == null)
                _menuFallback = CreatePanelSettings(PanelScaleMode.ScaleWithScreenSize, 100);
            return _menuFallback;
        }
    }
}
