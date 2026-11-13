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
        const string RuntimeUIDir = "Packages/com.metz.jamkit/Runtime/UI";
        const string OverlayDir = RuntimeUIDir + "/Overlays";
        const string UIDir = "Assets/_Project/UI";
        const string LegacyUIDir = UIDir + "/Resources";

        public const string MixerDest = "Assets/_Project/Audio/Resources/JamKitMixer.mixer";
        public const string PanelSettingsDest = UIDir + "/JamKitPanelSettings.asset";
        public const string MenuUxmlDest = UIDir + "/JamKitMenu.uxml";
        public const string StartUxmlDest = UIDir + "/JamKitStartMenu.uxml";
        public const string PauseUxmlDest = UIDir + "/JamKitPauseMenu.uxml";
        public const string SettingsUxmlDest = UIDir + "/JamKitSettingsMenu.uxml";
        public const string GameOverUxmlDest = UIDir + "/JamKitGameOverMenu.uxml";
        public const string MenuUssDest = UIDir + "/JamKitMenu.uss";

        public static ThemeStyleSheet Theme => AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>($"{RuntimeUIDir}/JamKitDefaultTheme.tss");

        public static VisualTreeAsset Overlay(string name) => AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{OverlayDir}/{name}.uxml");

        public static PanelSettings OverlayPanel(string name) => AssetDatabase.LoadAssetAtPath<PanelSettings>($"{OverlayDir}/{name}PanelSettings.asset");

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
            if (ps != null && ps.themeStyleSheet == null)
            {
                ps.themeStyleSheet = Theme;
                EditorUtility.SetDirty(ps);
                AssetDatabase.SaveAssets();
            }
            return ps;
        }

        public static VisualTreeAsset EnsureMenuDocument(Object score)
        {
            EnsureCopy<StyleSheet>($"{TemplateDir}/JamKitMenu.uss", MenuUssDest);
            EnsureCopy<VisualTreeAsset>($"{TemplateDir}/JamKitStartMenu.uxml", StartUxmlDest);
            EnsureCopy<VisualTreeAsset>($"{TemplateDir}/JamKitPauseMenu.uxml", PauseUxmlDest);
            EnsureCopy<VisualTreeAsset>($"{TemplateDir}/JamKitSettingsMenu.uxml", SettingsUxmlDest);
            EnsureCopy<VisualTreeAsset>($"{TemplateDir}/JamKitGameOverMenu.uxml", GameOverUxmlDest);
            BindScore(score);
            return EnsureCopy<VisualTreeAsset>($"{TemplateDir}/JamKitMenu.uxml", MenuUxmlDest);
        }

        static void BindScore(Object score)
        {
            string path = GameOverUxmlDest;
            string text = File.ReadAllText(path);
            const string element = "<jk:ValueLabel name=\"gameover-score\"";
            int at = text.IndexOf(element, System.StringComparison.Ordinal);
            if (at < 0) return;

            int end = text.IndexOf('>', at);
            if (text.IndexOf("data-source=", at, end - at, System.StringComparison.Ordinal) >= 0) return;
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(score, out string guid, out long fileId)) return;

            string source = $" data-source=\"project://database/{AssetDatabase.GetAssetPath(score)}?fileID={fileId}&amp;guid={guid}&amp;type=2#{score.name}\"";
            File.WriteAllText(path, text.Insert(at + element.Length, source));
            AssetDatabase.ImportAsset(path);
        }

        static T EnsureCopy<T>(string templatePath, string destPath) where T : Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(destPath);
            if (existing != null) return existing;

            Directory.CreateDirectory(Path.GetDirectoryName(destPath));
            AssetDatabase.Refresh();

            string legacy = $"{LegacyUIDir}/{Path.GetFileName(destPath)}";
            if (destPath.StartsWith(UIDir + "/") && AssetDatabase.LoadAssetAtPath<T>(legacy) != null)
            {
                string error = AssetDatabase.MoveAsset(legacy, destPath);
                if (string.IsNullOrEmpty(error)) return AssetDatabase.LoadAssetAtPath<T>(destPath);
                Debug.LogError($"[JamKit] Failed to move {legacy} → {destPath}: {error}");
            }

            if (!AssetDatabase.CopyAsset(templatePath, destPath))
            {
                Debug.LogError($"[JamKit] Failed to copy template {templatePath} → {destPath}");
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<T>(destPath);
        }
    }
}
