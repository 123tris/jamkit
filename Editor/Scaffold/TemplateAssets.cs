using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

namespace Metz.JamKit.Editor
{
    public static class TemplateAssets
    {
        const string TemplateDir = "Packages/com.metz.jamkit/Editor/Templates";
        public const string MixerDest = "Assets/_Project/Audio/Resources/JamKitMixer.mixer";
        public const string PanelSettingsDest = "Assets/_Project/UI/Resources/JamKitPanelSettings.asset";
        public const string MenuUxmlDest = "Assets/_Project/UI/Resources/JamKitMenu.uxml";
        public const string StartUxmlDest = "Assets/_Project/UI/Resources/JamKitStartMenu.uxml";
        public const string PauseUxmlDest = "Assets/_Project/UI/Resources/JamKitPauseMenu.uxml";
        public const string SettingsUxmlDest = "Assets/_Project/UI/Resources/JamKitSettingsMenu.uxml";
        public const string GameOverUxmlDest = "Assets/_Project/UI/Resources/JamKitGameOverMenu.uxml";
        public const string MenuUssDest = "Assets/_Project/UI/Resources/JamKitMenu.uss";

        public static AudioMixer EnsureMixer()
            => EnsureCopy<AudioMixer>($"{TemplateDir}/JamKitMixer.mixer", MixerDest);

        public static AudioMixer RepairMixer(string destPath)
        {
            AssetDatabase.DeleteAsset(destPath);
            return EnsureCopy<AudioMixer>($"{TemplateDir}/JamKitMixer.mixer", destPath);
        }

        public static PanelSettings EnsurePanelSettings()
        {
            var ps = EnsureCopy<PanelSettings>($"{TemplateDir}/JamKitPanelSettings.asset", PanelSettingsDest);
            if (ps != null && ps.themeStyleSheet == null && JamKitUI.DefaultTheme != null)
            {
                ps.themeStyleSheet = JamKitUI.DefaultTheme;
                EditorUtility.SetDirty(ps);
                AssetDatabase.SaveAssets();
            }
            return ps;
        }

        public static VisualTreeAsset EnsureMenuDocument()
        {
            EnsureCopy<StyleSheet>($"{TemplateDir}/JamKitMenu.uss", MenuUssDest);
            EnsureCopy<VisualTreeAsset>($"{TemplateDir}/JamKitStartMenu.uxml", StartUxmlDest);
            EnsureCopy<VisualTreeAsset>($"{TemplateDir}/JamKitPauseMenu.uxml", PauseUxmlDest);
            EnsureCopy<VisualTreeAsset>($"{TemplateDir}/JamKitSettingsMenu.uxml", SettingsUxmlDest);
            EnsureCopy<VisualTreeAsset>($"{TemplateDir}/JamKitGameOverMenu.uxml", GameOverUxmlDest);
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
