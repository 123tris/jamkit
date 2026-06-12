using System.Collections.Generic;
using Ripple;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    /// <summary>
    /// UI Toolkit menu controller. Owns the Start / Settings / Pause views in a single
    /// <see cref="UIDocument"/>. Holds direct SO references to the services it needs —
    /// no static lookup, no service locator. Volume sliders bind two-way to Ripple variables;
    /// graphics settings persist via PlayerPrefs inline.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MenuController : MonoBehaviour
    {
        public enum View { None, Start, Settings, Pause }

        /// <summary>Element names — must match JamKitMenu.uxml. Centralized so a typo is a compile error, not a silent no-op.</summary>
        static class N
        {
            public const string StartView = "start-menu", SettingsView = "settings-menu", PauseView = "pause-menu";
            public const string StartPlay = "start-play", StartSettings = "start-settings", StartQuit = "start-quit";
            public const string SettingsBack = "settings-back";
            public const string PauseResume = "pause-resume", PauseSettings = "pause-settings", PauseRestart = "pause-restart", PauseMainMenu = "pause-mainmenu";
            public const string Master = "settings-master", Music = "settings-music", Sfx = "settings-sfx";
            public const string Quality = "settings-quality", Resolution = "settings-resolution", Fullscreen = "settings-fullscreen", VSync = "settings-vsync";
        }

        [Header("Resources")]
        [Tooltip("UXML asset. If left empty, MenuController loads Resources/JamKitMenu.uxml.")]
        public VisualTreeAsset MenuUxml;
        [Tooltip("Optional USS to layer on top of the bundled stylesheet.")]
        public StyleSheet ExtraStyles;

        [Header("Services")]
        public AudioServiceSO AudioService;
        public TimeServiceSO TimeService;
        public SceneServiceSO SceneService;
        public InputServiceSO InputService;

        [Header("Volume (Ripple Variables)")]
        [Tooltip("Optional — overrides Audio.MasterVolume if set. Otherwise pulls from the AudioServiceSO.")]
        public FloatVariableSO MasterVolumeOverride;
        public FloatVariableSO MusicVolumeOverride;
        public FloatVariableSO SfxVolumeOverride;

        [Header("Behaviour")]
        public string GameSceneName = "Game";
        public string MainMenuSceneName = "Bootstrap";
        [Tooltip("Which view is shown on Enable. None hides the canvas until you call ShowStart().")]
        public View InitialView = View.Start;

        UIDocument _doc;
        VisualElement _root;
        VisualElement _startView, _settingsView, _pauseView;

        Slider _master, _music, _sfx;
        DropdownField _quality, _resolution;
        Toggle _fullscreen, _vsync;

        Resolution[] _resolutions;
        readonly Stack<View> _stack = new();
        readonly List<System.Action> _teardown = new();
        bool _pausedByMe;

        FloatVariableSO MasterVar => MasterVolumeOverride != null ? MasterVolumeOverride : AudioService != null ? AudioService.MasterVolume : null;
        FloatVariableSO MusicVar  => MusicVolumeOverride  != null ? MusicVolumeOverride  : AudioService != null ? AudioService.MusicVolume  : null;
        FloatVariableSO SfxVar    => SfxVolumeOverride    != null ? SfxVolumeOverride    : AudioService != null ? AudioService.SfxVolume    : null;

        public View Current => _stack.Count > 0 ? _stack.Peek() : View.None;
        public VisualElement Root => _root;

        // -------------------- lifecycle --------------------

        void Awake()
        {
            _doc = GetComponent<UIDocument>();
            if (MenuUxml == null) MenuUxml = Resources.Load<VisualTreeAsset>("JamKitMenu");
            if (_doc.visualTreeAsset == null && MenuUxml != null) _doc.visualTreeAsset = MenuUxml;
            if (_doc.panelSettings == null) _doc.panelSettings = MakeRuntimePanelSettings();
        }

        void OnEnable()
        {
            _root = _doc.rootVisualElement;
            if (_root == null) return;

            EnsureStylesheet();
            CacheViews();
            CacheSettingsControls();
            WireButtons();
            WireSettings();

            switch (InitialView)
            {
                case View.Start: ShowStart(); break;
                case View.Settings: ShowSettings(); break;
                case View.Pause: ShowPause(); break;
                default: HideAll(); break;
            }

            // When this menu is just a hidden pause layer over gameplay (InitialView = None),
            // bring the Gameplay input map online so movers/shooters respond immediately.
            if (InitialView == View.None) InputService?.SwitchToGameplay();
        }

        void OnDisable()
        {
            // Variable subscriptions live on persistent SOs, so they must be torn down explicitly.
            // (Element callbacks die with the visual tree when the UIDocument rebuilds on enable.)
            for (int i = 0; i < _teardown.Count; i++) _teardown[i]?.Invoke();
            _teardown.Clear();
            // Safety: never leave the game frozen if the menu is disabled mid-pause.
            ResumeIfPaused();
        }

        // -------------------- public API --------------------

        public void ShowStart() => ResetStack(View.Start);
        public void ShowSettings() => Push(View.Settings);
        public void ShowPause() => Push(View.Pause);
        public void HideAll() => ResetStack(View.None);

        public bool IsPauseOpen => Current == View.Pause;

        public void TogglePause()
        {
            if (IsPauseOpen) { Pop(); ResumeIfPaused(); InputService?.SwitchToGameplay(); }
            else { Push(View.Pause); PauseIfNot(); InputService?.SwitchToUI(); }
        }

        /// <summary>
        /// Back/cancel (e.g. the Esc key, routed via <see cref="PauseController"/>): if a submenu is
        /// stacked, pop it; otherwise toggle the pause menu. Fixes the case where Esc inside
        /// Pause → Settings used to push a second Pause instead of returning to Pause.
        /// </summary>
        public void HandleBack()
        {
            if (_stack.Count > 1) { Pop(); return; }     // Settings (over Start or Pause) → back one level.
            if (Current == View.Start) return;            // Main menu root: nothing to back out of.
            TogglePause();                                // Gameplay → open pause; Pause → resume.
        }

        // -------------------- stack --------------------

        void Push(View v)
        {
            if (_stack.Count > 0 && _stack.Peek() == v) return;
            _stack.Push(v);
            Apply();
        }

        void Pop()
        {
            if (_stack.Count == 0) return;
            var leaving = _stack.Pop();
            if (leaving == View.Pause) ResumeIfPaused();
            Apply();
        }

        void ResetStack(View v)
        {
            _stack.Clear();
            if (v != View.None) _stack.Push(v);
            ResumeIfPaused();
            Apply();
        }

        void Apply()
        {
            var top = Current;
            SetDisplay(_startView,    top == View.Start);
            SetDisplay(_settingsView, top == View.Settings);
            SetDisplay(_pauseView,    top == View.Pause);
            if (top == View.Settings) SyncSettingsValuesToUI();
            FocusFirst(top);
        }

        static void SetDisplay(VisualElement el, bool visible)
        {
            if (el == null) return;
            el.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Focus the primary control of the active view so keyboard/gamepad navigation has a starting point.</summary>
        void FocusFirst(View v)
        {
            VisualElement target = v switch
            {
                View.Start    => _root.Q<Button>(N.StartPlay),
                View.Pause    => _root.Q<Button>(N.PauseResume),
                View.Settings => (VisualElement)_master ?? _root.Q<Button>(N.SettingsBack),
                _ => null
            };
            // Schedule so the element is laid out (display:flex applied) before we focus it.
            target?.schedule.Execute(() => target.Focus());
        }

        // -------------------- caching --------------------

        void CacheViews()
        {
            _startView    = _root.Q<VisualElement>(N.StartView);
            _settingsView = _root.Q<VisualElement>(N.SettingsView);
            _pauseView    = _root.Q<VisualElement>(N.PauseView);
        }

        void CacheSettingsControls()
        {
            _master    = _root.Q<Slider>(N.Master);
            _music     = _root.Q<Slider>(N.Music);
            _sfx       = _root.Q<Slider>(N.Sfx);
            _quality   = _root.Q<DropdownField>(N.Quality);
            _resolution= _root.Q<DropdownField>(N.Resolution);
            _fullscreen= _root.Q<Toggle>(N.Fullscreen);
            _vsync     = _root.Q<Toggle>(N.VSync);
        }

        void EnsureStylesheet()
        {
            // The bundled JamKitMenu.uxml references JamKitMenu.uss via <Style>, so the stylesheet is
            // applied when the UXML loads — we don't re-add it here (that would be a second source of
            // truth for the same asset). If you point MenuUxml at a custom UXML, reference your
            // stylesheet from that UXML, or layer one via ExtraStyles below.
            if (ExtraStyles != null && !_root.styleSheets.Contains(ExtraStyles))
                _root.styleSheets.Add(ExtraStyles);
        }

        // -------------------- buttons --------------------

        void WireButtons()
        {
            Bind(N.StartPlay,     () => SceneService?.LoadAsync(GameSceneName));
            Bind(N.StartSettings, ShowSettings);
            Bind(N.StartQuit,     QuitGame);

            Bind(N.SettingsBack, Pop);

            Bind(N.PauseResume,   TogglePause);
            Bind(N.PauseSettings, ShowSettings);
            Bind(N.PauseRestart,  () => { ResumeIfPaused(); SceneService?.ReloadCurrent(); });
            Bind(N.PauseMainMenu, () => { ResumeIfPaused(); SceneService?.LoadAsync(MainMenuSceneName); });
        }

        void Bind(string elementName, System.Action action)
        {
            var btn = _root.Q<Button>(elementName);
            if (btn == null) return;
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

        // -------------------- settings binding --------------------

        const string PrefsQuality = "JamKit.Gfx.Quality";
        const string PrefsFullscreen = "JamKit.Gfx.Fullscreen";
        const string PrefsResolution = "JamKit.Gfx.ResolutionIndex";
        const string PrefsVSync = "JamKit.Gfx.VSync";

        void WireSettings()
        {
            BuildResolutions();
            BuildQualityChoices();

            HookVolumeSlider(_master, MasterVar);
            HookVolumeSlider(_music, MusicVar);
            HookVolumeSlider(_sfx, SfxVar);

            if (_quality != null)
                _quality.RegisterValueChangedCallback(e =>
                {
                    int idx = _quality.choices.IndexOf(e.newValue);
                    if (idx < 0) return;
                    PlayerPrefs.SetInt(PrefsQuality, idx);
                    QualitySettings.SetQualityLevel(idx, true);
                });

            if (_resolution != null)
                _resolution.RegisterValueChangedCallback(e =>
                {
                    int idx = _resolution.choices.IndexOf(e.newValue);
                    if (idx < 0) return;
                    PlayerPrefs.SetInt(PrefsResolution, idx);
                    ApplyScreen();
                });

            if (_fullscreen != null)
                _fullscreen.RegisterValueChangedCallback(e => { PlayerPrefs.SetInt(PrefsFullscreen, e.newValue ? 1 : 0); ApplyScreen(); });

            if (_vsync != null)
                _vsync.RegisterValueChangedCallback(e => { PlayerPrefs.SetInt(PrefsVSync, e.newValue ? 1 : 0); QualitySettings.vSyncCount = e.newValue ? 1 : 0; });

            // Apply persisted state once.
            QualitySettings.SetQualityLevel(Mathf.Clamp(PlayerPrefs.GetInt(PrefsQuality, QualitySettings.GetQualityLevel()), 0, QualitySettings.names.Length - 1), true);
            QualitySettings.vSyncCount = PlayerPrefs.GetInt(PrefsVSync, QualitySettings.vSyncCount > 0 ? 1 : 0);
            ApplyScreen();
        }

        /// <summary>Wire a volume slider two-way to its Ripple variable, and register the variable→slider teardown.</summary>
        void HookVolumeSlider(Slider slider, FloatVariableSO variable)
        {
            if (slider == null) return;
            slider.lowValue = 0f; slider.highValue = 1f;
            if (variable == null) return;

            // slider → variable (callback dies with the visual tree, no teardown needed).
            slider.RegisterValueChangedCallback(e =>
            {
                if (!Mathf.Approximately(variable.CurrentValue, e.newValue)) variable.SetCurrentValue(e.newValue);
            });

            // variable → slider (lives on the persistent SO; must be torn down in OnDisable).
            void OnVarChanged(float v)
            {
                if (slider.panel != null && !Mathf.Approximately(slider.value, v)) slider.SetValueWithoutNotify(v);
            }
            variable.OnValueChanged += OnVarChanged;
            _teardown.Add(() => variable.OnValueChanged -= OnVarChanged);

            slider.SetValueWithoutNotify(variable.CurrentValue);
        }

        void BuildResolutions()
        {
            // Dedupe Screen.resolutions by width×height, keeping the highest refresh rate per size.
            var all = Screen.resolutions;
            var unique = new List<Resolution>(all.Length);
            for (int i = 0; i < all.Length; i++)
            {
                var r = all[i];
                int found = -1;
                for (int j = 0; j < unique.Count; j++)
                    if (unique[j].width == r.width && unique[j].height == r.height) { found = j; break; }
                if (found < 0) unique.Add(r);
                else if (r.refreshRateRatio.value > unique[found].refreshRateRatio.value) unique[found] = r;
            }
            _resolutions = unique.ToArray();

            if (_resolution == null) return;
            var options = new List<string>(_resolutions.Length);
            int defaultIdx = 0;
            for (int i = 0; i < _resolutions.Length; i++)
            {
                var r = _resolutions[i];
                options.Add($"{r.width} x {r.height}");
                if (r.width == Screen.currentResolution.width && r.height == Screen.currentResolution.height) defaultIdx = i;
            }
            _resolution.choices = options;
            int persisted = PlayerPrefs.GetInt(PrefsResolution, defaultIdx);
            _resolution.SetValueWithoutNotify(SafeChoice(_resolution, persisted));
        }

        void BuildQualityChoices()
        {
            if (_quality == null) return;
            _quality.choices = new List<string>(QualitySettings.names);
            _quality.SetValueWithoutNotify(SafeChoice(_quality, PlayerPrefs.GetInt(PrefsQuality, QualitySettings.GetQualityLevel())));
        }

        void SyncSettingsValuesToUI()
        {
            if (_master != null && MasterVar != null) _master.SetValueWithoutNotify(MasterVar.CurrentValue);
            if (_music  != null && MusicVar  != null) _music.SetValueWithoutNotify(MusicVar.CurrentValue);
            if (_sfx    != null && SfxVar    != null) _sfx.SetValueWithoutNotify(SfxVar.CurrentValue);
            if (_quality    != null) _quality.SetValueWithoutNotify(SafeChoice(_quality, PlayerPrefs.GetInt(PrefsQuality, QualitySettings.GetQualityLevel())));
            if (_resolution != null) _resolution.SetValueWithoutNotify(SafeChoice(_resolution, PlayerPrefs.GetInt(PrefsResolution, 0)));
            if (_fullscreen != null) _fullscreen.SetValueWithoutNotify(PlayerPrefs.GetInt(PrefsFullscreen, Screen.fullScreen ? 1 : 0) != 0);
            if (_vsync      != null) _vsync.SetValueWithoutNotify(PlayerPrefs.GetInt(PrefsVSync, QualitySettings.vSyncCount > 0 ? 1 : 0) != 0);
        }

        static string SafeChoice(DropdownField dd, int idx)
        {
            if (dd.choices == null || dd.choices.Count == 0) return string.Empty;
            return dd.choices[Mathf.Clamp(idx, 0, dd.choices.Count - 1)];
        }

        void ApplyScreen()
        {
            if (_resolutions == null || _resolutions.Length == 0) return;
            int i = Mathf.Clamp(PlayerPrefs.GetInt(PrefsResolution, 0), 0, _resolutions.Length - 1);
            var r = _resolutions[i];
            var mode = PlayerPrefs.GetInt(PrefsFullscreen, Screen.fullScreen ? 1 : 0) != 0
                ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            Screen.SetResolution(r.width, r.height, mode, r.refreshRateRatio);
        }

        // -------------------- pause helpers --------------------

        void PauseIfNot() { if (!_pausedByMe && TimeService != null) { TimeService.Pause(); _pausedByMe = true; } }
        void ResumeIfPaused() { if (_pausedByMe && TimeService != null) { TimeService.Resume(); _pausedByMe = false; } }

        // -------------------- panel settings fallback --------------------

        static PanelSettings _runtimePanelSettings;
        static PanelSettings MakeRuntimePanelSettings()
        {
            var loaded = Resources.Load<PanelSettings>("JamKitPanelSettings");
            if (loaded != null) return loaded;
            if (_runtimePanelSettings != null) return _runtimePanelSettings;
            _runtimePanelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            _runtimePanelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            _runtimePanelSettings.referenceResolution = new Vector2Int(1920, 1080);
            _runtimePanelSettings.sortingOrder = 100;
            return _runtimePanelSettings;
        }
    }
}
