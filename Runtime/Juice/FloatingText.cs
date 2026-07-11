using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Emits a floating number/word above this object through a scene <see cref="FloatingTextLayer"/>.
    /// Defaults to sibling damage with <see cref="JuiceBehaviour.ScaleByEventValue"/> on, so the
    /// actual damage amount is what gets printed. Use <see cref="ShowText(string)"/> for words
    /// ("PERFECT!", "+1 UP") from UltEvents or code.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FloatingText : JuiceBehaviour
    {
        [Header("Output")]
        [Tooltip("Layer that renders the labels — one per scene. Auto-assigned on add when the scene has exactly one.")]
        public FloatingTextLayer Layer;
        [Tooltip("Format for the triggering value. '{0:0}' prints the damage amount, '-{0:0}' adds a minus.")]
        public string Format = "{0:0}";
        public Color Color = new(1f, 0.85f, 0.3f);
        [Tooltip("Where the text spawns, relative to this transform.")]
        public Vector3 WorldOffset = new(0f, 1f, 0f);
        [Tooltip("Label size multiplier on the layer's base font size.")]
        [Min(0.1f)] public float Scale = 1f;

        protected override bool DefaultOnSiblingDamage => true;

        protected override void Reset()
        {
            base.Reset();
            ScaleByEventValue = true; // the value IS the content here, unlike shake/flash strength
        }

        public override void Play(float strength)
        {
            if (Layer == null) return;
            Layer.Show(string.Format(Format, strength), transform.position + WorldOffset, Color, Scale);
        }

        /// <summary>Show an arbitrary string instead of the formatted value.</summary>
        public void ShowText(string text)
        {
            if (Layer == null) return;
            Layer.Show(text, transform.position + WorldOffset, Color, Scale);
        }
    }
}
