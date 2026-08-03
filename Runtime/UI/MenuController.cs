using System;
using System.Collections.Generic;
using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    /// <summary>UI Toolkit menu: Start / Settings / Pause views in one UIDocument, with direct SO service refs (no service locator).</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MenuController : MonoBehaviour
    {
        public enum View { None, Start, Settings, Pause }

        // Element names — must match JamKitMenu.uxml.
        static class N
        {
            public const string StartView = "start-menu", SettingsView = "settings-menu", PauseView = "pause-menu";
            public const string StartPlay = "start-play", StartSettings = "start-settings", StartQuit = "start-quit";
            public const string SettingsBack = "settings-back";
            public const string PauseResume = "pause-resume", PauseSettings = "pause-settings",
                PauseRestart = "pause-restart", PauseMainMenu = "pause-mainmenu";
        }

        [Header("Resources")]
        [Tooltip("UXML asset. If empty, loads JamKitMenu.uxml from Resources (the wizard scaffolds an editable copy).")]
        public VisualTreeAsset MenuUxml;
        [Tooltip("Optional USS layered on top of the menu stylesheet.")]
        public StyleSheet ExtraStyles;

        [Header("Services")]
        public TimeServiceSO TimeService;
        [Required] public SceneServiceSO SceneService;
        public InputServiceSO InputService;

        [Header("Volume (Ripple Variables)")]
        [FormerlySerializedAs("MasterVolumeOverride")] public FloatVariableSO masterVolume;
        [FormerlySerializedAs("MusicVolumeOverride")] public FloatVariableSO musicVolume;
        [FormerlySerializedAs("SfxVolumeOverride")] public FloatVariableSO sfxVolume;

        [Header("Behaviour")]
        public SceneRef GameScene = new("Game");
        public SceneRef MainMenuScene = new("Bootstrap");
        [Tooltip("Which view is shown on Enable. None hides the canvas until you call ShowStart().")]
        public View InitialView = View.Start;

        [SerializeField, Required] private VoidEventSO onPauseOpened;
        [SerializeField, Required] private VoidEventSO onPauseClosed;

        UIDocument _doc;
        VisualElement _root;
        MenuSettingsBinder _settings;
        PauseCoordinator _pause;
        MenuNavigator _nav;
        readonly HashSet<string> _warnedButtons = new();

        public View Current => _nav?.Current ?? View.None;
        public VisualElement Root => _root;
        public bool IsPauseOpen => Current == View.Pause;

        void Awake()
        {
            _doc = GetComponent<UIDocument>();
            if (MenuUxml == null) MenuUxml = Resources.Load<VisualTreeAsset>("JamKitMenu");
            if (MenuUxml == null)
                Debug.LogWarning("[JamKit] No JamKitMenu.uxml in Resources. Run JamKit > New Jam Project, or assign MenuUxml.", this);
            if (_doc.visualTreeAsset == null && MenuUxml != null) _doc.visualTreeAsset = MenuUxml;
            if (_doc.panelSettings == null) _doc.panelSettings = JamKitUI.LoadOrCreateMenuPanelSettings();
            else JamKitUI.ApplyDefaultTheme(_doc.panelSettings);
        }

        void OnEnable()
        {
            _root = _doc.rootVisualElement;
            if (_root == null) return;

            if (ExtraStyles != null && !_root.styleSheets.Contains(ExtraStyles)) _root.styleSheets.Add(ExtraStyles);

            _settings = new MenuSettingsBinder(_root, masterVolume, musicVolume, sfxVolume);
            _pause = new PauseCoordinator(TimeService, onPauseOpened, onPauseClosed);
            _nav = new MenuNavigator(new Dictionary<View, VisualElement>
            {
                { View.Start, _root.Q<VisualElement>(N.StartView) },
                { View.Settings, _root.Q<VisualElement>(N.SettingsView) },
                { View.Pause, _root.Q<VisualElement>(N.PauseView) },
            }, _pause, _settings);

            Bind(N.StartPlay, () => SceneService?.LoadAsync(GameScene));
            Bind(N.StartSettings, ShowSettings);
            Bind(N.StartQuit, QuitGame);
            Bind(N.SettingsBack, () => _nav.Pop());
            Bind(N.PauseResume, TogglePause);
            Bind(N.PauseSettings, ShowSettings);
            Bind(N.PauseRestart, () => { _pause.Resume(); SceneService?.ReloadCurrent(); });
            Bind(N.PauseMainMenu, () => { _pause.Resume(); SceneService?.LoadAsync(MainMenuScene); });

            // Quit is meaningless in a browser; hide it so WebGL builds don't ship a dead button.
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                var quit = _root.Q<Button>(N.StartQuit);
                if (quit != null) quit.style.display = DisplayStyle.None;
            }

            switch (InitialView)
            {
                case View.Start: ShowStart(); break;
                case View.Settings: ShowSettings(); break;
                case View.Pause: ShowPause(); break;
                default: HideAll(); break;
            }
            if (InitialView == View.None) InputService?.SwitchToGameplay();
        }

        void OnDisable()
        {
            _settings?.Dispose();
            _settings = null;
            _pause?.Resume(); // never leave the game frozen
        }

        [Button, DisableInEditorMode, FoldoutGroup("Debug"), HorizontalGroup("Debug/Row")]
        public void ShowStart() => _nav?.Reset(View.Start);
        public void ShowSettings() => _nav?.Push(View.Settings);
        public void ShowPause() => _nav?.Push(View.Pause);
        public void HideAll() => _nav?.Reset(View.None);

        [Button, DisableInEditorMode, HorizontalGroup("Debug/Row")]
        public void TogglePause()
        {
            if (_nav == null) return;
            if (IsPauseOpen) { _nav.Pop(); InputService?.SwitchToGameplay(); }
            else { _nav.Push(View.Pause); _pause?.Pause(); InputService?.SwitchToUI(); }
        }

        // Esc/Back (via PauseController): pop a stacked submenu, else toggle pause.
        public void HandleBack()
        {
            if (_nav == null) return;
            if (_nav.Depth > 1) { _nav.Pop(); return; }
            if (Current == View.Start) return;
            TogglePause();
        }

        void Bind(string name, Action action)
        {
            var btn = _root.Q<Button>(name);
            if (btn == null)
            {
                if (_warnedButtons.Add(name))
                    Debug.LogWarning($"[JamKit] MenuController: no button '{name}' in the menu UXML — that control does nothing.", this);
                return;
            }
            btn.clicked += action;
        }

        static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
