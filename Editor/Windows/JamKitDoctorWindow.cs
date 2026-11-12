using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Metz.JamKit.Editor
{
    public sealed class JamKitDoctorWindow : EditorWindow
    {
        class Issue
        {
            public MessageType Severity;
            public string Message;
            public string FixLabel;
            public Action Fix;
            public Object Context;
        }

        readonly List<Issue> _issues = new();
        Vector2 _scroll;
        bool _scanned;

        public delegate void IssueReporter(MessageType severity, string message, string fixLabel = null, Action fix = null, Object context = null);

        public static readonly List<Action<IssueReporter>> ExtraScans = new();

        [MenuItem("JamKit/Doctor", priority = 1)]
        public static void Open()
        {
            var window = GetWindow<JamKitDoctorWindow>("JamKit Doctor");
            window.minSize = new Vector2(420f, 240f);
            window.Scan();
        }

        void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Rescan", EditorStyles.toolbarButton, GUILayout.Width(70f))) Scan();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Auto-Assign All", EditorStyles.toolbarButton, GUILayout.Width(110f)))
                {
                    JamKitAutoAssign.FillOpenScenes();
                    Scan();
                }
            }

            if (!_scanned) Scan();

            if (_issues.Count == 0)
            {
                EditorGUILayout.HelpBox("All green. Go make the game.", MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var issue in _issues)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.HelpBox(issue.Message, issue.Severity);
                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(90f)))
                    {
                        if (issue.Fix != null && GUILayout.Button(issue.FixLabel ?? "Fix"))
                        {
                            issue.Fix();
                            Scan();
                            GUIUtility.ExitGUI();
                        }
                        if (issue.Context != null && GUILayout.Button("Select"))
                        {
                            Selection.activeObject = issue.Context;
                            EditorGUIUtility.PingObject(issue.Context);
                        }
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        void Scan()
        {
            _scanned = true;
            _issues.Clear();
            ScanServices();
            ScanInput();
            ScanPanelSettings();
            ScanBuildScenes();
            ScanOpenScenes();
            ScanGameAsmdef();
            ScanEmbeddedPackageEdits();
            foreach (var scan in ExtraScans) scan(Add);
            Repaint();
        }

        void Add(MessageType severity, string message, string fixLabel = null, Action fix = null, Object context = null)
            => _issues.Add(new Issue { Severity = severity, Message = message, FixLabel = fixLabel, Fix = fix, Context = context });

        static readonly Type[] ServiceTypes =
        {
            typeof(TimeServiceSO), typeof(SceneServiceSO), typeof(InputServiceSO), typeof(ScreenFadeSO),
        };

        void ScanServices()
        {
            var missing = new List<string>();
            foreach (var t in ServiceTypes)
                if (AssetDatabase.FindAssets("t:" + t.Name).Length == 0)
                    missing.Add(t.Name.Replace("ServiceSO", ""));
            if (missing.Count > 0)
                Add(MessageType.Warning,
                    $"Missing service assets: {string.Join(", ", missing)}. Run the wizard to create the full set.",
                    "Run Wizard", JamProjectWizard.Run);
        }

        void ScanInput()
        {
            foreach (var input in FindAssets<InputServiceSO>())
            {
                if (input.Actions == null)
                {
                    Add(MessageType.Error, $"InputService '{input.name}' has no InputActionAsset — every mover is dead.",
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
                    Add(MessageType.Error,
                        $"InputService '{input.name}': action map '{input.GameplayMapName}' not found in '{input.Actions.name}'.",
                        context: input);
                if (input.Actions.FindActionMap(input.UIMapName, false) == null)
                    Add(MessageType.Warning,
                        $"InputService '{input.name}': UI map '{input.UIMapName}' not found — menu gamepad nav breaks.",
                        context: input);
            }
        }

        void ScanPanelSettings()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:PanelSettings"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/")) continue;
                var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
                if (ps == null || ps.themeStyleSheet != null) continue;
                Add(MessageType.Error, $"PanelSettings '{ps.name}' has no theme — UI renders unstyled.",
                    "Assign Theme", () =>
                    {
                        ps.themeStyleSheet = JamKitUI.DefaultTheme;
                        EditorUtility.SetDirty(ps);
                        AssetDatabase.SaveAssets();
                    }, ps);
            }
        }

        void ScanBuildScenes()
        {
            var wanted = new[]
            {
                "Assets/_Project/Scenes/Bootstrap.unity",
                "Assets/_Project/Scenes/Game.unity",
                "Assets/_Project/Scenes/GameOver.unity",
            };
            foreach (var path in wanted)
            {
                if (!File.Exists(path)) continue;
                bool inBuild = false;
                foreach (var s in EditorBuildSettings.scenes)
                    if (s.path == path && s.enabled) { inBuild = true; break; }
                if (inBuild) continue;
                var scenePath = path;
                Add(MessageType.Error, $"{Path.GetFileName(path)} exists but is not enabled in Build Settings.",
                    "Add To Build", () =>
                    {
                        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
                        list.RemoveAll(s => s.path == scenePath);
                        list.Add(new EditorBuildSettingsScene(scenePath, true));
                        EditorBuildSettings.scenes = list.ToArray();
                    });
            }
        }

        void ScanOpenScenes()
        {
            bool hasUIDoc = Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0
                || Object.FindObjectsByType<MenuController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0;
            bool hasEventSystem = Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0;
            if (hasUIDoc && !hasEventSystem)
                Add(MessageType.Error, "Scene has UI but no EventSystem — clicks and gamepad nav are dead.",
                    "Create EventSystem", () =>
                    {
                        var es = new GameObject("EventSystem");
                        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                        Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
                    });

            var looseObjects = new HashSet<GameObject>();
            foreach (var mb in JamKitAutoAssign.AllJamKitComponentsInOpenScenes())
                if (!PrefabUtility.IsPartOfPrefabInstance(mb.gameObject))
                    looseObjects.Add(mb.gameObject);

            if (looseObjects.Count > 0)
                Add(MessageType.Info,
                    $"{looseObjects.Count} JamKit object(s) in open scenes are not prefab instances. " +
                    "Consider making them prefabs (or starter variants) — scenes as prefab lists merge better (PILLARS.md).");
        }

        void ScanGameAsmdef()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Scripts")) return;
            if (AssetDatabase.FindAssets("t:AssemblyDefinitionAsset", new[] { "Assets/_Project/Scripts" }).Length > 0) return;
            if (AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/_Project/Scripts" }).Length == 0) return;

            Add(MessageType.Info,
                "Game scripts have no .asmdef, so they compile into Assembly-CSharp alongside the kit. " +
                "A game class in the global namespace can then shadow a JamKit type of the same name, silently. " +
                "Re-run the wizard to scaffold Assets/_Project/Scripts/Game.asmdef.",
                "Run Wizard", JamProjectWizard.Run);
        }

        void ScanEmbeddedPackageEdits()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(Health).Assembly);
            if (info == null || info.source != UnityEditor.PackageManager.PackageSource.Embedded) return;

            Add(MessageType.Info,
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
