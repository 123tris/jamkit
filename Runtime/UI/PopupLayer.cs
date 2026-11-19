using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    internal sealed class PopupLayer : MonoBehaviour
    {
        const string PopupClass = "jk-popup";

        sealed class Popup
        {
            public Label Label;
            public Vector3 World;
            public float Age;
        }

        readonly List<Popup> live = new();
        PopupServiceSO service;
        PanelRenderer panel;
        VisualElement layer;

        public int Count => live.Count;

        public static PopupLayer Create(PopupServiceSO service)
        {
            var child = new GameObject($"Popups ({service.name})");
            child.transform.SetParent(JamKitHost.Root, false);
            var popups = child.AddComponent<PopupLayer>();
            popups.service = service;
            popups.panel = child.AddComponent<PanelRenderer>();
            popups.panel.RegisterUIReloadCallback(popups.OnUIReload);
            popups.panel.panelSettings = service.Panel;
            popups.panel.visualTreeAsset = service.Document;
            return popups;
        }

        public void Show(string text, Vector3 world, Color color)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(PopupClass);
            label.style.color = color;
            var popup = new Popup { Label = label, World = world };
            live.Add(popup);
            if (layer == null) return;
            layer.Add(label);
            Place(popup);
        }

        void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
        {
            root.pickingMode = PickingMode.Ignore;
            layer = root.Q("popups") ?? root;
            foreach (Popup popup in live) layer.Add(popup.Label);
        }

        void LateUpdate()
        {
            float delta = service.Unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            for (int i = live.Count - 1; i >= 0; i--)
            {
                Popup popup = live[i];
                popup.Age += delta;
                if (popup.Age < service.Seconds)
                {
                    Place(popup);
                    continue;
                }
                popup.Label.RemoveFromHierarchy();
                live.RemoveAt(i);
            }
        }

        void Place(Popup popup)
        {
            Camera camera = Camera.main;
            if (camera == null || popup.Label.panel == null) return;

            float progress = popup.Age / service.Seconds;
            Vector3 world = popup.World + Vector3.up * (service.Rise * Ease.OutCubic.Evaluate(progress));
            Vector2 position = RuntimePanelUtils.CameraTransformWorldToPanel(popup.Label.panel, world, camera);
            popup.Label.style.left = position.x;
            popup.Label.style.top = position.y;
            popup.Label.style.opacity = 1f - MathX.Remap01(progress, 0.6f, 1f);
        }
    }
}
