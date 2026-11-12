using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class Toast : MonoBehaviour
    {
        const int SortingOrder = 60;

        [Tooltip("Used by Show(float) and Show(int). {0} is the value.")]
        public string Format = "{0}";

        [Min(0.1f)] public float Duration = 1.6f;
        [Min(1)] public int FontSize = 44;
        public Color Color = Color.white;

        [Range(0f, 1f), Tooltip("0 is the top of the screen, 1 the bottom.")]
        public float ScreenY = 0.28f;

        public bool Unscaled = true;

        Label label;

        void Awake()
        {
            var document = GetComponent<UIDocument>();
            document.panelSettings = JamKitUI.CreatePanelSettings(PanelScaleMode.ScaleWithScreenSize, SortingOrder);

            VisualElement root = document.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;

            label = new Label { pickingMode = PickingMode.Ignore };
            label.style.position = Position.Absolute;
            label.style.left = label.style.right = 0;
            label.style.top = Length.Percent(ScreenY * 100f);
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = FontSize;
            label.style.color = Color;
            label.style.opacity = 0f;
            root.Add(label);
        }

        [Button, DisableInEditorMode]
        public void Show(string message)
        {
            label.text = message;
            StopAllCoroutines();
            StartCoroutine(Display());
        }

        public void Show(float value) => Show(string.Format(Format, value));

        public void Show(int value) => Show(string.Format(Format, value));

        IEnumerator Display()
        {
            const float fadeStart = 0.7f;
            for (float t = 0f; t < Duration; t += Unscaled ? Time.unscaledDeltaTime : Time.deltaTime)
            {
                label.style.opacity = 1f - MathX.Remap01(t / Duration, fadeStart, 1f);
                yield return null;
            }
            label.style.opacity = 0f;
        }
    }
}
