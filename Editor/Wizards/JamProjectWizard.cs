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
        const string ServicesDir = "Assets/_Project/Services";
        const string VariablesDir = "Assets/_Project/Variables";

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

            const string bootstrapPath = ProjectRoot + "/Scenes/Bootstrap.unity";
            const string gamePath = ProjectRoot + "/Scenes/Game.unity";
            const string gameOverPath = ProjectRoot + "/Scenes/GameOver.unity";

            // Never silently stomp scenes the user may have built their jam in. Asset creation is
            // create-or-load (safe); scene creation is the destructive part, so it gets a choice.
            bool overwriteScenes = true;
            var existingScenes = new List<string>();
            foreach (var p in new[] { bootstrapPath, gamePath, gameOverPath })
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

            // Bootstrap is built last so it's the scene left open for the user.
            if (overwriteScenes || !File.Exists(gamePath))
                CreateGameScene(gamePath, audio, time, scene, pool, timer, input, panelSettings);
            if (overwriteScenes || !File.Exists(gameOverPath))
                CreateGameOverScene(gameOverPath, audio, time, scene, pool, timer, score, panelSettings);
            if (overwriteScenes || !File.Exists(bootstrapPath))
                CreateBootstrapScene(bootstrapPath, audio, time, scene, pool, timer, input, panelSettings);

            AddScenesToBuildSettings(new[] { bootstrapPath, gamePath, gameOverPath });
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(bootstrapPath);

            EditorUtility.DisplayDialog("JamKit",
                "Setup complete.\n\n" +
                "Service SOs live in Assets/_Project/Services.\n" +
                "Press Play: Start → Settings → Game (pause with Esc) → GameOver all work end-to-end.", "OK");
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

        /// <summary>Create a JamKitCore with every service runner + the fade overlay, wired to the shared SOs.</summary>
        static GameObject BuildCore(AudioServiceSO audio, TimeServiceSO time, SceneServiceSO scene, PoolServiceSO pool, TimerServiceSO timer)
        {
            var core = new GameObject("JamKitCore");
            core.AddComponent<AudioServiceRunner>().Service = audio;
            core.AddComponent<TimeServiceRunner>().Service = time;
            core.AddComponent<PoolServiceRunner>().Service = pool;
            core.AddComponent<TimerServiceRunner>().Service = timer;
            var fade = core.AddComponent<FadeOverlay>();
            var sceneRunner = core.AddComponent<SceneServiceRunner>();
            sceneRunner.Service = scene;
            sceneRunner.Fade = fade;
            return core;
        }

        static void CreateEventSystem()
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        static void CreateCamera(bool cinemachine)
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
                camGo.AddComponent<CinemachineImpulseListener>();
            }
            camGo.AddComponent<AudioListener>();
        }

        static MenuController AddMenu(string goName, PanelSettings panelSettings, MenuController.View initial,
            AudioServiceSO audio, TimeServiceSO time, SceneServiceSO scene, InputServiceSO input)
        {
            var menuGo = new GameObject(goName);
            var doc = menuGo.AddComponent<UIDocument>();
            doc.panelSettings = panelSettings;
            var uxml = Resources.Load<VisualTreeAsset>("JamKitMenu");
            if (uxml != null) doc.visualTreeAsset = uxml;
            var menu = menuGo.AddComponent<MenuController>();
            menu.MenuUxml = uxml;
            menu.AudioService = audio;
            menu.TimeService = time;
            menu.SceneService = scene;
            menu.InputService = input;
            menu.InitialView = initial;
            return menu;
        }

        // -------------------- scenes --------------------

        static void CreateBootstrapScene(string path,
            AudioServiceSO audio, TimeServiceSO time, SceneServiceSO scene, PoolServiceSO pool,
            TimerServiceSO timer, InputServiceSO input, PanelSettings panelSettings)
        {
            var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera(cinemachine: true);
            BuildCore(audio, time, scene, pool, timer);
            AddMenu("JamKitMenu", panelSettings, MenuController.View.Start, audio, time, scene, input);
            CreateEventSystem();

            EditorSceneManager.MarkSceneDirty(s);
            EditorSceneManager.SaveScene(s, path);
        }

        static void CreateGameScene(string path,
            AudioServiceSO audio, TimeServiceSO time, SceneServiceSO scene, PoolServiceSO pool,
            TimerServiceSO timer, InputServiceSO input, PanelSettings panelSettings)
        {
            var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera(cinemachine: true);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            BuildCore(audio, time, scene, pool, timer);

            // Hidden pause layer: Esc opens pause, gameplay input goes live (InitialView = None).
            var menu = AddMenu("JamKitGameplayUI", panelSettings, MenuController.View.None, audio, time, scene, input);
            var pause = menu.gameObject.AddComponent<PauseController>();
            pause.Menu = menu;
            pause.InputService = input;

            CreateEventSystem();

            EditorSceneManager.MarkSceneDirty(s);
            EditorSceneManager.SaveScene(s, path);
        }

        static void CreateGameOverScene(string path,
            AudioServiceSO audio, TimeServiceSO time, SceneServiceSO scene, PoolServiceSO pool,
            TimerServiceSO timer, ScoreServiceSO score, PanelSettings panelSettings)
        {
            var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera(cinemachine: false);
            BuildCore(audio, time, scene, pool, timer);

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
