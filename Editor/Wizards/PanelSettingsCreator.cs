using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit.Editor
{
    /// <summary>
    /// Creates a saved PanelSettings asset (Resources/JamKitPanelSettings) so the menu
    /// canvas renders with predictable scale + sort order across all scenes. If you skip this,
    /// <see cref="MenuController"/> falls back to a runtime-only PanelSettings with the same defaults.
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
                Debug.Log($"[JamKit] PanelSettings already exists at {assetPath}.");
                return AssetDatabase.LoadAssetAtPath<PanelSettings>(assetPath);
            }

            var ps = ScriptableObject.CreateInstance<PanelSettings>();
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1920, 1080);
            ps.match = 0.5f;
            ps.sortingOrder = 100;
            ps.clearColor = false;

            AssetDatabase.CreateAsset(ps, assetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[JamKit] Created PanelSettings at {assetPath}.");
            return ps;
        }
    }
}
