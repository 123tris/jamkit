using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit.Editor
{
    /// <summary>
    /// Creates a saved PanelSettings asset (Resources/JamKitPanelSettings) so the menu
    /// canvas renders with predictable scale + sort order across all scenes, with JamKit's
    /// default theme assigned (no theme = unstyled controls). Re-running on an existing asset
    /// repairs a missing theme. If you skip this, <see cref="MenuController"/> falls back to a
    /// runtime-only PanelSettings with the same defaults.
    /// </summary>
    public static class PanelSettingsCreator
    {
        public const string DefaultDir = "Assets/_Project/UI/Resources";
        public const string DefaultName = "JamKitPanelSettings";

        [MenuItem("JamKit/Setup/Create Panel Settings")]
        public static void CreateMenu() => CreateAt($"{DefaultDir}/{DefaultName}.asset");

        public static PanelSettings CreateAt(string assetPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
            if (File.Exists(assetPath))
            {
                var existing = AssetDatabase.LoadAssetAtPath<PanelSettings>(assetPath);
                if (existing != null && existing.themeStyleSheet == null && JamKitUI.DefaultTheme != null)
                {
                    existing.themeStyleSheet = JamKitUI.DefaultTheme;
                    EditorUtility.SetDirty(existing);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[JamKit] Repaired PanelSettings at {assetPath} — assigned the default theme.");
                }
                else
                {
                    Debug.Log($"[JamKit] PanelSettings already exists at {assetPath}.");
                }
                return existing;
            }

            var ps = JamKitUI.CreatePanelSettings(PanelScaleMode.ScaleWithScreenSize, sortingOrder: 100);
            ps.match = 0.5f;
            ps.clearColor = false;
            if (ps.themeStyleSheet == null)
                Debug.LogWarning("[JamKit] JamKitDefaultTheme.tss not found — assign a Theme Style Sheet to the PanelSettings manually or UI controls will render unstyled.");

            AssetDatabase.CreateAsset(ps, assetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[JamKit] Created PanelSettings at {assetPath}.");
            return ps;
        }
    }
}
