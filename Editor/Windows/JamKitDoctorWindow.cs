using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Metz.JamKit.Editor
{
    /// <summary>
    /// Jam-day insurance: one window that checks the PROJECT-SHAPE failure points (mixer params
    /// not exposed, PanelSettings without a theme, scenes missing from Build Settings, no
    /// EventSystem, HitStop without a TimeServiceRunner) and offers a Fix button wherever the
    /// fix is unambiguous. Per-field null references are not scanned here anymore — [Required]
    /// attributes surface them in the inspector and project-wide in Odin Validator, and
    /// auto-assign fills them; the toolbar button runs that fill on demand.
    /// </summary>
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

        /// <summary>Signature of <see cref="Add"/> — what an external scan reports through.</summary>
        public delegate void IssueReporter(MessageType severity, string message, string fixLabel = null, Action fix = null, Object context = null);

        /// <summary>
        /// Optional integrations (FMOD, …) append extra checks here from [InitializeOnLoad];
        /// they run on every scan and report through the supplied <see cref="IssueReporter"/>.
        /// </summary>
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
            ScanAudio();
            ScanInput();
            ScanPanelSettings();
            ScanBuildScenes();
            ScanOpenScenes();
            foreach (var scan in ExtraScans) scan(Add);
            Repaint();
        }

        void Add(MessageType severity, string message, string fixLabel = null, Action fix = null, Object context = null)
            => _issues.Add(new Issue { Severity = severity, Message = message, FixLabel = fixLabel, Fix = fix, Context = context });

        // ------------------------------------------------------------------ checks

        static readonly Type[] ServiceTypes =
        {
            typeof(TimeServiceSO), typeof(SceneServiceSO), typeof(InputServiceSO),
            typeof(SaveServiceSO), typeof(PoolServiceSO),
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

        // Unity-audio path only: with FMOD installed the scaffold creates no AudioServiceSO, so
        // this loop simply finds nothing (the FMOD ExtraScan covers that backend).
        void ScanAudio()
        {
            foreach (var audio in FindAssets<AudioServiceSO>())
            {
                if (audio.Mixer == null)
                {
                    Add(MessageType.Warning, $"AudioService '{audio.name}' has no mixer — volume sliders will do nothing.",
                        "Copy Template", () =>
                        {
                            var mixer = TemplateAssets.EnsureMixer();
                            if (mixer != null)
                            {
                                audio.Mixer = mixer;
                                EditorUtility.SetDirty(audio);
                                AssetDatabase.SaveAssets();
                            }
                        }, audio);
                    continue;
                }

                foreach (var param in new[] { audio.MasterParam, audio.MusicParam, audio.SfxParam })
                {
                    if (!audio.Mixer.GetFloat(param, out _))
                    {
                        var mixerPath = AssetDatabase.GetAssetPath(audio.Mixer);
                        Add(MessageType.Error,
                            $"Mixer '{audio.Mixer.name}' does not expose '{param}' — volume sliders will not work.",
                            "Repair Mixer", () =>
                            {
                                var repaired = TemplateAssets.RepairMixer(mixerPath);
                                if (repaired != null)
                                {
                                    audio.Mixer = repaired;
                                    EditorUtility.SetDirty(audio);
                                    AssetDatabase.SaveAssets();
                                }
                            }, audio.Mixer);
                        break; // repair fixes all three at once
                    }
                }

                if (audio.MasterVolume == null || audio.MusicVolume == null || audio.SfxVolume == null)
                    Add(MessageType.Warning,
                        $"AudioService '{audio.name}' is missing Ripple volume variables — settings sliders won't bind.",
                        context: audio);
            }
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
                if (!path.StartsWith("Assets/")) continue; // leave package-shipped assets alone
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

            bool hasHitStop = false;
            bool hasTimeRunner = Object.FindObjectsByType<TimeServiceRunner>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0;
            var looseObjects = new HashSet<GameObject>();

            foreach (var mb in JamKitAutoAssign.AllJamKitComponentsInOpenScenes())
            {
                if (mb is HitStop) hasHitStop = true;
                if (!PrefabUtility.IsPartOfPrefabInstance(mb.gameObject))
                    looseObjects.Add(mb.gameObject);
            }

            if (hasHitStop && !hasTimeRunner)
                Add(MessageType.Warning, "HitStop present but no TimeServiceRunner in the scene — freeze-frames won't run. Add a JamKitCore (wizard) or a TimeServiceRunner.");

            // Pillar nudge, not an error: scenes as lists of prefab instances merge better and
            // propagate fixes. No auto-fix — deciding what becomes a prefab is design work.
            if (looseObjects.Count > 0)
                Add(MessageType.Info,
                    $"{looseObjects.Count} JamKit object(s) in open scenes are not prefab instances. " +
                    "Consider making them prefabs (or starter variants) — scenes as prefab lists merge better (PILLARS.md).");
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
