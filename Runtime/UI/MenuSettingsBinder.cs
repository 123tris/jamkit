using System.Collections.Generic;
using Ripple;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    /// <summary>
    /// Owns the Settings view's controls for <see cref="MenuController"/>: two-way volume sliders
    /// bound to Ripple variables, and graphics options persisted to PlayerPrefs. Plain class (not a
    /// MonoBehaviour) so MenuController's inspector/serialized API is unchanged — this is purely
    /// code organization. Construct it with the menu root; call <see cref="Dispose"/> from
    /// OnDisable to release the variable subscriptions.
    /// On WebGL the Resolution and VSync rows are hidden (the browser owns both).
    /// </summary>
    internal sealed class MenuSettingsBinder
    {
        // Element names — must match JamKitMenu.uxml.
        const string MasterName = "settings-master", MusicName = "settings-music", SfxName = "settings-sfx";
        const string QualityName = "settings-quality", ResolutionName = "settings-resolution";
        const string FullscreenName = "settings-fullscreen", VSyncName = "settings-vsync";

        const string PrefsQuality = "JamKit.Gfx.Quality";
        const string PrefsFullscreen = "JamKit.Gfx.Fullscreen";
        const string PrefsResolution = "JamKit.Gfx.ResolutionIndex";
        const string PrefsVSync = "JamKit.Gfx.VSync";

        readonly FloatVariableSO _masterVar, _musicVar, _sfxVar;
        readonly List<System.Action> _teardown = new();
        readonly bool _isWebGL;

        Slider _master, _music, _sfx;
        DropdownField _quality, _resolution;
        Toggle _fullscreen, _vsync;
        Resolution[] _resolutions;

        /// <summary>The control keyboard/gamepad focus should land on when the view opens (may be null).</summary>
        public VisualElement FirstFocus => _master;

        public MenuSettingsBinder(VisualElement root, FloatVariableSO master, FloatVariableSO music, FloatVariableSO sfx)
        {
            _masterVar = master; _musicVar = music; _sfxVar = sfx;
            _isWebGL = Application.platform == RuntimePlatform.WebGLPlayer;

            _master     = root.Q<Slider>(MasterName);
            _music      = root.Q<Slider>(MusicName);
            _sfx        = root.Q<Slider>(SfxName);
            _quality    = root.Q<DropdownField>(QualityName);
            _resolution = root.Q<DropdownField>(ResolutionName);
            _fullscreen = root.Q<Toggle>(FullscreenName);
            _vsync      = root.Q<Toggle>(VSyncName);

            Wire();
        }

        /// <summary>Release the Ripple variable subscriptions (element callbacks die with the visual tree).</summary>
        public void Dispose()
        {
            for (int i = 0; i < _teardown.Count; i++) _teardown[i]?.Invoke();
            _teardown.Clear();
        }

        // -------------------- wiring --------------------

        void Wire()
        {
            BuildResolutions();
            BuildQualityChoices();

            HookVolumeSlider(_master, _masterVar);
            HookVolumeSlider(_music, _musicVar);
            HookVolumeSlider(_sfx, _sfxVar);

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

            if (_isWebGL)
            {
                // The browser owns resolution and vsync; showing dead controls just confuses players.
                HideRow(_resolution);
                HideRow(_vsync);
            }

            // Apply persisted state once.
            QualitySettings.SetQualityLevel(Mathf.Clamp(PlayerPrefs.GetInt(PrefsQuality, QualitySettings.GetQualityLevel()), 0, QualitySettings.names.Length - 1), true);
            if (!_isWebGL)
            {
                QualitySettings.vSyncCount = PlayerPrefs.GetInt(PrefsVSync, QualitySettings.vSyncCount > 0 ? 1 : 0);
                ApplyScreen();
            }
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

            // variable → slider (lives on the persistent SO; must be torn down via Dispose).
            void OnVarChanged(float v)
            {
                if (slider.panel != null && !Mathf.Approximately(slider.value, v)) slider.SetValueWithoutNotify(v);
            }
            variable.OnValueChanged += OnVarChanged;
            _teardown.Add(() => variable.OnValueChanged -= OnVarChanged);

            slider.SetValueWithoutNotify(variable.CurrentValue);
        }

        static void HideRow(VisualElement control)
        {
            // Controls sit inside a .jk-row container with their label; hide the whole row.
            var row = control?.parent;
            if (row != null) row.style.display = DisplayStyle.None;
        }

        // -------------------- choices --------------------

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

        // -------------------- sync + apply --------------------

        /// <summary>Push current values into the controls (call when the Settings view opens).</summary>
        public void SyncToUI()
        {
            if (_master != null && _masterVar != null) _master.SetValueWithoutNotify(_masterVar.CurrentValue);
            if (_music  != null && _musicVar  != null) _music.SetValueWithoutNotify(_musicVar.CurrentValue);
            if (_sfx    != null && _sfxVar    != null) _sfx.SetValueWithoutNotify(_sfxVar.CurrentValue);
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
            if (_isWebGL) return; // browser owns the canvas size
            if (_resolutions == null || _resolutions.Length == 0) return;
            int i = Mathf.Clamp(PlayerPrefs.GetInt(PrefsResolution, 0), 0, _resolutions.Length - 1);
            var r = _resolutions[i];
            var mode = PlayerPrefs.GetInt(PrefsFullscreen, Screen.fullScreen ? 1 : 0) != 0
                ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            Screen.SetResolution(r.width, r.height, mode, r.refreshRateRatio);
        }
    }
}
