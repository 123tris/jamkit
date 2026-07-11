using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Squash-and-stretch pop: snaps the transform to <see cref="Punch"/> × its base scale, then
    /// eases back. Struct math only — this is deliberately not a tween library (that's Feel's job).
    /// Punch &gt; 1 pops (pickups, score), Punch &lt; 1 squashes (landing, hit).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PunchScale : JuiceBehaviour
    {
        [Header("Punch")]
        [Tooltip("Scale multiplier at the peak. 1.25 = pop out, 0.8 = squash in.")]
        public float Punch = 1.25f;
        [Min(0.01f)] public float Duration = 0.18f;
        [Tooltip("Unscaled time so the pop still animates during a HitStop freeze.")]
        public bool Unscaled = true;

        Vector3 _base;
        bool _hasBase;
        float _peak = 1f;
        float _t = -1f;

        protected override bool DefaultOnSiblingDamage => true;

        void Awake()
        {
            _base = transform.localScale;
            _hasBase = true;
        }

        public override void Play(float strength)
        {
            if (!_hasBase) { _base = transform.localScale; _hasBase = true; }
            _peak = 1f + (Punch - 1f) * strength;
            _t = 0f;
            transform.localScale = _base * _peak;
        }

        void Update()
        {
            if (_t < 0f) return;
            _t += (Unscaled ? Time.unscaledDeltaTime : Time.deltaTime) / Duration;
            if (_t >= 1f)
            {
                transform.localScale = _base;
                _t = -1f;
                return;
            }
            // Ease-out cubic back to base — snappy start, soft settle.
            float eased = 1f - Mathf.Pow(1f - _t, 3f);
            transform.localScale = Vector3.Lerp(_base * _peak, _base, eased);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_t >= 0f && _hasBase) transform.localScale = _base;
            _t = -1f;
        }
    }
}
