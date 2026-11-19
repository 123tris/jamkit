using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Metz.JamKit.Editor
{
    public static partial class JamKitDoctor
    {
        static void ScanAudioOutputs(List<Issue> issues)
        {
            foreach (ClipSound sound in FindAssets<ClipSound>())
                if (sound.Output == null)
                    AddOutputIssue(issues, sound, "SFX", group => sound.Output = group);
            foreach (ClipMusic music in FindAssets<ClipMusic>())
                if (music.Output == null)
                    AddOutputIssue(issues, music, "Music", group => music.Output = group);
        }

        static void AddOutputIssue(List<Issue> issues, ScriptableObject asset, string groupName, System.Action<AudioMixerGroup> assign)
        {
            AudioMixerGroup group = MixerGroup(groupName);
            Add(issues, MessageType.Warning, $"'{asset.name}' has no Output, so the volume sliders don't reach it.",
                group == null ? null : $"Use the {groupName} group", group == null ? null : () =>
                {
                    Undo.RecordObject(asset, $"Use the {groupName} group");
                    assign(group);
                    EditorUtility.SetDirty(asset);
                }, asset);
        }

        static AudioMixerGroup MixerGroup(string groupName)
        {
            AudioMixerGroup[] groups = AssetDatabase.FindAssets("t:AudioMixer")
                .Select(guid => AssetDatabase.LoadAssetAtPath<AudioMixer>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(mixer => mixer != null)
                .SelectMany(mixer => mixer.FindMatchingGroups(groupName))
                .Where(group => group.name == groupName)
                .ToArray();
            return groups.Length == 1 ? groups[0] : null;
        }

        static void ScanScenesWithoutCore(List<Issue> issues)
        {
            bool canAdd = File.Exists(ProjectTemplate.CorePrefab);
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled || !File.Exists(scene.path)) continue;
                if (AssetDatabase.GetDependencies(scene.path, true).Any(path => path.EndsWith("/" + nameof(GameFlowDriver) + ".cs"))) continue;

                string scenePath = scene.path;
                Add(issues, MessageType.Warning,
                    $"{Path.GetFileNameWithoutExtension(scenePath)} has no JamKitCore, so Game Flow transitions, the volume settings, the DebugPanel and toasts don't work in it.",
                    canAdd ? "Add JamKitCore" : null, canAdd ? () => AddCore(scenePath) : null,
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath));
            }
        }

        static void AddCore(string scenePath)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool wasOpen = scene.isLoaded;
            if (!wasOpen) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            var core = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ProjectTemplate.CorePrefab), scene);
            Undo.RegisterCreatedObjectUndo(core, "Add JamKitCore");

            if (wasOpen)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                return;
            }
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.CloseScene(scene, true);
        }

        static void ScanGameFlows(List<Issue> issues)
        {
            var inBuild = new HashSet<string>(EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => Path.GetFileNameWithoutExtension(scene.path)));

            foreach (GameFlow flow in FindAssets<GameFlow>())
                foreach (GameFlow.Transition transition in flow.Transitions)
                {
                    string trigger = transition.On == null ? "its event" : transition.On.name;
                    if (transition.To.HasValue && !inBuild.Contains(transition.To.Name))
                        AddSceneIssue(issues, MessageType.Error, flow, transition.To.Name,
                            $"Game Flow '{flow.name}' loads {transition.To.Name} on {trigger}, but {transition.To.Name} isn't enabled in Build Settings, so the load fails.");
                    if (transition.From.HasValue && !inBuild.Contains(transition.From.Name))
                        AddSceneIssue(issues, MessageType.Warning, flow, transition.From.Name,
                            $"Game Flow '{flow.name}' listens for {trigger} only in {transition.From.Name}, which isn't enabled in Build Settings, so that transition never runs.");
                }
        }

        static void AddSceneIssue(List<Issue> issues, MessageType severity, GameFlow flow, string sceneName, string message)
        {
            string[] matches = AssetDatabase.FindAssets($"t:SceneAsset {sceneName}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Path.GetFileNameWithoutExtension(path) == sceneName)
                .ToArray();
            if (matches.Length != 1)
            {
                Add(issues, severity, message, context: flow);
                return;
            }

            string scenePath = matches[0];
            Add(issues, severity, message, "Add To Build", () =>
            {
                List<EditorBuildSettingsScene> build = EditorBuildSettings.scenes.ToList();
                EditorBuildSettingsScene existing = build.FirstOrDefault(scene => scene.path == scenePath);
                if (existing != null) existing.enabled = true;
                else build.Add(new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = build.ToArray();
            }, flow);
        }

        static void ScanImpulseListeners(List<Issue> issues)
        {
            if (Object.FindObjectsByType<CameraShake>(FindObjectsInactive.Include).Length == 0) return;

            CinemachineCamera[] cameras = Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include);
            bool listened = cameras.Any(camera => camera.TryGetComponent(out CinemachineImpulseListener _))
                            || Object.FindObjectsByType<CinemachineExternalImpulseListener>(FindObjectsInactive.Include).Length > 0;
            if (listened) return;

            if (cameras.Length > 0)
            {
                Add(issues, MessageType.Warning,
                    "A CameraShake sends impulses, but no Cinemachine camera in the open scenes listens for them, so nothing shakes.",
                    "Add Impulse Listeners", () =>
                    {
                        foreach (CinemachineCamera camera in cameras)
                            Undo.AddComponent<CinemachineImpulseListener>(camera.gameObject);
                    }, cameras[0].gameObject);
                return;
            }

            Camera main = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
            Add(issues, MessageType.Warning,
                "A CameraShake sends impulses, but no camera in the open scenes listens for them, so nothing shakes.",
                main == null ? null : "Add Listener To Camera",
                main == null ? null : () => Undo.AddComponent<CinemachineExternalImpulseListener>(main.gameObject),
                main == null ? null : main.gameObject);
        }

        static void ScanInitialViews(List<Issue> issues)
        {
            bool hasPlayer = Object.FindObjectsByType<Mover2D>(FindObjectsInactive.Include).Length > 0
                             || Object.FindObjectsByType<Mover3D>(FindObjectsInactive.Include).Length > 0
                             || Object.FindObjectsByType<FirstPersonLook>(FindObjectsInactive.Include).Length > 0;
            if (!hasPlayer) return;

            foreach (MenuController menu in Object.FindObjectsByType<MenuController>(FindObjectsInactive.Include))
            {
                if (menu.InitialView == null || !menu.InitialView.SwitchesToUIInput) continue;
                Add(issues, MessageType.Info,
                    $"'{menu.name}' opens on {menu.InitialView.name}, which takes UI input, so the movers in this scene get no input until it closes. " +
                    "A gameplay scene's menu usually starts with no view.",
                    "Start With No View", () =>
                    {
                        Undo.RecordObject(menu, "Start With No View");
                        menu.InitialView = null;
                    }, menu.gameObject);
            }
        }
    }
}
