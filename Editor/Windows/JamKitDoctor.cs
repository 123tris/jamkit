using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Metz.JamKit.Editor
{
    public static class JamKitDoctor
    {
        public sealed class Issue
        {
            public MessageType Severity;
            public string Message;
            public string FixLabel;
            public Action Fix;
            public Object Context;
        }

        const string RuntimeUI = "Packages/com.metz.jamkit/Runtime/UI";

        static readonly Type[] ServiceTypes =
        {
            typeof(TimeServiceSO), typeof(SceneServiceSO), typeof(InputServiceSO), typeof(ScreenFadeSO),
        };

        static ThemeStyleSheet Theme => AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>($"{RuntimeUI}/JamKitDefaultTheme.tss");

        static VisualTreeAsset Overlay(string name) => AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{RuntimeUI}/Overlays/{name}.uxml");

        static PanelSettings OverlayPanel(string name)
            => AssetDatabase.LoadAssetAtPath<PanelSettings>($"{RuntimeUI}/Overlays/{name}PanelSettings.asset");

        public static List<Issue> Scan() => Scan(openScenes: true);

        internal static List<Issue> ScanProject() => Scan(openScenes: false);

        static List<Issue> Scan(bool openScenes)
        {
            var issues = new List<Issue>();
            ScanServices(issues);
            ScanInput(issues);
            ScanPanelSettings(issues);
            ScanBuildScenes(issues);
            if (openScenes) ScanOpenScenes(issues);
            ScanGameAsmdef(issues);
            ScanEmbeddedPackageEdits(issues);
            return issues;
        }

        static void Add(List<Issue> issues, MessageType severity, string message, string fixLabel = null, Action fix = null, Object context = null)
            => issues.Add(new Issue { Severity = severity, Message = message, FixLabel = fixLabel, Fix = fix, Context = context });

        static void ScanServices(List<Issue> issues)
        {
            var missing = new List<string>();
            foreach (var t in ServiceTypes)
                if (AssetDatabase.FindAssets("t:" + t.Name).Length == 0)
                    missing.Add(t.Name.Replace("ServiceSO", ""));
            if (missing.Count > 0)
                Add(issues, MessageType.Warning,
                    $"Missing service assets: {string.Join(", ", missing)}. Run the wizard to create the full set.",
                    "Run Wizard", JamProjectWizard.Run);

            foreach (var fade in FindAssets<ScreenFadeSO>())
                if (fade.Document == null || fade.Panel == null)
                    Add(issues, MessageType.Error, $"ScreenFade '{fade.name}' has no overlay document or panel, so fades never show.",
                        "Use JamKit's", () =>
                        {
                            if (fade.Document == null) fade.Document = Overlay("JamKitFade");
                            if (fade.Panel == null) fade.Panel = OverlayPanel("JamKitFade");
                            EditorUtility.SetDirty(fade);
                            AssetDatabase.SaveAssets();
                        }, fade);
        }

        static void ScanInput(List<Issue> issues)
        {
            foreach (var input in FindAssets<InputServiceSO>())
            {
                if (input.Actions == null)
                {
                    Add(issues, MessageType.Error, $"InputService '{input.name}' has no InputActionAsset — every mover is dead.",
                        "Assign JamKitInput", () =>
                        {
                            var actions = Resources.Load<UnityEngine.InputSystem.InputActionAsset>("JamKitInput");
                            if (actions == null) { Debug.LogWarning("[JamKit] JamKitInput not found in Resources."); return; }
                            input.Actions = actions;
                            EditorUtility.SetDirty(input);
                            AssetDatabase.SaveAssets();
                        }, input);
                    continue;
                }
                if (input.Actions.FindActionMap(input.GameplayMapName, false) == null)
                    Add(issues, MessageType.Error,
                        $"InputService '{input.name}': action map '{input.GameplayMapName}' not found in '{input.Actions.name}'.",
                        context: input);
                if (input.Actions.FindActionMap(input.UIMapName, false) == null)
                    Add(issues, MessageType.Warning,
                        $"InputService '{input.name}': UI map '{input.UIMapName}' not found — menu gamepad nav breaks.",
                        context: input);
            }
        }

        static void ScanPanelSettings(List<Issue> issues)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:PanelSettings"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/")) continue;
                var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
                if (ps == null || ps.themeStyleSheet != null) continue;
                Add(issues, MessageType.Error, $"PanelSettings '{ps.name}' has no theme — UI renders unstyled.",
                    "Assign Theme", () =>
                    {
                        ps.themeStyleSheet = Theme;
                        EditorUtility.SetDirty(ps);
                        AssetDatabase.SaveAssets();
                    }, ps);
            }
        }

        static void ScanBuildScenes(List<Issue> issues)
        {
            foreach (var path in ProjectTemplate.Scenes)
            {
                if (!File.Exists(path)) continue;
                bool inBuild = false;
                foreach (var s in EditorBuildSettings.scenes)
                    if (s.path == path && s.enabled) { inBuild = true; break; }
                if (inBuild) continue;
                var scenePath = path;
                Add(issues, MessageType.Error, $"{Path.GetFileName(path)} exists but is not enabled in Build Settings.",
                    "Add To Build", () =>
                    {
                        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
                        list.RemoveAll(s => s.path == scenePath);
                        list.Add(new EditorBuildSettingsScene(scenePath, true));
                        EditorBuildSettings.scenes = list.ToArray();
                    });
            }
        }

        static void ScanOpenScenes(List<Issue> issues)
        {
            var panels = Object.FindObjectsByType<PanelRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            bool hasUIDoc = panels.Length > 0
                || Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0;
            bool hasEventSystem = Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0;
            if (hasUIDoc && !hasEventSystem)
                Add(issues, MessageType.Error, "Scene has UI but no EventSystem — clicks and gamepad nav are dead.",
                    "Create EventSystem", () =>
                    {
                        var es = new GameObject("EventSystem");
                        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                        Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
                    });

            foreach (var panel in panels)
            {
                if (panel.visualTreeAsset != null && panel.panelSettings != null) continue;
                var overlay = panel.TryGetComponent(out Toast _) ? "JamKitToast" : panel.TryGetComponent(out DebugPanel _) ? "JamKitDebug" : null;
                Add(issues, MessageType.Error, $"Panel Renderer on '{panel.name}' has no document or no panel settings, so it shows nothing.",
                    overlay == null ? null : "Use JamKit's", overlay == null ? null : () =>
                    {
                        Undo.RecordObject(panel, "Use JamKit Overlay");
                        if (panel.panelSettings == null) panel.panelSettings = OverlayPanel(overlay);
                        if (panel.visualTreeAsset == null) panel.visualTreeAsset = Overlay(overlay);
                    }, panel.gameObject);
            }

            var looseObjects = new HashSet<GameObject>();
            foreach (var mb in JamKitAutoAssign.AllJamKitComponentsInOpenScenes())
                if (!PrefabUtility.IsPartOfPrefabInstance(mb.gameObject))
                    looseObjects.Add(mb.gameObject);

            if (looseObjects.Count > 0)
                Add(issues, MessageType.Info,
                    $"{looseObjects.Count} JamKit object(s) in open scenes are not prefab instances. " +
                    "Consider making them prefabs (or starter variants) — scenes as prefab lists merge better (PILLARS.md).");
        }

        static void ScanGameAsmdef(List<Issue> issues)
        {
            if (!AssetDatabase.IsValidFolder(ProjectTemplate.ScriptsFolder)) return;
            if (AssetDatabase.FindAssets("t:AssemblyDefinitionAsset", new[] { ProjectTemplate.ScriptsFolder }).Length > 0) return;
            if (AssetDatabase.FindAssets("t:MonoScript", new[] { ProjectTemplate.ScriptsFolder }).Length == 0) return;

            Add(issues, MessageType.Info,
                "Game scripts have no .asmdef, so they compile into Assembly-CSharp alongside the kit. " +
                "A game class in the global namespace can then shadow a JamKit type of the same name, silently. " +
                $"Re-run the wizard to scaffold {ProjectTemplate.ScriptsFolder}/Game.asmdef.",
                "Run Wizard", JamProjectWizard.Run);
        }

        static void ScanEmbeddedPackageEdits(List<Issue> issues)
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(Health).Assembly);
            if (info == null || info.source != UnityEditor.PackageManager.PackageSource.Embedded) return;

            Add(issues, MessageType.Info,
                $"JamKit is embedded at {info.resolvedPath.Replace('\\', '/')} (version {info.version}), so edits to it are " +
                "project-local and invisible to upstream. That's a legitimate jam move — but bump the version to a " +
                "'-fork.N' suffix so two different trees never both claim the same version, and see UPSTREAMING.md " +
                "for getting the good parts back.");
        }

        static IEnumerable<T> FindAssets<T>() where T : ScriptableObject
        {
            foreach (var guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) yield return asset;
            }
        }
    }
}
