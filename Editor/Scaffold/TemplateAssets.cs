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
