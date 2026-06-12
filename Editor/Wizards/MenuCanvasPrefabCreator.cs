using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Metz.JamKit.Editor
{
    /// <summary>
    /// Saves the JamKit menu (UIDocument + MenuController + PanelSettings) as a reusable prefab.
    /// Drag the resulting prefab into any scene to get the full Start / Settings / Pause flow.
    /// </summary>
    public static class MenuCanvasPrefabCreator
    {
        const string DefaultDir = "Assets/_Project/Prefabs";
        const string DefaultName = "JamKitMenu";

        [MenuItem("JamKit/Create Menu Prefab", priority = 10)]
        public static void Create()
        {
            Directory.CreateDirectory(DefaultDir);
            var path = EditorUtility.SaveFilePanelInProject(
                "Save JamKit Menu Prefab",
                DefaultName,
                "prefab",
                "Choose where to save the menu prefab.",
                DefaultDir);

            if (string.IsNullOrEmpty(path)) return;
            CreateAt(path);
        }

        public static GameObject CreateAt(string assetPath)
        {
            var activeScene = SceneManager.GetActiveScene();
            var before = activeScene.GetRootGameObjects();
            var beforeSet = new System.Collections.Generic.HashSet<int>();
            foreach (var go in before) beforeSet.Add(go.GetInstanceID());

            var refs = MenuCanvasBuilder.Build();

            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
            var saved = PrefabUtility.SaveAsPrefabAsset(refs.Root, assetPath, out var success);

            // Clean up any new roots in the active scene.
            foreach (var go in activeScene.GetRootGameObjects())
            {
                if (beforeSet.Contains(go.GetInstanceID())) continue;
                Object.DestroyImmediate(go);
            }

            if (!success || saved == null)
            {
                Debug.LogError($"[JamKit] Failed to save menu prefab at {assetPath}.");
                return null;
            }

            Debug.Log($"[JamKit] Menu prefab saved at {assetPath}.");
            EditorGUIUtility.PingObject(saved);
            Selection.activeObject = saved;
            return saved;
        }
    }
}
