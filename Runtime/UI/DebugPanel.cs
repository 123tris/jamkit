using System;
using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class DebugPanel : MonoBehaviour
    {
        const int SortingOrder = 30000;

        [Required] public TimeServiceSO TimeService;
        [Required] public SceneServiceSO SceneService;

        [Tooltip("Browsers swallow function keys, so avoid those for WebGL builds.")]
        public Key ToggleKey = Key.Backquote;

        public bool VisibleAtStart;

        VisualElement root;
        Label fps;
        float elapsed;
        int frames;

        void Awake() => GetComponent<UIDocument>().panelSettings =
            JamKitUI.CreatePanelSettings(PanelScaleMode.ConstantPixelSize, SortingOrder);

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            Build();
            SetVisible(VisibleAtStart);
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard[ToggleKey].wasPressedThisFrame) SetVisible(root.style.display == DisplayStyle.None);

            elapsed += Time.unscaledDeltaTime;
            frames++;
            if (elapsed < 0.5f) return;
            fps.text = $"FPS: {frames / elapsed:0}";
            elapsed = 0f;
            frames = 0;
        }

        void Build()
        {
            root.Clear();

            var panel = new VisualElement();
            panel.style.position = Position.Absolute;
            panel.style.top = panel.style.left = 8;
            panel.style.paddingLeft = panel.style.paddingRight = 12;
            panel.style.paddingTop = panel.style.paddingBottom = 12;
            panel.style.minWidth = 220;
            panel.style.backgroundColor = new Color(0f, 0f, 0f, 0.8f);
            root.Add(panel);

            fps = new Label("FPS: --");
            fps.style.color = Color.white;
            panel.Add(fps);

            var timeScale = new Slider("Time Scale", 0f, 2f) { value = 1f };
            timeScale.RegisterValueChangedCallback(change => TimeService.BaseScale = change.newValue);
            panel.Add(timeScale);

            panel.Add(new Button(SceneService.Reload) { text = "Reload Scene" });
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string sceneName = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
                panel.Add(new Button(() => SceneService.Load(sceneName)) { text = $"Load: {sceneName}" });
            }
            panel.Add(new Button(Quit) { text = "Quit" });
        }

        void SetVisible(bool visible) => root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

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
