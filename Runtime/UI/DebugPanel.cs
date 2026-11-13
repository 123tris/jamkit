using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    [RequireComponent(typeof(PanelRenderer))]
    public sealed class DebugPanel : MonoBehaviour
    {
        const string HiddenClass = "jk-debug--hidden";
        const string ButtonClass = "jk-debug__button";

        [Required] public TimeServiceSO TimeService;
        [Required] public SceneServiceSO SceneService;

        [Tooltip("Browsers swallow function keys, so avoid those for WebGL builds.")]
        public Key ToggleKey = Key.Backquote;

        public bool VisibleAtStart;

        PanelRenderer panel;
        VisualElement window;
        Label fps;
        int boundVersion = -1;
        bool visible;
        float elapsed;
        int frames;

        void OnEnable()
        {
            visible = VisibleAtStart;
            panel = GetComponent<PanelRenderer>();
            panel.RegisterUIReloadCallback(OnUIReload);
        }

        void OnDisable() => panel.UnregisterUIReloadCallback(OnUIReload);

        void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
        {
            if (version != boundVersion)
            {
                boundVersion = version;
                Bind(root);
            }

            SetVisible(visible);
        }

        void Bind(VisualElement root)
        {
            root.pickingMode = PickingMode.Ignore;
            window = root.Q("debug-panel");
            fps = root.Q<Label>("debug-fps");

            var timeScale = root.Q<Slider>("debug-time-scale");
            timeScale.SetValueWithoutNotify(TimeService.BaseScale);
            timeScale.RegisterValueChangedCallback(change => TimeService.BaseScale = change.newValue);
            root.Q<Button>("debug-reload").clicked += SceneService.Reload;
            root.Q<Button>("debug-quit").clicked += Quit;

            VisualElement scenes = root.Q("debug-scenes");
            scenes.Clear();
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string sceneName = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
                var load = new Button(() => SceneService.Load(sceneName)) { text = $"Load: {sceneName}" };
                load.AddToClassList(ButtonClass);
                scenes.Add(load);
            }
        }

        void Update()
        {
            if (window == null) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard[ToggleKey].wasPressedThisFrame) SetVisible(!visible);

            elapsed += Time.unscaledDeltaTime;
            frames++;
            if (elapsed < 0.5f) return;
            fps.text = $"FPS: {frames / elapsed:0}";
            elapsed = 0f;
            frames = 0;
        }

        void SetVisible(bool on)
        {
            visible = on;
            window.EnableInClassList(HiddenClass, !on);
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
