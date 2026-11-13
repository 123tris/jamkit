using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PanelRenderer))]
    public sealed class Toast : MonoBehaviour
    {
        [Tooltip("Used by Show(float) and Show(int). {0} is the value.")]
        public string Format = "{0}";

        [Min(0.1f)] public float Duration = 1.6f;

        public bool Unscaled = true;

        PanelRenderer panel;
        Label label;

        void OnEnable()
        {
            panel = GetComponent<PanelRenderer>();
            panel.RegisterUIReloadCallback(OnUIReload);
        }

        void OnDisable() => panel.UnregisterUIReloadCallback(OnUIReload);

        void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
        {
            root.pickingMode = PickingMode.Ignore;
            label = root.Q<Label>("toast-message");
            label.style.opacity = 0f;
        }

        [Button, DisableInEditorMode]
        public void Show(string message)
        {
            if (label == null) return;
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
