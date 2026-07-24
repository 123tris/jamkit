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
    /// One-click jam scaffold. Creates the SO service assets (Time / Scene / Input / Save / Pool,
    /// plus Unity-mixer Audio only when FMOD is absent — with FMOD installed, audio is driven
    /// directly through FMOD instead), the Ripple variables (score + high score +
    /// timer as plain variables — no service; volume + high score marked persistent), the template
    /// PanelSettings (+ mixer on the Unity-audio path), the starter prefab library, and three
    /// scenes (Bootstrap / Game / GameOver). Every scene gets a self-contained JamKitCore (all
    /// runners + HighScoreTracker + DebugPanel) plus an EventSystem, so audio/pause/input/
    /// transitions work in each scene without a persistent root or any singletons — the runners
    /// simply register with the shared service SOs on load.
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
        public const string MenuPrefabPath = PrefabsDir + "/JamKitMenu.prefab";

        /// <summary>
        /// Optional integrations (FMOD, …) append extra scaffold steps here from
        /// [InitializeOnLoad]; they run at the end of every <see cref="Scaffold"/> — wizard and
        /// one-click sample setup alike. Steps must be idempotent (create-or-load) and work on
        /// assets only: a throwaway scene is open when they run.
        /// </summary>
        public static readonly List<System.Action> PostScaffold = new();

        [MenuItem("JamKit/New Jam Project", priority = 0)]
        public static void Run()
        {
            if (!EditorUtility.DisplayDialog(
                "JamKit: New Jam Project",
                "Creates Bootstrap / Game / GameOver scenes, the SO service assets, the Ripple variables, " +
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

            Debug.Log("[JamKit] Setup complete. Services in Assets/_Project/Services; JamKitCore + JamKitMenu " +
                      "prefabs and the starter library in Assets/_Project/Prefabs (edit a prefab once, every scene " +
                      "updates — customize starters as prefab VARIANTS). Press Play: Start → Settings → Game " +
                      "(Esc pauses) → GameOver all work end-to-end.");
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

            var panelSettings = TemplateAssets.EnsurePanelSettings();
            var menuUxml = TemplateAssets.EnsureMenuDocument();

            // Ripple variables: volumes (persistent — settings survive restarts), score/high-score
            // (high score persistent), and a timer readout for HUD binding. State lives here, not
            // in services (see PILLARS.md); the only score logic is the HighScoreTracker on Core.
            var master = CreateOrLoadFloatVar($"{VariablesDir}/MasterVolume.asset", persist: true);
            var music  = CreateOrLoadFloatVar($"{VariablesDir}/MusicVolume.asset", persist: true);
            var sfx    = CreateOrLoadFloatVar($"{VariablesDir}/SfxVolume.asset", persist: true);
            var scoreVar = CreateOrLoadFloatVar($"{VariablesDir}/Score.asset", 0f, 999999f, 0f);
            var highVar  = CreateOrLoadFloatVar($"{VariablesDir}/HighScore.asset", 0f, 999999f, 0f, persist: true);
            CreateOrLoadFloatVar($"{VariablesDir}/Timer.asset", 0f, 999999f, 0f);

            // Service SOs — services wrap behavior only (scene loads, timescale, input, pooling, IO).
            // Audio is FMOD-first: with FMOD installed, no mixer and no Unity AudioService are
            // scaffolded — audio is driven directly through FMOD, with the menu sliders writing the
            // same Ripple volume variables that the project's FMOD glue reads.
            AudioServiceSO audio = null;
#if !JAMKIT_FMOD
            var mixer = TemplateAssets.EnsureMixer();
            audio = CreateOrLoadSO<AudioServiceSO>($"{ServicesDir}/AudioService.asset", a =>
            {
                a.Mixer = mixer;
                a.MasterVolume = master;
                a.MusicVolume = music;
                a.SfxVolume = sfx;
            });
#endif
            var time  = CreateOrLoadSO<TimeServiceSO>($"{ServicesDir}/TimeService.asset");
            var scene = CreateOrLoadSO<SceneServiceSO>($"{ServicesDir}/SceneService.asset");
            CreateOrLoadSO<SaveServiceSO>($"{ServicesDir}/SaveService.asset");
            var pool  = CreateOrLoadSO<PoolServiceSO>($"{ServicesDir}/PoolService.asset");

            var actions = Resources.Load<InputActionAsset>("JamKitInput");
            var input = CreateOrLoadSO<InputServiceSO>($"{ServicesDir}/InputService.asset", i => { i.Actions = actions; });

            AssetDatabase.SaveAssets();

            // Prefab-first: JamKitCore + Menu are prefab assets instanced into each scene, so a
            // later fix or addition propagates everywhere at once. Built in a throwaway empty
            // scene so the user's scene never gets temp objects; we open Bootstrap at the end.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var corePrefab = CreateOrLoadCorePrefab(audio, time, scene, pool, scoreVar, highVar);
            var menuPrefab = CreateOrLoadMenuPrefab(audio, time, scene, input, panelSettings, menuUxml, master, music, sfx);

            // Starter prefab assets — the Lego bricks designers variant and drop into scenes.
            StarterPrefabLibrary.EnsureAll(new StarterPrefabLibrary.Context
            {
                Input = input,
                Time = time,
                Pool = pool,
                Score = scoreVar,
            });

            // Bootstrap is built last so it's the scene left open for the user.
            if (overwriteScenes || !File.Exists(GameScenePath))
                CreateGameScene(GameScenePath, corePrefab, menuPrefab, input);
            if (overwriteScenes || !File.Exists(GameOverScenePath))
                CreateGameOverScene(GameOverScenePath, corePrefab, scene, scoreVar, highVar, panelSettings);
            if (overwriteScenes || !File.Exists(BootstrapScenePath))
                CreateBootstrapScene(BootstrapScenePath, corePrefab, menuPrefab);

            AddScenesToBuildSettings(new[] { BootstrapScenePath, GameScenePath, GameOverScenePath });

            foreach (var step in PostScaffold) step();

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

        static FloatVariableSO CreateOrLoadFloatVar(string path, float min = 0f, float max = 1f, float initial = 1f, bool persist = false)
        {
            var existing = AssetDatabase.LoadAssetAtPath<FloatVariableSO>(path);
            if (existing != null) return existing;
            var inst = ScriptableObject.CreateInstance<FloatVariableSO>();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(inst, path);

            // Set min/max/initial/persist on the Ripple base via SerializedObject (string paths, so
            // no compile-time dependency on Ripple's private fields) so the values stick.
            var so = new SerializedObject(inst);
            TrySetFloat(so, "min", min);
            TrySetFloat(so, "max", max);
            TrySetFloat(so, "_initialValue", initial);
            TrySetBool(so, "_persist", persist);
            so.ApplyModifiedPropertiesWithoutUndo();
            return inst;
        }

        static void TrySetFloat(SerializedObject so, string name, float v)
        {
            var prop = so.FindProperty(name);
            if (prop != null && prop.propertyType == SerializedPropertyType.Float) prop.floatValue = v;
        }

        static void TrySetBool(SerializedObject so, string name, bool v)
        {
            var prop = so.FindProperty(name);
            if (prop != null && prop.propertyType == SerializedPropertyType.Boolean) prop.boolValue = v;
        }

        // -------------------- shared scene pieces --------------------

        /// <summary>
        /// The JamKitCore prefab: every service runner + fade overlay + the score tracker +
        /// the DebugPanel (Backquote — the only debug surface that exists in builds), plus the
        /// Toast child (it hosts its own UIDocument at runtime). Load-or-create so user
        /// customizations survive re-running. On the FMOD path <paramref name="audio"/> is null
        /// — audio runs directly through FMOD rather than a JamKit audio-service runner.
        /// </summary>
        static GameObject CreateOrLoadCorePrefab(AudioServiceSO audio, TimeServiceSO time, SceneServiceSO scene, PoolServiceSO pool,
            FloatVariableSO scoreVar, FloatVariableSO highVar)
        {
            string path = CorePrefabPath;
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var core = new GameObject("JamKitCore");
            if (audio != null) core.AddComponent<AudioServiceRunner>().Service = audio;
            core.AddComponent<TimeServiceRunner>().Service = time;
            core.AddComponent<PoolServiceRunner>().Service = pool;
            var fade = core.AddComponent<FadeOverlay>();
            var sceneRunner = core.AddComponent<SceneServiceRunner>();
            sceneRunner.Service = scene;
            sceneRunner.Fade = fade;
            var tracker = core.AddComponent<HighScoreTracker>();
            tracker.Score = scoreVar;
            tracker.HighScore = highVar;

            var toast = new GameObject("Toast");
            toast.transform.SetParent(core.transform, false);
            toast.AddComponent<Toast>();

            var debug = new GameObject("DebugPanel");
            debug.transform.SetParent(core.transform, false);
            var panel = debug.AddComponent<DebugPanel>();
            panel.TimeService = time;
            panel.SceneService = scene;

            Directory.CreateDirectory(PrefabsDir);
            var prefab = PrefabUtility.SaveAsPrefabAsset(core, path);
            Object.DestroyImmediate(core);
            return prefab;
        }

        /// <summary>
        /// Menu prefab wired to the services; scenes instance it and override InitialView. The
        /// volume-override variables are set explicitly so the sliders bind with EITHER audio
        /// backend (under FMOD there is no AudioServiceSO to fall back to).
        /// </summary>
        static GameObject CreateOrLoadMenuPrefab(AudioServiceSO audio, TimeServiceSO time, SceneServiceSO scene, InputServiceSO input,
            PanelSettings panelSettings, VisualTreeAsset menuUxml, FloatVariableSO master, FloatVariableSO music, FloatVariableSO sfx)
        {
            string path = MenuPrefabPath;
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return RepointMenuUxml(existing, path, menuUxml);

            var root = new GameObject("JamKitMenu");
            var doc = root.AddComponent<UIDocument>();
            var controller = root.AddComponent<MenuController>();

            if (menuUxml != null)
            {
                doc.visualTreeAsset = menuUxml;
                controller.MenuUxml = menuUxml;
            }
            if (panelSettings != null) doc.panelSettings = panelSettings;

            controller.AudioService = audio; // null on the FMOD path — menu SFX run through FMOD instead
            controller.TimeService = time;
            controller.SceneService = scene;
            controller.InputService = input;
            controller.MasterVolumeOverride = master;
            controller.MusicVolumeOverride = music;
            controller.SfxVolumeOverride = sfx;
            controller.InitialView = MenuController.View.Start;

            Directory.CreateDirectory(PrefabsDir);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>
        /// Point an already-scaffolded menu prefab at the project's UXML copy. Projects scaffolded
        /// before the markup became a project-owned template still reference the package asset (which
        /// lives in the read-only package cache); re-running the wizard migrates them. No-op once the
        /// reference is already local, so this stays idempotent.
        /// </summary>
        static GameObject RepointMenuUxml(GameObject prefab, string path, VisualTreeAsset menuUxml)
        {
            if (menuUxml == null) return prefab;

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var doc = root.GetComponent<UIDocument>();
                var controller = root.GetComponent<MenuController>();

                bool changed = false;
                if (doc != null && doc.visualTreeAsset != menuUxml) { doc.visualTreeAsset = menuUxml; changed = true; }
                if (controller != null && controller.MenuUxml != menuUxml) { controller.MenuUxml = menuUxml; changed = true; }
                if (!changed) return prefab;

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
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
            SceneServiceSO scene, FloatVariableSO scoreVar, FloatVariableSO highVar, PanelSettings panelSettings)
        {
            var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera(cinemachine: false);
            Instance(corePrefab);

            var uiGo = new GameObject("GameOverUI");
            var doc = uiGo.AddComponent<UIDocument>();
            doc.panelSettings = panelSettings;
            var over = uiGo.AddComponent<GameOverController>();
            over.SceneService = scene;
            over.ScoreVariable = scoreVar;
            over.HighScoreVariable = highVar;
            over.RetryScene = new SceneRef("Game");
            over.MainMenuScene = new SceneRef("Bootstrap");

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
