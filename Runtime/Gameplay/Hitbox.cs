using System.Collections.Generic;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Damage-dealing trigger volume. On overlap with a <see cref="Hurtbox"/> on a target layer it
    /// applies <see cref="Damage"/> to that hurtbox's <see cref="Health"/>. More flexible than
    /// <see cref="Damager"/>: lets one Health own several hurtboxes (weak points), and limits each
    /// swing to one hit per target. Works for 2D and 3D triggers. Needs a trigger collider.
    /// </summary>
    public sealed class Hitbox : MonoBehaviour
    {
        public float Damage = 1f;
        public LayerMask TargetLayers = ~0;
        [Tooltip("Hit each hurtbox at most once until this hitbox is re-enabled — good for a melee swing.")]
        public bool OncePerTarget = true;
        public bool DestroyOnHit = false;

        readonly HashSet<Hurtbox> _hit = new();

        /// <summary>Clear the per-swing hit set, e.g. at the start of each attack.</summary>
        public void ResetSwing() => _hit.Clear();

        void OnEnable() => _hit.Clear();

        void OnTriggerEnter(Collider c) => TryHit(c);
        void OnTriggerEnter2D(Collider2D c) => TryHit(c);

        void TryHit(Component other)
        {
            if (((1 << other.gameObject.layer) & TargetLayers) == 0) return;

            var hurt = other.GetComponent<Hurtbox>();
            if (hurt == null) hurt = other.GetComponentInParent<Hurtbox>();
            if (hurt == null) return;

            if (OncePerTarget && !_hit.Add(hurt)) return;
            hurt.Receive(Damage);
            if (DestroyOnHit) Destroy(gameObject);
        }
    }

    /// <summary>
    /// Receiving end for <see cref="Hitbox"/>. Put on a trigger collider and point at the
    /// <see cref="Health"/> hits should apply to (defaults to one on this object or a parent).
    /// Use <see cref="DamageMultiplier"/> for weak points / armor.
    /// </summary>
    public sealed class Hurtbox : MonoBehaviour
    {
        [Tooltip("Health that hits on this box apply to. Defaults to a Health on this object or a parent.")]
        public Health Health;
        [Min(0f)] public float DamageMultiplier = 1f;

        void Awake() { if (Health == null) Health = GetComponentInParent<Health>(); }

        public void Receive(float damage)
        {
            if (Health != null) Health.Damage(damage * DamageMultiplier);
        }
    }
}
