using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Metz.JamKit.Editor
{
    static class JamKitToolbar
    {
        const string ScenesPath = "JamKit/Scenes";
        const string StartersPath = "JamKit/Starters";
        const string PlayFromStartPath = "JamKit/Play From Start";
        const string TimeScalePath = "JamKit/Time Scale";
        const string DoctorPath = "JamKit/Doctor";
        const string BuildPath = "JamKit/Build";

        [MainToolbarElement(ScenesPath, defaultDockPosition = MainToolbarDockPosition.Middle)]
        static MainToolbarElement Scenes()
            => new MainToolbarDropdown(new MainToolbarContent("Scenes", "Open a scene from Build Settings"), ShowScenes);

        [MainToolbarElement(PlayFromStartPath, defaultDockPosition = MainToolbarDockPosition.Middle)]
        static MainToolbarElement PlayFromStart()
            => new MainToolbarToggle(
                new MainToolbarContent("From Start", "Enter Play mode in the first Build Settings scene"),
                EditorSceneManager.playModeStartScene != null, SetPlayFromStart);

        [MainToolbarElement(TimeScalePath, defaultDockPosition = MainToolbarDockPosition.Middle)]
        static MainToolbarElement TimeScale()
            => new MainToolbarSlider(new MainToolbarContent("Time", "Time scale while playing"), 1f, 0f, 2f, SetTimeScale);

        [MainToolbarElement(StartersPath, defaultDockPosition = MainToolbarDockPosition.Middle)]
        static MainToolbarElement Starters()
            => new MainToolbarButton(new MainToolbarContent("Starters", "Place a starter from Assets/_Project/Prefabs/Starters"),
                () => StarterPicker.Open());

        [InitializeOnLoadMethod]
        static void RefreshDoctorAfterEachScan() => JamKitDoctor.Scanned += () => MainToolbar.Refresh(DoctorPath);

        [MainToolbarElement(DoctorPath, defaultDockPosition = MainToolbarDockPosition.Right)]
        static MainToolbarElement Doctor()
        {
            int problems = JamKitDoctor.ProblemCount;
            return new MainToolbarButton(
                new MainToolbarContent(problems > 0 ? $"Doctor · {problems}" : "Doctor",
                    problems > 0 ? $"The Doctor found {problems} problem(s) in the JamKit setup. Click for fixes." : "Check the JamKit project setup"),
                JamKitDoctorWindow.Open);
        }

        [MainToolbarElement(BuildPath, defaultDockPosition = MainToolbarDockPosition.Right)]
        static MainToolbarElement Build()
            => new MainToolbarButton(new MainToolbarContent("Build Web", "Build a WebGL player for itch.io"), WebGLItchBuild.Build);

        static void ShowScenes(Rect rect)
        {
            var menu = new GenericMenu();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                string path = scene.path;
                menu.AddItem(new GUIContent(Path.GetFileNameWithoutExtension(path)), false, () => Open(path));
            }
            menu.DropDown(rect);
        }

        static void Open(string path)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(path);
        }

        static void SetPlayFromStart(bool enabled)
        {
            EditorSceneManager.playModeStartScene = enabled && EditorBuildSettings.scenes.Length > 0
                ? AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorBuildSettings.scenes[0].path)
                : null;
            MainToolbar.Refresh(PlayFromStartPath);
        }

        static void SetTimeScale(float scale)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(TimeServiceSO)))
                AssetDatabase.LoadAssetAtPath<TimeServiceSO>(AssetDatabase.GUIDToAssetPath(guid)).BaseScale = scale;
        }
    }
}
