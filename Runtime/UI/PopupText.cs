using Metz.JamKit.Inspector;
using UnityEngine;

namespace Metz.JamKit
{
    public sealed class PopupText : MonoBehaviour
    {
        [Required] public PopupServiceSO Popups;

        [Tooltip("Used by Show(float) and Show(int). {0} is the value.")]
        public string Format = "{0:0}";

        public Color Color = Color.white;

        [Tooltip("Where a popup starts, relative to this object.")]
        public Vector3 Offset = new(0f, 1f, 0f);

        public void Show(float value) => Show(string.Format(Format, value));

        public void Show(int value) => Show(string.Format(Format, value));

        [Button]
        public void Show(string text) => Popups.Show(text, transform.position + Offset, Color);
    }
}
