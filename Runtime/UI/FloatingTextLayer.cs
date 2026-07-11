using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    /// <summary>
    /// Pooled damage/score numbers on a UI Toolkit overlay — no TMP, no world-space canvas.
    /// One per scene; <see cref="FloatingText"/> emitters (or your code via <see cref="Show(string, Vector3)"/>)
    /// push labels that rise and fade while tracking their world position. Self-builds its
    /// UIDocument like <see cref="FadeOverlay"/> — just add the component.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FloatingTextLayer : MonoBehaviour
    {
        [Header("Camera")]
        [Tooltip("Camera used to project world → screen. Null = Camera.main (cached).")]
        public Camera WorldCamera;

        [Header("Text")]
        [Min(0.1f)] public float Lifetime = 0.8f;
        [Tooltip("Rise speed in panel pixels per second.")]
        public float RiseSpeed = 70f;
        [Min(1)] public int FontSize = 28;
        public Color DefaultColor = Color.white;
        [Tooltip("Unscaled time so numbers keep floating during a HitStop freeze.")]
        public bool Unscaled = true;

        class Entry
        {
            public Label Label;
            public Vector3 World;
            public float Age;
        }

        // One overlay config for all scenes (same rationale as FadeOverlay's shared PanelSettings).
        static PanelSettings _panelSettings;

        UIDocument _doc;
        VisualElement _root;
        readonly List<Entry> _active = new();
        readonly Stack<Entry> _idle = new();

        void Awake()
        {
            _doc = GetComponent<UIDocument>();
            if (_doc == null) _doc = gameObject.AddComponent<UIDocument>();
            if (_panelSettings == null)
            {
                _panelSettings = JamKitUI.CreatePanelSettings(PanelScaleMode.ScaleWithScreenSize, 50);
                _panelSettings.name = "JamKitFloatingTextPanelSettings";
            }
            _doc.panelSettings = _panelSettings;

            _root = _doc.rootVisualElement;
            _root.pickingMode = PickingMode.Ignore;
            if (WorldCamera == null) WorldCamera = Camera.main;
        }

        public void Show(string text, Vector3 worldPosition) => Show(text, worldPosition, DefaultColor, 1f);

        public void Show(string text, Vector3 worldPosition, Color color, float scale = 1f)
        {
            if (_root == null) return;
            var e = _idle.Count > 0 ? _idle.Pop() : NewEntry();
            e.World = worldPosition;
            e.Age = 0f;
            e.Label.text = text;
            e.Label.style.color = color;
            e.Label.style.fontSize = Mathf.RoundToInt(FontSize * Mathf.Max(0.1f, scale));
            e.Label.style.opacity = 1f;
            e.Label.style.display = DisplayStyle.Flex;
            _active.Add(e);
        }

        Entry NewEntry()
        {
            var label = new Label { pickingMode = PickingMode.Ignore };
            label.style.position = Position.Absolute;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            _root.Add(label);
            return new Entry { Label = label };
        }

        void Update()
        {
            if (_active.Count == 0) return;
            var cam = WorldCamera != null ? WorldCamera : (WorldCamera = Camera.main);
            if (cam == null) return;

            float dt = Unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            var panel = _root.panel;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var e = _active[i];
                e.Age += dt;
                if (e.Age >= Lifetime)
                {
                    e.Label.style.display = DisplayStyle.None;
                    _active.RemoveAt(i);
                    _idle.Push(e);
                    continue;
                }

                Vector2 p = RuntimePanelUtils.CameraTransformWorldToPanel(panel, e.World, cam);
                float rise = RiseSpeed * e.Age;
                e.Label.style.left = p.x - e.Label.resolvedStyle.width * 0.5f;
                e.Label.style.top = p.y - rise;

                float life01 = e.Age / Lifetime;
                e.Label.style.opacity = life01 > 0.6f ? 1f - (life01 - 0.6f) / 0.4f : 1f;
            }
        }
    }
}
