using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Damage flash for sprites: tints <see cref="SpriteRenderer.color"/> to <see cref="FlashColor"/>
    /// and fades back. Uses the renderer color (a vertex color), so it needs no special shader and
    /// allocates nothing. Note: tinting multiplies the sprite, so flash-to-white only shows on
    /// non-white sprites — the red default is visible on anything.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpriteFlash : JuiceBehaviour
    {
        [Header("Flash")]
        [Tooltip("Renderers to tint. Empty = all SpriteRenderers on this object and children (found on Awake).")]
        public SpriteRenderer[] Renderers;
        public Color FlashColor = Color.red;
        [Min(0.01f)] public float Duration = 0.12f;
        [Tooltip("Unscaled time so the flash still animates during a HitStop freeze.")]
        public bool Unscaled = true;

        Color[] _baseColors;
        float _t = -1f;

        protected override bool DefaultOnSiblingDamage => true;

        void Awake()
        {
            if (Renderers == null || Renderers.Length == 0)
                Renderers = GetComponentsInChildren<SpriteRenderer>(true);
            _baseColors = new Color[Renderers.Length];
            for (int i = 0; i < Renderers.Length; i++)
                if (Renderers[i] != null) _baseColors[i] = Renderers[i].color;
        }

        public override void Play(float strength) => _t = 0f;

        void Update()
        {
            if (_t < 0f) return;
            _t += (Unscaled ? Time.unscaledDeltaTime : Time.deltaTime) / Duration;
            bool done = _t >= 1f;
            float blend = done ? 0f : 1f - _t;
            for (int i = 0; i < Renderers.Length; i++)
            {
                if (Renderers[i] == null) continue;
                Renderers[i].color = Color.Lerp(_baseColors[i], FlashColor, blend);
            }
            if (done) _t = -1f;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            // Restore instantly so a pooled despawn mid-flash doesn't leave a tinted sprite.
            if (_t >= 0f && Renderers != null)
                for (int i = 0; i < Renderers.Length; i++)
                    if (Renderers[i] != null) Renderers[i].color = _baseColors[i];
            _t = -1f;
        }
    }
}
