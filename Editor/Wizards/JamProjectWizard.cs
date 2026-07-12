using System.Collections.Generic;
using System.IO;
using Ripple;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Metz.JamKit.Editor
{
    /// <summary>
    /// One-click jam scaffold. Creates the SO service assets (Audio / Time / Scene / Input / Save /
    /// Pool / Score / Timer), the Ripple variables, the AudioMixer + PanelSettings, and three scenes
    /// (Bootstrap / Game / GameOver). Every scene gets a self-contained JamKitCore (all runners) plus
    /// an EventSystem, so audio/pause/input/transitions work in each scene without a persistent root
    /// or any singletons — the runners simply register with the shared service SOs on load.
    /// </summary>
    public static class JamProjectWizard
    {
        const string ProjectRoot = "Assets/_Project";
        public const string ServicesDir = "Assets/_Project/Services";
        const string VariablesDir = "Assets/_Project/Variables";
        const string PrefabsDir = "Assets/_Project/Prefabs";

        public const string BootstrapScenePath = ProjectRoot + "/Scenes/Bootstrap.unity";
        public const string GameScenePath = ProjectRoot + "/Scenes/Game.unity";
        public const string GameOverScenePath = ProjectRoot + "/Scenes/GameOver.unity";
        public const string CorePrefabPath = PrefabsDir + "/JamKitCore.prefab";

        [MenuItem("JamKit/New Jam Project", priority = 0)]
        public static void Run()
        {
            if (!EditorUtility.DisplayDialog(
                "JamKit: New Jam Project",
                "Creates Bootstrap / Game / GameOver scenes, the SO service assets (incl. Score + Timer), " +
                "the audio mixer, the UI Toolkit panel settings, and wires each scene with a JamKitCore, " +
                "menu/pause flow, and an EventSystem for gamepad navigation.\n\nProceed?",
                "Create", "Cancel"))
                return;

            // Never silently stomp scenes the user may have built their jam in. Asset creation is
            // create-or-load (safe); scene creation is the destructive part, so it gets a choice.
            bool overwriteScenes = true;
            var existingScenes = new List<string>();
            foreach (var p in new[] { BootstrapScenePath, GameScenePath, GameOverScenePath })
                if (File.Exists(p)) existingScenes.Add(p);
            if (existingScenes.Count > 0)
            {
                int choice = EditorUtility.DisplayDialogComplex(
                    "JamKit: Scenes Already Exist",
                    "These scenes already exist:\n\n" + string.Join("\n", existingScenes) +
                    "\n\nKeep them (only missing scenes are created), or overwrite all three with fresh copies?",
                    "Keep Existing", "Cancel", "Overwrite All");
                if (choice == 1) return;
                overwriteScenes = choice == 2;
            }

            // The scene builds below open new scenes, which discards unsaved work without this prompt.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scaffold(overwriteScenes);
            EditorSceneManager.OpenScene(BootstrapScenePath);

            OfferFastPlayMode();

            EditorUtility.DisplayDialog("JamKit",
                "Setup complete.\n\n" +
                "Service SOs live in Assets/_Project/Services; JamKitCore + JamKitMenu prefabs in Assets/_Project/Prefabs\n" +
                "(edit the prefab once, every scene updates).\n\n" +
                "Press Play: Start → Settings → Game (pause with Esc) → GameOver all work end-to-end.", "OK");
        }

        /// <summary>
        /// The wizard's work without its dialogs, for tools that scaffold on demand (sample setup).
        /// Create-or-load throughout; existing scenes are replaced only when <paramref name="overwriteScenes"/>.
        /// Callers own the save-modified-scenes prompt, and a throwaway scene is left open at the
        /// end (prefab building) — open a real one afterwards.
        /// </summary>
        public static void Scaffold(bool overwriteScenes)
        {
            EnsureFolders();

            var mixerPath = $"{ProjectRoot}/Audio/Resources/JamKitMixer.mixer";
            AudioMixerCreator.CreateAt(mixerPath);
            var mixer = AssetDatabase.LoadAssetAtPath<UnityEngine.Audio.AudioMixer>(mixerPath);

            var panelSettings = PanelSettingsCreator.CreateAt($"{PanelSettingsCreator.DefaultDir}/{PanelSettingsCreator.DefaultName}.asset");

            // Ripple volume variables (0..1) and score/timer mirrors (HUD-bindable).
            var master = CreateOrLoadFloatVar($"{VariablesDir}/MasterVolume.asset");
            var music  = CreateOrLoadFloatVar($"{VariablesDir}/MusicVolume.asset");
            var sfx    = CreateOrLoadFloatVar($"{VariablesDir}/SfxVolume.asset");
            var scoreVar = CreateOrLoadFloatVar($"{VariablesDir}/Score.asset", 0f, 999999f, 0f);
            var highVar  = CreateOrLoadFloatVar($"{VariablesDir}/HighScore.asset", 0f, 999999f, 0f);
            var timeVar  = CreateOrLoadFloatVar($"{VariablesDir}/Timer.asset", 0f, 999999f, 0f);

            // Service SOs.
            var audio = CreateOrLoadSO<AudioServiceSO>($"{ServicesDir}/AudioService.asset", a =>
            {
                a.Mixer = mixer;
                a.MasterVolume = master;
                a.MusicVolume = music;
                a.SfxVolume = sfx;
            });
            var time  = CreateOrLoadSO<TimeServiceSO>($"{ServicesDir}/TimeService.asset");
            var scene = CreateOrLoadSO<SceneServiceSO>($"{ServicesDir}/SceneService.asset");
            var save  = CreateOrLoadSO<SaveServiceSO>($"{ServicesDir}/SaveService.asset");
            var pool  = CreateOrLoadSO<PoolServiceSO>($"{ServicesDir}/PoolService.asset");
            var timer = CreateOrLoadSO<TimerServiceSO>($"{ServicesDir}/TimerService.asset", t => { t.TimeVariable = timeVar; });
            var score = CreateOrLoadSO<ScoreServiceSO>($"{ServicesDir}/ScoreService.asset", sc =>
            {
                sc.SaveService = save;
                sc.ScoreVariable = scoreVar;
                sc.HighScoreVariable = highVar;
            });

            var actions = Resources.Load<InputActionAsset>("JamKitInput");
            var input = CreateOrLoadSO<InputServiceSO>($"{ServicesDir}/InputService.asset", i => { i.Actions = actions; });

            AssetDatabase.SaveAssets();

            // Prefab-first: JamKitCore + Menu are prefab assets instanced into each scene, so a
            // later fix or addition propagates everywhere at once. Built in a throwaway empty
            // scene so the user's scene never gets temp objects; we open Bootstrap at the end.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var corePrefab = CreateOrLoadCorePrefab(audio, time, scene, pool, timer);
            var menuPrefab = CreateOrLoadMenuPrefab(audio, time, scene, input);

            // Bootstrap is built last so it's the scene left open for the user.
            if (overwriteScenes || !File.Exists(GameScenePath))
                CreateGameScene(GameScenePath, corePrefab, menuPrefab, input);
            if (overwriteScenes || !File.Exists(GameOverScenePath))
                CreateGameOverScene(GameOverScenePath, corePrefab, scene, score, panelSettings);
            if (overwriteScenes || !File.Exists(BootstrapScenePath))
                CreateBootstrapScene(BootstrapScenePath, corePrefab, menuPrefab);

            AddScenesToBuildSettings(new[] { BootstrapScenePath, GameScenePath, GameOverScenePath });
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Domain reload off = near-instant Play. Safe because every JamKit SO resets its mutable
        /// state per session (the 0.4.0 reset guards exist exactly for this).
        /// </summary>
        static void OfferFastPlayMode()
        {
            if (EditorSettings.enterPlayModeOptionsEnabled) return;
            if (!EditorUtility.DisplayDialog("JamKit: Fast Play Mode",
                "Enable fast enter-play-mode (disables domain reload)?\n\n" +
                "Play starts near-instantly. JamKit's services are built for it; your own statics " +
                "must reset themselves (or use [RuntimeInitializeOnLoadMethod]).\n\n" +
                "You can change this any time in Project Settings > Editor > Enter Play Mode Settings.",
                "Enable", "Skip"))
                return;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        }

        // -------------------- asset helpers --------------------

        static void EnsureFolders()
        {
            string[] folders = {
                "Assets/_Project",
                "Assets/_Project/Scenes",
                "Assets/_Project/Scripts",
                "Assets/_Project/Prefabs",
                "Assets/_Project/Audio",
                "Assets/_Project/Audio/Resources",
                "Assets/_Project/UI",
                "Assets/_Project/UI/Resources",
                "Assets/_Project/Art",
                "Assets/_Project/Services",
                "Assets/_Project/Variables",
            };
            foreach (var f in folders)
            {
                if (!AssetDatabase.IsValidFolder(f))
                {
                    var parent = Path.GetDirectoryName(f).Replace("\\", "/");
                    var leaf = Path.GetFileName(f);
                    AssetDatabase.CreateFolder(parent, leaf);
                }
            }
        }

        static T CreateOrLoadSO<T>(string path, System.Action<T> configure = null) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var inst = ScriptableObject.CreateInstance<T>();
            configure?.Invoke(inst);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(inst, path);
            return inst;
        }

        static FloatVariableSO CreateOrLoadFloatVar(string path, float min = 0f, float max = 1f, float initial = 1f)
        {
            var existing = AssetDatabase.LoadAssetAtPath<FloatVariableSO>(path);
            if (existing != null) return existing;
            var inst = ScriptableObject.CreateInstance<FloatVariableSO>();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(inst, path);

            // Set min/max/initial on the NumericalVariable<float> base via SerializedObject so the
            // values stick across domain reload.
            var so = new SerializedObject(inst);
            TrySetFloat(so, "min", min);
            TrySetFloat(so, "max", max);
            TrySetFloat(so, "_initialValue", initial);
            so.ApplyModifiedPropertiesWithoutUndo();
            return inst;
        }

        static void TrySetFloat(SerializedObject so, string name, float v)
        {
            var prop = so.FindProperty(name);
            if (prop != null && prop.propertyType == SerializedPropertyType.Float) prop.floatValue = v;
        }

        // -------------------- shared scene pieces --------------------

        /// <summary>
        /// The JamKitCore prefab: every service runner + fade overlay, plus the juice/UI scene
        /// counterparts (FloatingTextLayer, Toast — each on its own child because each hosts its
        /// own UIDocument at runtime). Load-or-create so user customizations survive re-running.
        /// </summary>
        static GameObject CreateOrLoadCorePrefab(AudioServiceSO audio, TimeServiceSO time, SceneServiceSO scene, PoolServiceSO pool, TimerServiceSO timer)
        {
            string path = CorePrefabPath;
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var core = new GameObject("JamKitCore");
            core.AddComponent<AudioServiceRunner>().Service = audio;
            core.AddComponent<TimeServiceRunner>().Service = time;
            core.AddComponent<PoolServiceRunner>().Service = pool;
            core.AddComponent<TimerServiceRunner>().Service = timer;
            var fade = core.AddComponent<FadeOverlay>();
            var sceneRunner = core.AddComponent<SceneServiceRunner>();
            sceneRunner.Service = scene;
            sceneRunner.Fade = fade;

            var textLayer = new GameObject("FloatingTextLayer");
            textLayer.transform.SetParent(core.transform, false);
            textLayer.AddComponent<FloatingTextLayer>();

            var toast = new GameObject("Toast");
            toast.transform.SetParent(core.transform, false);
            toast.AddComponent<Toast>();

            Directory.CreateDirectory(PrefabsDir);
            var prefab = PrefabUtility.SaveAsPrefabAsset(core, path);
            Object.DestroyImmediate(core);
            return prefab;
        }

        /// <summary>Menu prefab wired to the services; scenes instance it and override InitialView.</summary>
        static GameObject CreateOrLoadMenuPrefab(AudioServiceSO audio, TimeServiceSO time, SceneServiceSO scene, InputServiceSO input)
        {
            string path = PrefabsDir + "/JamKitMenu.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var refs = MenuCanvasBuilder.Build();
            refs.Controller.AudioService = audio;
            refs.Controller.TimeService = time;
            refs.Controller.SceneService = scene;
            refs.Controller.InputService = input;
            refs.Controller.InitialView = MenuController.View.Start;

            Directory.CreateDirectory(PrefabsDir);
            var prefab = PrefabUtility.SaveAsPrefabAsset(refs.Root, path);
            Object.DestroyImmediate(refs.Root);
            return prefab;
        }

        static GameObject Instance(GameObject prefab, string name = null)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (!string.IsNullOrEmpty(name)) go.name = name;
            return go;
        }

        internal static void CreateEventSystem()
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        internal static void CreateDirectionalLight()
        {
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        internal static void CreateCamera(bool cinemachine)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.transform.position = new Vector3(0, 1, -10);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.09f);
            if (cinemachine)
            {
                camGo.AddComponent<CinemachineBrain>();
                // NOT CinemachineImpulseListener: that's a vcam extension and never runs on a
                // plain camera — shake would silently do nothing until a CinemachineCamera exists.
                camGo.AddComponent<CinemachineExternalImpulseListener>();
            }
            camGo.AddComponent<AudioListener>();
        }

        // -------------------- scenes --------------------

        static void CreateBootstrapScene(string path, GameObject corePrefab, GameObject menuPrefab)
        {
            var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera(cinemachine: true);
            Instance(corePrefab);
            Instance(menuPrefab); // prefab default InitialView = Start
            CreateEventSystem();

            EditorSceneManager.MarkSceneDirty(s);
            EditorSceneManager.SaveScene(s, path);
        }

        static void CreateGameScene(string path, GameObject corePrefab, GameObject menuPrefab, InputServiceSO input)
        {
            var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera(cinemachine: true);
            CreateDirectionalLight();

            Instance(corePrefab);

            // Hidden pause layer: Esc opens pause, gameplay input goes live (InitialView = None
            // as an instance override; PauseController is an added component on the instance).
            var menuGo = Instance(menuPrefab, "JamKitGameplayUI");
            var menu = menuGo.GetComponent<MenuController>();
            menu.InitialView = MenuController.View.None;
            var pause = menuGo.AddComponent<PauseController>();
            pause.Menu = menu;
            pause.InputService = input;

            CreateEventSystem();

            EditorSceneManager.MarkSceneDirty(s);
            EditorSceneManager.SaveScene(s, path);
        }

        static void CreateGameOverScene(string path, GameObject corePrefab,
            SceneServiceSO scene, ScoreServiceSO score, PanelSettings panelSettings)
        {
            var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera(cinemachine: false);
            Instance(corePrefab);

            var uiGo = new GameObject("GameOverUI");
            var doc = uiGo.AddComponent<UIDocument>();
            doc.panelSettings = panelSettings;
            var over = uiGo.AddComponent<GameOverController>();
            over.SceneService = scene;
            over.ScoreService = score;
            over.RetrySceneName = "Game";
            over.MainMenuSceneName = "Bootstrap";

            CreateEventSystem();

            EditorSceneManager.MarkSceneDirty(s);
            EditorSceneManager.SaveScene(s, path);
        }

        static void AddScenesToBuildSettings(string[] scenePaths)
        {
            var existing = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var p in scenePaths)
            {
                bool already = false;
                foreach (var sc in existing) if (sc.path == p) { already = true; break; }
                if (!already) existing.Add(new EditorBuildSettingsScene(p, true));
            }
            EditorBuildSettings.scenes = existing.ToArray();
        }
    }
}
