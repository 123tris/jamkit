using Ripple;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Base for the Juice Lite receivers (CameraShake, HitStop, SpriteFlash, PunchScale, …).
    /// Three ways to trigger, all optional and combinable:
    ///   1. Sibling <see cref="Health"/> — per-instance damage/death on this object (or a parent),
    ///      zero wiring needed. This is the default for most receivers.
    ///   2. Ripple event assets — global triggers (any-damage screen shake, wave stingers).
    ///   3. <see cref="Play()"/> — call from UltEvents, UnityEvents, Feel, or code.
    /// Graduating to Feel later: point the same triggers at an MMF_Player and delete nothing.
    /// Adding receivers from code at runtime? The sibling-trigger defaults come from Reset(),
    /// which is editor-only — set <see cref="OnSiblingDamage"/>/<see cref="OnSiblingDeath"/>
    /// yourself before OnEnable runs (e.g. add to an inactive GameObject, then activate).
    /// </summary>
    public abstract class JuiceBehaviour : MonoBehaviour
    {
        [Header("Trigger")]
        [Tooltip("Play when a Health on this object (or a parent) takes damage. Per-instance, zero wiring.")]
        public bool OnSiblingDamage;
        [Tooltip("Play when a Health on this object (or a parent) dies.")]
        public bool OnSiblingDeath;
        [Tooltip("Optional global trigger (e.g. OnWaveStarted).")]
        public VoidEventSO PlayOn;
        [Tooltip("Optional global trigger carrying a value (e.g. a shared damage event).")]
        public FloatEvent PlayOnFloat;
        [Tooltip("Multiply the effect by the triggering value (damage amount / float event payload). Off = every trigger plays at strength 1.")]
        public bool ScaleByEventValue;

        Health _health;

        /// <summary>Editor-time default: should this receiver hook sibling damage when first added?</summary>
        protected virtual bool DefaultOnSiblingDamage => false;
        /// <summary>Editor-time default: should this receiver hook sibling death when first added?</summary>
        protected virtual bool DefaultOnSiblingDeath => false;

        protected virtual void Reset()
        {
            OnSiblingDamage = DefaultOnSiblingDamage;
            OnSiblingDeath = DefaultOnSiblingDeath;
        }

        protected virtual void OnEnable()
        {
            if (OnSiblingDamage || OnSiblingDeath)
            {
                _health = GetComponentInParent<Health>();
                if (_health != null)
                {
                    if (OnSiblingDamage) _health.Damaged += HandleDamaged;
                    if (OnSiblingDeath) _health.Died += HandleDied;
                }
            }
            if (PlayOn != null) PlayOn.AddListener(Play);
            if (PlayOnFloat != null) PlayOnFloat.AddListener(HandleFloat);
        }

        protected virtual void OnDisable()
        {
            if (_health != null)
            {
                _health.Damaged -= HandleDamaged;
                _health.Died -= HandleDied;
                _health = null;
            }
            if (PlayOn != null) PlayOn.RemoveListener(Play);
            if (PlayOnFloat != null) PlayOnFloat.RemoveListener(HandleFloat);
        }

        void HandleDamaged(float amount) => Play(ScaleByEventValue ? amount : 1f);
        void HandleDied() => Play(1f);
        void HandleFloat(float value) => Play(ScaleByEventValue ? value : 1f);

        /// <summary>Trigger at authored strength — wire this from UltEvents / UnityEvents / Feel.</summary>
        public void Play() => Play(1f);

        /// <summary>Trigger with a strength multiplier (1 = authored values).</summary>
        public abstract void Play(float strength);
    }
}
