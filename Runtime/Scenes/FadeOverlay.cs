using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    internal sealed class FadeOverlay : MonoBehaviour
    {
        const int SortingOrder = 32767;

        VisualElement layer;

        public float Alpha { get; private set; }

        void Awake()
        {
            var document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = JamKitUI.CreatePanelSettings(PanelScaleMode.ConstantPixelSize, SortingOrder);

            VisualElement root = document.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            layer = new VisualElement { pickingMode = PickingMode.Ignore };
            layer.StretchToParentSize();
            root.Add(layer);
        }

        public void Show(Color color, float alpha)
        {
            Alpha = alpha;
            layer.style.backgroundColor = color.WithA(alpha);
        }
    }
}
