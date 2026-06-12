using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    /// <summary>
    /// Full-screen color overlay for scene transitions. Self-builds its own UIDocument +
    /// PanelSettings at Awake so all you need to do is add this component to a GameObject
    /// (typically the same one as <see cref="SceneServiceRunner"/>) and reference it from
    /// the runner.
    /// </summary>
    public sealed class FadeOverlay : MonoBehaviour
    {
        [Tooltip("Sort order for the fade panel. Keep above your other UIDocuments so the fade always covers.")]
        public int SortingOrder = 32767;

        UIDocument _doc;
        VisualElement _layer;

        // Shared across instances/scene loads — one fade panel config is enough, and recreating a
        // PanelSettings per scene load both leaks SOs and triggers the no-theme warning.
        static PanelSettings _panelSettings;

        void Awake()
        {
            _doc = gameObject.GetComponent<UIDocument>();
            if (_doc == null) _doc = gameObject.AddComponent<UIDocument>();

            if (_panelSettings == null)
            {
                _panelSettings = JamKitUI.CreatePanelSettings(PanelScaleMode.ConstantPixelSize, SortingOrder);
                _panelSettings.name = "JamKitFadePanelSettings";
            }
            _panelSettings.sortingOrder = SortingOrder;
            _doc.panelSettings = _panelSettings;

            var root = _doc.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            _layer = new VisualElement { name = "fade-layer", pickingMode = PickingMode.Ignore };
            _layer.style.position = Position.Absolute;
            _layer.style.left = 0; _layer.style.top = 0; _layer.style.right = 0; _layer.style.bottom = 0;
            _layer.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
            root.Add(_layer);
        }

        public Coroutine FadeTo(Color color, float seconds) => StartCoroutine(FadeRoutine(color, 1f, seconds));
        public Coroutine FadeOut(float seconds)
        {
            var c = _layer.resolvedStyle.backgroundColor;
            return StartCoroutine(FadeRoutine(new Color(c.r, c.g, c.b, c.a), 0f, seconds));
        }

        IEnumerator FadeRoutine(Color color, float targetAlpha, float seconds)
        {
            if (_layer == null) yield break;
            seconds = Mathf.Max(0.0001f, seconds);
            Color from = _layer.resolvedStyle.backgroundColor;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / seconds);
                var c = Color.Lerp(from, new Color(color.r, color.g, color.b, targetAlpha), p);
                _layer.style.backgroundColor = c;
                yield return null;
            }
            _layer.style.backgroundColor = new Color(color.r, color.g, color.b, targetAlpha);
        }
    }
}
