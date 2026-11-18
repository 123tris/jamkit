using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Metz.JamKit.Editor
{
    public static class NewLevel
    {
        const string Title = "JamKit: New Level";

        [MenuItem("JamKit/New Level", priority = 2)]
        public static void Create()
        {
            if (!File.Exists(ProjectTemplate.LevelTemplate))
            {
                if (EditorUtility.DisplayDialog(Title,
                        $"{ProjectTemplate.LevelTemplate} is missing. Run JamKit > New Jam Project to copy it in?", "Run Wizard", "Cancel"))
                    JamProjectWizard.Run();
                return;
            }

            string path = EditorUtility.SaveFilePanelInProject(Title, "Level", "unity",
                "The level starts with JamKitCore, the menu (Esc pauses), a camera, a light and an EventSystem, and joins the build before GameOver.",
                Path.GetDirectoryName(ProjectTemplate.GameScene)?.Replace('\\', '/'));
            if (string.IsNullOrEmpty(path)) return;
            if (File.Exists(path))
            {
                EditorUtility.DisplayDialog(Title, $"{path} already exists. Pick another name.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (!AssetDatabase.CopyAsset(ProjectTemplate.LevelTemplate, path))
            {
                Debug.LogError($"[JamKit] Could not copy {ProjectTemplate.LevelTemplate} to {path}.");
                return;
            }

            ProjectTemplate.AddLevelToBuild(path);
            EditorSceneManager.OpenScene(path);
            Debug.Log($"[JamKit] {Path.GetFileNameWithoutExtension(path)} is in the build before GameOver. " +
                      "Load it from a Game Flow transition, or from a SceneLoader on a signal.");
        }
    }
}
