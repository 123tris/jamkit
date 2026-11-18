using System.Collections.Generic;
using Metz.JamKit.Inspector;
using Ripple;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    [RequireComponent(typeof(PanelRenderer))]
    public sealed class MenuController : MonoBehaviour
    {
        const string WebGLClass = "jk-webgl";

        [Header("Services"), Required] public TimeServiceSO TimeService;
        [Required] public InputServiceSO InputService;

        [Header("Input"), Tooltip("Gameplay action that opens the pause screen.")]
        public InputActionReference Pause;

        [Tooltip("UI action that closes the top screen.")]
        public InputActionReference Cancel;

        [Header("Volume"), FormerlySerializedAs("masterVolume")] public FloatVariableSO MasterVolume;
        [FormerlySerializedAs("musicVolume")] public FloatVariableSO MusicVolume;
        [FormerlySerializedAs("sfxVolume")] public FloatVariableSO SfxVolume;

        [Header("Screens")] public List<MenuView> Views = new();

        [Tooltip("Shown on enable. Empty starts hidden.")]
        public MenuView InitialView;

        [Tooltip("Opened by Pause.")]
        public MenuView PauseView;

        public MenuView SettingsView;

        readonly Stack<MenuView> stack = new();
        PanelRenderer panel;
        MenuSettingsBinder settings;
        int boundVersion = -1;
        bool showInitial;

        public MenuView Current => stack.Count > 0 ? stack.Peek() : null;

        void OnEnable()
        {
            showInitial = true;
            panel = GetComponent<PanelRenderer>();
            panel.RegisterUIReloadCallback(OnUIReload);

            if (Pause != null) Pause.action.performed += OnBack;
            if (Cancel != null) Cancel.action.performed += OnBack;
        }

        void OnDisable()
        {
            panel.UnregisterUIReloadCallback(OnUIReload);
            if (Pause != null) Pause.action.performed -= OnBack;
            if (Cancel != null) Cancel.action.performed -= OnBack;
            TimeService.Resume();
        }

        void OnDestroy() => settings?.Dispose();

        void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
        {
            if (version != boundVersion)
            {
                boundVersion = version;
                root.EnableInClassList(WebGLClass, Application.platform == RuntimePlatform.WebGLPlayer);
                settings?.Dispose();
                settings = new MenuSettingsBinder(root, MasterVolume, MusicVolume, SfxVolume);
                foreach (MenuView view in Views) view.Bind(root);
            }

            if (!showInitial)
            {
                Apply();
                return;
            }

            showInitial = false;
            ShowOnly(InitialView);
        }

        public void Show(MenuView view)
        {
            if (view == Current || !view.CanOpen.Value) return;
            stack.Push(view);
            Apply();
        }

        public void Back()
        {
            if (stack.Count == 0) return;
            stack.Pop();
            Apply();
        }

        public void ShowOnly(MenuView view)
        {
            stack.Clear();
            if (view != null && view.CanOpen.Value) stack.Push(view);
            Apply();
        }

        public void ShowSettings() => Show(SettingsView);

        public void ShowPause() => Show(PauseView);

        [Button]
        public void HideAll() => ShowOnly(null);

        [Button]
        public void TogglePause()
        {
            if (Current == PauseView) Back();
            else Show(PauseView);
        }

        public void HandleBack()
        {
            if (stack.Count > 1) Back();
            else if (stack.Count == 0 || Current == PauseView) TogglePause();
        }

        [Button("Quit")]
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void OnBack(InputAction.CallbackContext context) => HandleBack();

        void Apply()
        {
            MenuView top = Current;
            foreach (MenuView view in Views) view.SetVisible(view == top);

            bool pausesTime = false;
            foreach (MenuView view in stack) pausesTime |= view.PausesTime;
            if (pausesTime) TimeService.Pause();
            else TimeService.Resume();

            if (top != null && top.SwitchesToUIInput) InputService.SwitchToUI();
            else InputService.SwitchToGameplay();

            if (top == null) return;
            if (top == SettingsView) settings?.SyncToUI();
            top.Focus();
        }
    }
}
