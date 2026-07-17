using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    /// <summary>
    /// In-game debug overlay toggled by a key — the only debug surface that exists in BUILDS
    /// (WebGL jam testing has no inspector; see PILLARS.md). Self-builds a top-sorted UIDocument
    /// and shows an FPS readout, a time-scale slider, quick-jump buttons for every scene in
    /// Build Settings, reload, and quit. Lives on JamKitCore by default. Strip it from release
    /// builds by removing the component or gating with a scripting define.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class DebugPanel : MonoBehaviour
    {
        [Header("Services")]
        public TimeServiceSO TimeService;
        public SceneServiceSO SceneService;

        [Header("Toggle")]
        [Tooltip("Backquote (`) by default — browsers swallow F-keys, so avoid those for WebGL jam builds.")]
        public Key ToggleKey = Key.Backquote;
        public bool VisibleAtStart = false;

        UIDocument _doc;
        Label _fps;
        bool _visible;
        float _accum;
        int _frames;

        void Awake()
        {
            _doc = GetComponent<UIDocument>();
            if (_doc.panelSettings == null)
                _doc.panelSettings = JamKitUI.CreatePanelSettings(PanelScaleMode.ConstantPixelSize, 30000); // above menus, below the fade overlay
            else
                JamKitUI.ApplyDefaultTheme(_doc.panelSettings);
        }

        void OnEnable()
        {
            Build();
            SetVisible(VisibleAtStart);
        }

        void Build()
        {
            var root = _doc.rootVisualElement;
            if (root == null) return;
            root.Clear();

            var panel = new VisualElement();
            panel.style.position = Position.Absolute;
            panel.style.top = 8; panel.style.left = 8;
            panel.style.paddingLeft = panel.style.paddingRight = 12;
            panel.style.paddingTop = panel.style.paddingBottom = 12;
            panel.style.minWidth = 220;
            panel.style.backgroundColor = new Color(0f, 0f, 0f, 0.8f);
            panel.style.borderTopLeftRadius = panel.style.borderBottomRightRadius = 8;
            root.Add(panel);

            _fps = MakeLabel("FPS: --");
            panel.Add(_fps);

            var ts = new Slider("Time Scale", 0f, 2f) { value = 1f };
            ts.style.marginTop = 8;
            ts.RegisterValueChangedCallback(e =>
            {
                if (TimeService != null) TimeService.BaseScale = e.newValue;
                else Time.timeScale = e.newValue;
            });
            panel.Add(ts);

            panel.Add(MakeButton("Reload Scene", () => SceneService?.ReloadCurrent()));
            // Every scene in Build Settings gets a jump button — no list to maintain.
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var sceneName = System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
                panel.Add(MakeButton($"Load: {sceneName}", () => SceneService?.LoadAsync(sceneName)));
            }
            panel.Add(MakeButton("Quit", Quit));
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb[ToggleKey].wasPressedThisFrame) SetVisible(!_visible);

            _accum += Time.unscaledDeltaTime;
            _frames++;
            if (_accum >= 0.5f)
            {
                if (_fps != null) _fps.text = $"FPS: {_frames / _accum:0}";
                _accum = 0f;
                _frames = 0;
            }
        }

        void SetVisible(bool v)
        {
            _visible = v;
            var root = _doc.rootVisualElement;
            if (root != null) root.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;
        }

        static Label MakeLabel(string t)
        {
            var l = new Label(t);
            l.style.color = Color.white;
            l.style.fontSize = 14;
            return l;
        }

        static Button MakeButton(string t, System.Action a)
        {
            var b = new Button(a) { text = t };
            b.style.marginTop = 4;
            return b;
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
