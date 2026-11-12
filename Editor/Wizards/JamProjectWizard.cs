using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ripple;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Metz.JamKit.Editor
{
    public static class JamProjectWizard
    {
        const string ProjectRoot = "Assets/_Project";
        const string VariablesDir = ProjectRoot + "/Variables";
        const string EventsDir = ProjectRoot + "/Events";
        const string PrefabsDir = ProjectRoot + "/Prefabs";
        const string GameAsmdefPath = ProjectRoot + "/Scripts/Game.asmdef";

        public const string ServicesDir = ProjectRoot + "/Services";
        public const string BootstrapScenePath = ProjectRoot + "/Scenes/Bootstrap.unity";
        public const string GameScenePath = ProjectRoot + "/Scenes/Game.unity";
        public const string GameOverScenePath = ProjectRoot + "/Scenes/GameOver.unity";
        public const string CorePrefabPath = PrefabsDir + "/JamKitCore.prefab";
        public const string MenuPrefabPath = PrefabsDir + "/JamKitMenu.prefab";

        static readonly string[] ScenePaths = { BootstrapScenePath, GameScenePath, GameOverScenePath };

        static readonly string[] Folders =
        {
            "Scenes", "Scripts", "Prefabs", "Audio/Resources", "UI/Resources", "Art", "Services", "Variables", "Events",
        };

        public static readonly List<Action> PostScaffold = new();

        struct Volumes
        {
            public FloatVariableSO Master;
            public FloatVariableSO Music;
            public FloatVariableSO Sfx;
        }

        struct FlowEvents
        {
            public VoidEventSO Play;
            public VoidEventSO MainMenu;
            public VoidEventSO Restart;
            public VoidEventSO GameOver;
        }

        [MenuItem("JamKit/New Jam Project", priority = 0)]
        public static void Run()
        {
            if (!EditorUtility.DisplayDialog("JamKit: New Jam Project",
                    "Creates Bootstrap, Game and GameOver scenes, the service assets, the Ripple variables, " +
                    "the menu and starter prefabs, and wires each scene with a menu and pause flow.\n\nProceed?",
                    "Create", "Cancel"))
                return;

            bool overwriteScenes = true;
            string[] existing = ScenePaths.Where(File.Exists).ToArray();
            if (existing.Length > 0)
            {
                int choice = EditorUtility.DisplayDialogComplex("JamKit: Scenes Already Exist",
                    "These scenes already exist:\n\n" + string.Join("\n", existing) +
                    "\n\nKeep them and create only the missing ones, or overwrite all three?",
                    "Keep Existing", "Cancel", "Overwrite All");
                if (choice == 1) return;
                overwriteScenes = choice == 2;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scaffold(overwriteScenes);
            EditorSceneManager.OpenScene(BootstrapScenePath);
            OfferFastPlayMode();
            Debug.Log("[JamKit] Setup complete. Press Play: Start, Settings, Game (Esc pauses) and GameOver work end to end.");
        }

        public static void Scaffold(bool overwriteScenes)
        {
            foreach (string folder in Folders) Directory.CreateDirectory($"{ProjectRoot}/{folder}");
            AssetDatabase.Refresh();
            EnsureGameAsmdef();

            PanelSettings panelSettings = TemplateAssets.EnsurePanelSettings();
            VisualTreeAsset menuUxml = TemplateAssets.EnsureMenuDocument();

            var volumes = new Volumes
            {
                Master = Variable("MasterVolume", persist: true),
                Music = Variable("MusicVolume", persist: true),
                Sfx = Variable("SfxVolume", persist: true),
            };
            FloatVariableSO score = Variable("Score", 0f, 999999f, 0f);
            FloatVariableSO highScore = Variable("HighScore", 0f, 999999f, 0f, persist: true);
            Variable("Timer", 0f, 999999f, 0f);
            GameObjectListSO players = Asset<GameObjectListSO>($"{VariablesDir}/Players.asset");

            var time = Asset<TimeServiceSO>($"{ServicesDir}/TimeService.asset");
            var fade = Asset<ScreenFadeSO>($"{ServicesDir}/ScreenFade.asset");
            var scenes = Asset<SceneServiceSO>($"{ServicesDir}/SceneService.asset", service => service.Fade = fade);
            var input = Asset<InputServiceSO>($"{ServicesDir}/InputService.asset",
                service => service.Actions = Resources.Load<InputActionAsset>("JamKitInput"));

            var events = new FlowEvents
            {
                Play = Asset<VoidEventSO>($"{EventsDir}/Play.asset"),
                MainMenu = Asset<VoidEventSO>($"{EventsDir}/MainMenu.asset"),
                Restart = Asset<VoidEventSO>($"{EventsDir}/Restart.asset"),
                GameOver = Asset<VoidEventSO>($"{EventsDir}/GameOver.asset"),
            };
            var flow = Asset<GameFlow>($"{ProjectRoot}/GameFlow.asset", asset => asset.Transitions = new List<GameFlow.Transition>
            {
                new() { On = events.Play, To = new SceneRef("Game") },
                new() { On = events.MainMenu, To = new SceneRef("Bootstrap") },
                new() { On = events.GameOver, To = new SceneRef("GameOver") },
                new() { On = events.Restart },
            });
            AssetDatabase.SaveAssets();

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject core = CorePrefab(time, scenes, flow, score, highScore, volumes);
            GameObject menu = MenuPrefab(time, input, events, panelSettings, menuUxml, volumes);
            StarterPrefabLibrary.EnsureAll(new StarterPrefabLibrary.Context { Time = time, Score = score, Players = players });

            if (overwriteScenes || !File.Exists(GameScenePath)) CreateGameScene(core, menu);
            if (overwriteScenes || !File.Exists(GameOverScenePath)) CreateGameOverScene(core, menu, score);
            if (overwriteScenes || !File.Exists(BootstrapScenePath)) CreateBootstrapScene(core, menu);

            EditorBuildSettings.scenes = EditorBuildSettings.scenes
                .Concat(ScenePaths.Where(path => EditorBuildSettings.scenes.All(scene => scene.path != path))
                    .Select(path => new EditorBuildSettingsScene(path, true)))
                .ToArray();

            foreach (Action step in PostScaffold) step();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void OfferFastPlayMode()
        {
            if (EditorSettings.enterPlayModeOptionsEnabled) return;
            if (!EditorUtility.DisplayDialog("JamKit: Fast Play Mode",
                    "Disable domain reload so Play starts near-instantly?\n\n" +
                    "JamKit and Ripple reset their own state each session. Your own statics must do the same.\n\n" +
                    "Change it any time in Project Settings > Editor > Enter Play Mode Settings.",
                    "Enable", "Skip"))
                return;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        }

        static void EnsureGameAsmdef()
        {
            if (File.Exists(GameAsmdefPath)) return;
            File.WriteAllText(GameAsmdefPath,
@"{
    ""name"": ""Game"",
    ""references"": [
        ""Metz.JamKit.Runtime"",
        ""Metz.Ripple.Runtime"",
        ""Unity.Cinemachine"",
        ""Unity.InputSystem"",
        ""Unity.RenderPipelines.Universal.Runtime""
    ],
    ""autoReferenced"": true
}
");
            AssetDatabase.ImportAsset(GameAsmdefPath);
        }

        static T Asset<T>(string path, Action<T> configure = null) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            configure?.Invoke(asset);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static FloatVariableSO Variable(string name, float min = 0f, float max = 1f, float initial = 1f, bool persist = false)
            => Asset<FloatVariableSO>($"{VariablesDir}/{name}.asset", variable =>
            {
                var serialized = new SerializedObject(variable);
                serialized.FindProperty("_isClamped").boolValue = true;
                serialized.FindProperty("min._constantValue").floatValue = min;
                serialized.FindProperty("max._constantValue").floatValue = max;
                serialized.FindProperty("_initialValue").floatValue = initial;
                serialized.FindProperty("_persist").boolValue = persist;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            });

        static GameObject CorePrefab(TimeServiceSO time, SceneServiceSO scenes, GameFlow flow, FloatVariableSO score,
            FloatVariableSO highScore, Volumes volumes)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(CorePrefabPath);
            if (existing != null) return existing;

            var core = new GameObject("JamKitCore");
            var tracker = core.AddComponent<HighScoreTracker>();
            tracker.Score = score;
            tracker.HighScore = highScore;

            var driver = core.AddComponent<GameFlowDriver>();
            driver.Flow = flow;
            driver.SceneService = scenes;

#if !JAMKIT_FMOD
            var mixer = TemplateAssets.EnsureMixer();
            foreach ((string parameter, FloatVariableSO volume) in new[]
                     {
                         ("MasterVol", volumes.Master), ("MusicVol", volumes.Music), ("SfxVol", volumes.Sfx),
                     })
            {
                var channel = core.AddComponent<MixerVolume>();
                channel.Mixer = mixer;
                channel.Parameter = parameter;
                channel.Volume = volume;
            }
#endif

            Child(core, "Toast").AddComponent<Toast>();

            var debug = Child(core, "DebugPanel").AddComponent<DebugPanel>();
            debug.TimeService = time;
            debug.SceneService = scenes;

            return SaveAsPrefab(core, CorePrefabPath);
        }

        static GameObject MenuPrefab(TimeServiceSO time, InputServiceSO input, FlowEvents events,
            PanelSettings panelSettings, VisualTreeAsset menuUxml, Volumes volumes)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPrefabPath);
            if (existing != null) return existing;

            var root = new GameObject("JamKitMenu");
            var document = root.AddComponent<UIDocument>();
            document.visualTreeAsset = menuUxml;
            document.panelSettings = panelSettings;

            var controller = root.AddComponent<MenuController>();
            controller.TimeService = time;
            controller.InputService = input;
            controller.Pause = InputReferences.Find("Pause");
            controller.Cancel = InputReferences.Find("Cancel");
            controller.MasterVolume = volumes.Master;
            controller.MusicVolume = volumes.Music;
            controller.SfxVolume = volumes.Sfx;

            MenuView start = View(root, "Start", "start-menu");
            Button(start, "start-play", events.Play.Raise);
            Button(start, "start-settings", controller.ShowSettings);
            Button(start, "start-quit", controller.QuitGame);

            MenuView settings = View(root, "Settings", "settings-menu");
            Button(settings, "settings-back", controller.Back);

            MenuView pause = View(root, "Pause", "pause-menu");
            pause.PausesTime = true;
            Button(pause, "pause-resume", controller.Back);
            Button(pause, "pause-settings", controller.ShowSettings);
            Button(pause, "pause-restart", events.Restart.Raise);
            Button(pause, "pause-mainmenu", events.MainMenu.Raise);

            MenuView gameOver = View(root, "GameOver", "gameover-menu");
            Button(gameOver, "gameover-retry", events.Play.Raise);
            Button(gameOver, "gameover-mainmenu", events.MainMenu.Raise);

            controller.Views = new List<MenuView> { start, settings, pause, gameOver };
            controller.InitialView = start;
            controller.PauseView = pause;
            controller.SettingsView = settings;

            return SaveAsPrefab(root, MenuPrefabPath);
        }

        static MenuView View(GameObject menu, string name, string rootElement)
        {
            var view = Child(menu, name).AddComponent<MenuView>();
            view.RootElementName = rootElement;
            return view;
        }

        static void Button(MenuView view, string elementName, UnityAction action)
        {
            var binding = new MenuView.ButtonBinding { ElementName = elementName };
            UnityEventTools.AddPersistentListener(binding.OnClicked.Response, action);
            view.Buttons.Add(binding);
        }

        static GameObject Child(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        static GameObject SaveAsPrefab(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static void CreateBootstrapScene(GameObject core, GameObject menu)
        {
            Scene scene = NewScene(cinemachine: true);
            PrefabUtility.InstantiatePrefab(core);
            PrefabUtility.InstantiatePrefab(menu);
            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
        }

        static void CreateGameScene(GameObject core, GameObject menu)
        {
            Scene scene = NewScene(cinemachine: true);
            new GameObject("Directional Light") { transform = { rotation = Quaternion.Euler(50f, -30f, 0f) } }
                .AddComponent<Light>().type = LightType.Directional;
            PrefabUtility.InstantiatePrefab(core);

            var gameplayMenu = (GameObject)PrefabUtility.InstantiatePrefab(menu);
            gameplayMenu.name = "JamKitGameplayUI";
            gameplayMenu.GetComponent<MenuController>().InitialView = null;
            EditorSceneManager.SaveScene(scene, GameScenePath);
        }

        static void CreateGameOverScene(GameObject core, GameObject menu, FloatVariableSO score)
        {
            Scene scene = NewScene(cinemachine: false);
            PrefabUtility.InstantiatePrefab(core);

            var gameOverMenu = (GameObject)PrefabUtility.InstantiatePrefab(menu);
            gameOverMenu.name = "JamKitGameOverUI";
            var controller = gameOverMenu.GetComponent<MenuController>();
            controller.InitialView = controller.Views.First(view => view.RootElementName == "gameover-menu");

            var scoreLabel = gameOverMenu.AddComponent<LabelBinding>();
            scoreLabel.Document = gameOverMenu.GetComponent<UIDocument>();
            scoreLabel.ElementName = "gameover-score";
            scoreLabel.Variable = score;
            scoreLabel.Format = "Score: {0:0}";
            EditorSceneManager.SaveScene(scene, GameOverScenePath);
        }

        static Scene NewScene(bool cinemachine)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camera = new GameObject("Main Camera") { tag = "MainCamera" };
            camera.transform.position = new Vector3(0f, 1f, -10f);
            var lens = camera.AddComponent<Camera>();
            lens.clearFlags = CameraClearFlags.SolidColor;
            lens.backgroundColor = new Color(0.05f, 0.06f, 0.09f);
            camera.AddComponent<AudioListener>();
            if (cinemachine)
            {
                camera.AddComponent<CinemachineBrain>();
                camera.AddComponent<CinemachineExternalImpulseListener>();
            }

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
            return scene;
        }
    }
}
