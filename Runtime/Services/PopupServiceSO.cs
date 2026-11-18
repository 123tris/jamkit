using Metz.JamKit.Inspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    [CreateAssetMenu(menuName = "JamKit/Services/Popups", fileName = "Popups")]
    public sealed class PopupServiceSO : ServiceSO
    {
        [Required, Tooltip("The overlay popups draw on. Its element named popups holds them.")]
        public VisualTreeAsset Document;

        [Required, Tooltip("Panel the popups draw on. Keep its sort order under the menus.")]
        public PanelSettings Panel;

        [Min(0.1f)] public float Seconds = 0.8f;

        [Tooltip("World units a popup rises over its life.")]
        public float Rise = 1f;

        [Tooltip("Keep rising and fading while time is paused or frozen by a hit stop.")]
        public bool Unscaled = true;

        PopupLayer layer;

        [ShowInInspector]
        public int Showing => layer == null ? 0 : layer.Count;

        public void Show(string text, Vector3 world, Color color)
        {
            if (layer == null) layer = PopupLayer.Create(this);
            layer.Show(text, world, color);
        }

        [Button]
        public void ShowInFrontOfCamera(string text)
        {
            Camera camera = Camera.main;
            if (camera != null) Show(text, camera.transform.position + camera.transform.forward * 5f, Color.white);
        }
    }
}
