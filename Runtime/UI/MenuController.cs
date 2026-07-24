using System;
using System.Collections.Generic;
using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    /// <summary>
    /// UI Toolkit menu controller. Owns the Start / Settings / Pause views in a single
    /// <see cref="UIDocument"/>. Holds direct SO references to the services it needs —
    /// no static lookup, no service locator. View flow and buttons live here; the Settings
    /// view's controls (volume sliders, graphics options) are handled by <see cref="MenuSettingsBinder"/>.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MenuController : MonoBehaviour
    {
        public enum View
        {
            None,
            Start,
            Settings,
            Pause
        }

        /// <summary>Element names — must match JamKitMenu.uxml. Centralized so a typo is a compile error, not a silent no-op.</summary>
        static class N
        {
            public const string StartView = "start-menu", SettingsView = "settings-menu", PauseView = "pause-menu";
            public const string StartPlay = "start-play", StartSettings = "start-settings", StartQuit = "start-quit";
            public const string SettingsBack = "settings-back";

            public const string PauseResume = "pause-resume",
                PauseSettings = "pause-settings",
                PauseRestart = "pause-restart",
                PauseMainMenu = "pause-mainmenu";
        }

        [Header("Resources")]
        [Tooltip(
            "UXML asset. If left empty, MenuController loads JamKitMenu.uxml from Resources (the wizard scaffolds an editable copy to Assets/_Project/UI/Resources).")]
        public VisualTreeAsset MenuUxml;

        [Tooltip("Optional USS to layer on top of the menu stylesheet.")]
        public StyleSheet ExtraStyles;

        [Header("Services")]
        public TimeServiceSO TimeService;
        [Required] public SceneServiceSO SceneService;
        public InputServiceSO InputService;

        [FormerlySerializedAs("MasterVolumeOverride")]
        [Header("Volume (Ripple Variables)")]
        [Tooltip("Optional — overrides Audio.MasterVolume if set. Otherwise pulls from the AudioServiceSO.")]
        public FloatVariableSO masterVolume;

        [FormerlySerializedAs("MusicVolumeOverride")] public FloatVariableSO musicVolume;
        [FormerlySerializedAs("SfxVolumeOverride")] public FloatVariableSO sfxVolume;

        [Header("Behaviour")] public SceneRef GameScene = new("Game");
        public SceneRef MainMenuScene = new("Bootstrap");

        [Tooltip("Which view is shown on Enable. None hides the canvas until you call ShowStart().")]
        public View InitialView = View.Start;

        [Header("Sounds (optional)")]
        public VoidEventSO playHoverSound;

        [Tooltip("Played on button click/submit.")]
        public VoidEventSO playClickSound;

        [Range(0f, 1f)] public float SoundVolume = 0.8f;

        [SerializeField, Required]
        private VoidEventSO onPauseOpened;
        [SerializeField, Required]
        private VoidEventSO onPauseClosed;
        
        UIDocument _doc;
        VisualElement _root;
        VisualElement _startView, _settingsView, _pauseView;
        MenuSettingsBinder _settings;

        readonly Stack<View> _stack = new();
        bool _pausedByMe;

        public View Current => _stack.Count > 0 ? _stack.Peek() : View.None;
        public VisualElement Root => _root;

        // -------------------- lifecycle --------------------

        void Awake()
        {
            _doc = GetComponent<UIDocument>();
            // The markup is project-owned (scaffolded to Assets/_Project/UI/Resources), so the
            // fallback finds the designer's copy — not a package asset they can't edit.
            if (MenuUxml == null) MenuUxml = Resources.Load<VisualTreeAsset>("JamKitMenu");
            if (MenuUxml == null)
                Debug.LogWarning(
                    "[JamKit] No JamKitMenu.uxml found in Resources. Run JamKit > New Jam Project to scaffold the menu template, or assign MenuUxml manually.",
                    this);
            if (_doc.visualTreeAsset == null && MenuUxml != null) _doc.visualTreeAsset = MenuUxml;
            if (_doc.panelSettings == null) _doc.panelSettings = JamKitUI.LoadOrCreateMenuPanelSettings();
            else JamKitUI.ApplyDefaultTheme(_doc.panelSettings);
        }

        void OnEnable()
        {
            _root = _doc.rootVisualElement;
            if (_root == null) return;

            EnsureStylesheet();
            CacheViews();
            WireButtons();
            WireSounds();
            _settings = new MenuSettingsBinder(_root, masterVolume, musicVolume, sfxVolume);

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
            _settings?.Dispose();
            _settings = null;
            // Safety: never leave the game frozen if the menu is disabled mid-pause.
            ResumeIfPaused();
        }

        // -------------------- public API --------------------

        [Button, DisableInEditorMode, FoldoutGroup("Debug"), HorizontalGroup("Debug/Row")]
        public void ShowStart() => ResetStack(View.Start);

        public void ShowSettings() => Push(View.Settings);
        public void ShowPause() => Push(View.Pause);
        public void HideAll() => ResetStack(View.None);

        public bool IsPauseOpen => Current == View.Pause;

        [Button, DisableInEditorMode, HorizontalGroup("Debug/Row")]
        public void TogglePause()
        {
            if (IsPauseOpen)
            {
                Pop();
                ResumeIfPaused();
                InputService?.SwitchToGameplay();
            }
            else
            {
                Push(View.Pause);
                PauseIfNot();
                InputService?.SwitchToUI();
            }
        }

        /// <summary>
        /// Back/cancel (e.g. the Esc key, routed via <see cref="PauseController"/>): if a submenu is
        /// stacked, pop it; otherwise toggle the pause menu. Fixes the case where Esc inside
        /// Pause → Settings used to push a second Pause instead of returning to Pause.
        /// </summary>
        public void HandleBack()
        {
            if (_stack.Count > 1)
            {
                Pop();
                return;
            } // Settings (over Start or Pause) → back one level.

            if (Current == View.Start) return; // Main menu root: nothing to back out of.
            TogglePause(); // Gameplay → open pause; Pause → resume.
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
            SetDisplay(_startView, top == View.Start);
            SetDisplay(_settingsView, top == View.Settings);
            SetDisplay(_pauseView, top == View.Pause);
            if (top == View.Settings) _settings?.SyncToUI();
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
                View.Start => _root.Q<Button>(N.StartPlay),
                View.Pause => _root.Q<Button>(N.PauseResume),
                View.Settings => _settings?.FirstFocus ?? _root.Q<Button>(N.SettingsBack),
                _ => null
            };
            // Schedule so the element is laid out (display:flex applied) before we focus it.
            target?.schedule.Execute(() => target.Focus());
        }

        // -------------------- caching --------------------

        void CacheViews()
        {
            _startView = _root.Q<VisualElement>(N.StartView);
            _settingsView = _root.Q<VisualElement>(N.SettingsView);
            _pauseView = _root.Q<VisualElement>(N.PauseView);
        }

        void EnsureStylesheet()
        {
            // JamKitMenu.uxml references its sibling JamKitMenu.uss via a relative <Style>, so the
            // stylesheet is applied when the UXML loads — we don't re-add it here (that would be a
            // second source of truth for the same asset). If you point MenuUxml at a custom UXML,
            // reference your stylesheet from that UXML, or layer one via ExtraStyles below.
            if (ExtraStyles != null && !_root.styleSheets.Contains(ExtraStyles))
                _root.styleSheets.Add(ExtraStyles);
        }

        // -------------------- buttons --------------------
        void WireButtons()
        {
            Bind(N.StartPlay, () => SceneService?.LoadAsync(GameScene));
            Bind(N.StartSettings, ShowSettings);
            Bind(N.StartQuit, QuitGame);

            Bind(N.SettingsBack, Pop);

            Bind(N.PauseResume, TogglePause);
            Bind(N.PauseSettings, ShowSettings);
            Bind(N.PauseRestart, () =>
            {
                ResumeIfPaused();
                SceneService?.ReloadCurrent();
            });
            Bind(N.PauseMainMenu, () =>
            {
                ResumeIfPaused();
                SceneService?.LoadAsync(MainMenuScene);
            });

            // Quit is meaningless in a browser; hide it so WebGL jam builds don't ship a dead button.
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                var quit = _root.Q<Button>(N.StartQuit);
                if (quit != null) quit.style.display = DisplayStyle.None;
            }
        }

        readonly HashSet<string> _warnedButtons = new();

        void Bind(string elementName, Action action)
        {
            var btn = _root.Q<Button>(elementName);
            if (btn == null)
            {
                // A name that doesn't match the UXML binds nothing — a dead control, silently. Warn
                // once per name (survives re-enable) so UXML/const drift is visible.
                if (_warnedButtons.Add(elementName))
                    Debug.LogWarning(
                        $"[JamKit] MenuController: no button '{elementName}' in the menu UXML — that control does nothing. " +
                        "Check JamKitMenu.uxml has an element with this name, or that MenuUxml points at the right asset.",
                        this);
                return;
            }

            btn.clicked += action;
        }

        /// <summary>
        /// Hover/click sounds on every button in the document. FocusIn covers gamepad/keyboard
        /// navigation (the controller equivalent of hover). Safe to re-run: the UIDocument
        /// rebuilds its tree on enable, so callbacks never stack.
        /// </summary>
        void WireSounds()
        {
            if (playHoverSound == null && playClickSound == null) 
                return;
            
            _root.Query<Button>().ForEach(b =>
            {
                // if (playHoverSound != null)
                // {
                //     b.RegisterCallback<PointerEnterEvent>(_ => AudioService.PlaySfx(playHoverSound, SoundVolume, 0.03f));
                //     b.RegisterCallback<FocusInEvent>(_ => AudioService.PlaySfx(playHoverSound, SoundVolume, 0.03f));
                // }
                //
                // if (playClickSound != null)
                //     b.RegisterCallback<ClickEvent>(_ => AudioService.PlaySfx(playClickSound, SoundVolume, 0.03f));
            });
        }

        static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // -------------------- pause helpers --------------------

        void PauseIfNot()
        {
            if (!_pausedByMe && TimeService != null)
            {
                TimeService.Pause();
                onPauseOpened?.Invoke();
                _pausedByMe = true;
            }
        }

        void ResumeIfPaused()
        {
            if (_pausedByMe && TimeService != null)
            {
                TimeService.Resume();
                onPauseClosed?.Invoke();
                _pausedByMe = false;
            }
        }
    }
}