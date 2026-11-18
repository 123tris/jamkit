using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    internal sealed class FadeOverlay : MonoBehaviour
    {
        PanelRenderer panel;
        VisualElement layer;
        Color color = Color.black;

        public float Alpha { get; private set; }

        public static FadeOverlay Create(string owner)
        {
            var child = new GameObject($"Fade ({owner})");
            child.transform.SetParent(JamKitHost.Root, false);
            return child.AddComponent<FadeOverlay>();
        }

        public void Load(VisualTreeAsset document, PanelSettings settings)
        {
            if (panel != null) return;
            panel = gameObject.AddComponent<PanelRenderer>();
            panel.RegisterUIReloadCallback(OnUIReload);
            panel.panelSettings = settings;
            panel.visualTreeAsset = document;
        }

        public void Show(Color tint, float alpha)
        {
            color = tint;
            Alpha = alpha;
            Apply();
        }

        void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
        {
            root.pickingMode = PickingMode.Ignore;
            layer = root.Q("fade");
            Apply();
        }

        void Apply()
        {
            if (layer != null) layer.style.backgroundColor = color.WithA(Alpha);
        }
    }
}
