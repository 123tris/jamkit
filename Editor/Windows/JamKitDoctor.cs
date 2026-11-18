using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Metz.JamKit.Editor
{
    public static partial class JamKitDoctor
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
            ScanRipple(issues);
            ScanServices(issues);
            ScanInput(issues);
            ScanPanelSettings(issues);
            ScanBuildScenes(issues);
            ScanScenesWithoutCore(issues);
            ScanGameFlows(issues);
            ScanAudioOutputs(issues);
            if (openScenes)
            {
                ScanOpenScenes(issues);
                ScanImpulseListeners(issues);
                ScanInitialViews(issues);
            }
            ScanRequired(issues, openScenes);
            ScanGameAsmdef(issues);
            ScanEmbeddedPackageEdits(issues);
            return issues;
        }

        static void Add(List<Issue> issues, MessageType severity, string message, string fixLabel = null, Action fix = null, Object context = null)
            => issues.Add(new Issue { Severity = severity, Message = message, FixLabel = fixLabel, Fix = fix, Context = context });

        static void ScanRipple(List<Issue> issues)
        {
            if (ProjectTemplate.RippleProblem is string problem) Add(issues, MessageType.Error, problem);
        }

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
                if (input.Actions.FindActionMap(input.UIMapName, false) == null)
                    Add(issues, MessageType.Error,
                        $"InputService '{input.name}': '{input.Actions.name}' has no map named '{input.UIMapName}', so switching between menus and gameplay throws.",
                        context: input);
                if (input.Actions.actionMaps.All(map => map.name == input.UIMapName))
                    Add(issues, MessageType.Error,
                        $"InputService '{input.name}': '{input.Actions.name}' has no map besides '{input.UIMapName}', so nothing reads gameplay input.",
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
                        EditorBuildSettingsScene[] build = EditorBuildSettings.scenes;
                        foreach (var s in build)
                            if (s.path == scenePath) s.enabled = true;
                        EditorBuildSettings.scenes = build;
                        ProjectTemplate.RegisterScenes();
                    });
            }
        }

        static void ScanOpenScenes(List<Issue> issues)
        {
            var panels = Object.FindObjectsByType<PanelRenderer>(FindObjectsInactive.Include);
            bool hasUIDoc = panels.Length > 0
                || Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include).Length > 0;
            bool hasEventSystem = Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include).Length > 0;
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

            foreach (FirstPersonLook look in Object.FindObjectsByType<FirstPersonLook>(FindObjectsInactive.Include))
            {
                if (look.YawTransform != look.transform || !look.TryGetComponent(out Mover3D mover) || !mover.RotateToFaceMove) continue;
                Add(issues, MessageType.Warning,
                    $"'{look.name}' turns with FirstPersonLook, and its Mover3D also turns it to face the movement, so the two fight.",
                    "Untick Rotate To Face Move", () =>
                    {
                        Undo.RecordObject(mover, "Untick Rotate To Face Move");
                        mover.RotateToFaceMove = false;
                    }, look.gameObject);
            }
        }

        static void ScanRequired(List<Issue> issues, bool openScenes)
        {
            if (openScenes)
                foreach (MonoBehaviour component in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
                {
                    if (component == null) continue;
                    List<string> missing = RequiredReferences.Missing(component);
                    MonoBehaviour source = PrefabUtility.GetCorrespondingObjectFromSource(component);
                    if (source != null && IsCheckedPrefab(AssetDatabase.GetAssetPath(source)))
                        missing = missing.Except(RequiredReferences.Missing(source)).ToList();
                    ReportRequired(issues, MessageType.Error, missing, component.gameObject,
                        fields => $"'{component.name}' ({component.GetType().Name}) is missing {fields}, which it needs to work.");
                }

            if (!AssetDatabase.IsValidFolder(ProjectTemplate.Root)) return;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { ProjectTemplate.Root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                bool starter = !IsCheckedPrefab(path);
                foreach (MonoBehaviour component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component == null) continue;
                    string type = component.GetType().Name;
                    ReportRequired(issues, starter ? MessageType.Info : MessageType.Error, RequiredReferences.Missing(component), prefab,
                        fields => starter
                            ? $"Starter {prefab.name} leaves {fields} on its {type} empty, for you to fill in on each copy you place."
                            : $"'{component.name}' ({type}) in prefab {prefab.name} is missing {fields}, which it needs to work.");
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { ProjectTemplate.Root }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null) continue;
                ReportRequired(issues, MessageType.Error, RequiredReferences.Missing(asset), asset,
                    fields => $"'{asset.name}' is missing {fields}, which it needs to work.");
            }
        }

        static bool IsCheckedPrefab(string path)
            => path.StartsWith(ProjectTemplate.Root + "/") && !path.StartsWith(ProjectTemplate.StartersFolder + "/");

        static void ReportRequired(List<Issue> issues, MessageType severity, List<string> missing, Object context, Func<string, string> message)
        {
            if (missing.Count > 0) Add(issues, severity, message(string.Join(", ", missing)), context: context);
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
