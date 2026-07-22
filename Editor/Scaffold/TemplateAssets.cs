using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

namespace Metz.JamKit.Editor
{
    /// <summary>
    /// Copies the package's template assets (audio mixer with exposed volume params, scaled
    /// PanelSettings) into the project. Replaces ~270 lines of reflection-based asset builders:
    /// the templates are authored once and copied byte-for-byte, so there is nothing to drift.
    /// </summary>
    public static class TemplateAssets
    {
        const string TemplateDir = "Packages/com.metz.jamkit/Editor/Templates";
        public const string MixerDest = "Assets/_Project/Audio/Resources/JamKitMixer.mixer";
        public const string PanelSettingsDest = "Assets/_Project/UI/Resources/JamKitPanelSettings.asset";
        public const string MenuUxmlDest = "Assets/_Project/UI/Resources/JamKitMenu.uxml";
        public const string StartUxmlDest = "Assets/_Project/UI/Resources/JamKitStartMenu.uxml";
        public const string PauseUxmlDest = "Assets/_Project/UI/Resources/JamKitPauseMenu.uxml";
        public const string SettingsUxmlDest = "Assets/_Project/UI/Resources/JamKitSettingsMenu.uxml";
        public const string MenuUssDest = "Assets/_Project/UI/Resources/JamKitMenu.uss";

        /// <summary>Master/Music/SFX groups with MasterVol/MusicVol/SfxVol exposed. Unity-audio path only.</summary>
        public static AudioMixer EnsureMixer()
            => EnsureCopy<AudioMixer>($"{TemplateDir}/JamKitMixer.mixer", MixerDest);

        /// <summary>Delete and re-copy the mixer template — the Doctor's fix for missing exposed params.</summary>
        public static AudioMixer RepairMixer(string destPath)
        {
            AssetDatabase.DeleteAsset(destPath);
            return EnsureCopy<AudioMixer>($"{TemplateDir}/JamKitMixer.mixer", destPath);
        }

        /// <summary>Scale-with-screen-size PanelSettings, sort order 100, package theme assigned on copy.</summary>
        public static PanelSettings EnsurePanelSettings()
        {
            var ps = EnsureCopy<PanelSettings>($"{TemplateDir}/JamKitPanelSettings.asset", PanelSettingsDest);
            if (ps != null && ps.themeStyleSheet == null && JamKitUI.DefaultTheme != null)
            {
                // The template ships theme-less (a baked cross-asset ref is one more thing to
                // break); the theme is assigned here, where Resources can resolve it.
                ps.themeStyleSheet = JamKitUI.DefaultTheme;
                EditorUtility.SetDirty(ps);
                AssetDatabase.SaveAssets();
            }
            return ps;
        }

        /// <summary>
        /// The menu markup + styles, copied into the project so the designer owns them: these are a
        /// starting point to build off, not package internals. Edits live in the project and are never
        /// overwritten (EnsureCopy is load-or-copy), so re-running the wizard is safe.
        /// Copy order matters because every reference is a relative &lt;... src&gt;, resolved at import:
        /// the USS first (the UXMLs' &lt;Style src="JamKitMenu.uss"&gt;), then the three per-view
        /// documents, then JamKitMenu.uxml — the composition root that instances them via
        /// &lt;ui:Template src="JamKit{Start,Pause,Settings}Menu.uxml"&gt;.
        /// </summary>
        public static VisualTreeAsset EnsureMenuDocument()
        {
            EnsureCopy<StyleSheet>($"{TemplateDir}/JamKitMenu.uss", MenuUssDest);
            EnsureCopy<VisualTreeAsset>($"{TemplateDir}/JamKitStartMenu.uxml", StartUxmlDest);
            EnsureCopy<VisualTreeAsset>($"{TemplateDir}/JamKitPauseMenu.uxml", PauseUxmlDest);
            EnsureCopy<VisualTreeAsset>($"{TemplateDir}/JamKitSettingsMenu.uxml", SettingsUxmlDest);
            return EnsureCopy<VisualTreeAsset>($"{TemplateDir}/JamKitMenu.uxml", MenuUxmlDest);
        }

        static T EnsureCopy<T>(string templatePath, string destPath) where T : Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(destPath);
            if (existing != null) return existing;

            Directory.CreateDirectory(Path.GetDirectoryName(destPath));
            AssetDatabase.Refresh();
            if (!AssetDatabase.CopyAsset(templatePath, destPath))
            {
                Debug.LogError($"[JamKit] Failed to copy template {templatePath} → {destPath}");
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<T>(destPath);
        }
    }
}
