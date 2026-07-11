using System;
using System.Collections.Generic;
using Ripple;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    /// <summary>
    /// Center-screen banner text ("Wave 2!", "New High Score!") bound to Ripple events — the
    /// no-code way to narrate game state. Map <see cref="VoidEventSO"/>s to fixed messages and
    /// <see cref="IntEvent"/>s to format strings, or call <see cref="Show(string)"/> from code.
    /// Self-builds its UIDocument like <see cref="FadeOverlay"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Toast : MonoBehaviour
    {
        [Serializable]
        public class VoidMessage
        {
            public VoidEventSO Event;
            public string Message = "Go!";
        }

        [Serializable]
        public class IntMessage
        {
            public IntEvent Event;
            [Tooltip("{0} is replaced with the event's value.")]
            public string Format = "Wave {0}!";
        }

        [Header("Messages")]
        public VoidMessage[] Messages;
        public IntMessage[] IntMessages;

        [Header("Style")]
        [Min(0.1f)] public float Duration = 1.6f;
        [Min(1)] public int FontSize = 44;
        public Color Color = Color.white;
        [Tooltip("Vertical position, 0 = top, 1 = bottom.")]
        [Range(0f, 1f)] public float ScreenY = 0.28f;
        [Tooltip("Unscaled time so toasts survive pause/hit-stop.")]
        public bool Unscaled = true;

        static PanelSettings _panelSettings;

        UIDocument _doc;
        Label _label;
        float _age = -1f;
        readonly List<(VoidEventSO evt, Action handler)> _voidSubs = new();
        readonly List<(IntEvent evt, Action<int> handler)> _intSubs = new();

        void Awake()
        {
            _doc = GetComponent<UIDocument>();
            if (_doc == null) _doc = gameObject.AddComponent<UIDocument>();
            if (_panelSettings == null)
            {
                _panelSettings = JamKitUI.CreatePanelSettings(PanelScaleMode.ScaleWithScreenSize, 60);
                _panelSettings.name = "JamKitToastPanelSettings";
            }
            _doc.panelSettings = _panelSettings;

            var root = _doc.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            _label = new Label { pickingMode = PickingMode.Ignore };
            _label.style.position = Position.Absolute;
            _label.style.left = 0;
            _label.style.right = 0;
            _label.style.top = Length.Percent(ScreenY * 100f);
            _label.style.unityTextAlign = TextAnchor.MiddleCenter;
            _label.style.unityFontStyleAndWeight = FontStyle.Bold;
            _label.style.fontSize = FontSize;
            _label.style.color = Color;
            _label.style.display = DisplayStyle.None;
            root.Add(_label);
        }

        void OnEnable()
        {
            if (Messages != null)
                foreach (var m in Messages)
                {
                    if (m?.Event == null) continue;
                    var msg = m; // capture per entry, not the loop variable's final value
                    Action handler = () => Show(msg.Message);
                    msg.Event.AddListener(handler);
                    _voidSubs.Add((msg.Event, handler));
                }
            if (IntMessages != null)
                foreach (var m in IntMessages)
                {
                    if (m?.Event == null) continue;
                    var msg = m;
                    Action<int> handler = v => Show(string.Format(msg.Format, v));
                    msg.Event.AddListener(handler);
                    _intSubs.Add((msg.Event, handler));
                }
        }

        void OnDisable()
        {
            foreach (var (evt, handler) in _voidSubs) evt.RemoveListener(handler);
            foreach (var (evt, handler) in _intSubs) evt.RemoveListener(handler);
            _voidSubs.Clear();
            _intSubs.Clear();
        }

        public void Show(string message)
        {
            if (_label == null) return;
            _label.text = message;
            _label.style.display = DisplayStyle.Flex;
            _label.style.opacity = 1f;
            _age = 0f;
        }

        void Update()
        {
            if (_age < 0f) return;
            _age += Unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            float life01 = _age / Duration;
            if (life01 >= 1f)
            {
                _label.style.display = DisplayStyle.None;
                _age = -1f;
                return;
            }
            // Hold, then fade out over the last 30%.
            _label.style.opacity = life01 > 0.7f ? 1f - (life01 - 0.7f) / 0.3f : 1f;
        }
    }
}
