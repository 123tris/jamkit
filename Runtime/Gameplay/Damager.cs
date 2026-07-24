using System;
using System.Collections.Generic;
using Metz.JamKit.Utils;
using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Deals damage to any <see cref="Health"/> on objects that collide or trigger with this one.
    /// One component for 2D and 3D — whichever physics engine fires. Covers projectiles
    /// (DestroyOnHit + pool), contact hazards, and melee swings (<see cref="OncePerTarget"/> +
    /// enable/disable the collider with the attack animation).
    /// </summary>
    public sealed class Damager : MonoBehaviour
    {
        [Tooltip("Damage per hit. A constant, or a shared Ripple variable so a buff/difficulty knob scales every hitter at once.")]
        public FloatReference Damage = new(1f);
        public LayerMask TargetLayers = ~0;
        [Tooltip("Hit each Health at most once until this damager is re-enabled — melee swings, one-touch traps.")]
        public bool OncePerTarget = false;
        public bool DestroyOnHit = false;
        [Tooltip("Optional. When assigned, DestroyOnHit returns this object to the pool instead of destroying it.")]
        public PoolServiceSO PoolService;
        
        readonly HashSet<Health> _hit = new();

        void OnEnable() => _hit.Clear();

        /// <summary>Clear the once-per-target memory, e.g. at the start of each melee swing.</summary>
        public void ResetHits() => _hit.Clear();

        void OnCollisionEnter(Collision c) => TryHit(c.collider.gameObject);
        void OnTriggerEnter(Collider c) => TryHit(c.gameObject);
        void OnCollisionEnter2D(Collision2D c) => TryHit(c.collider.gameObject);
        void OnTriggerEnter2D(Collider2D c) => TryHit(c.gameObject);

        void TryHit(GameObject other)
        {
            if (!TargetLayers.IsInLayerMask(other)) return;
            
            var h = other.GetComponent<Health>();
            if (h == null) h = other.GetComponentInParent<Health>();
            if (h == null) return;
            if (OncePerTarget && !_hit.Add(h)) return;
            h.Damage(Damage.Value);
            if (!DestroyOnHit) return;
            if (PoolService != null) PoolService.Despawn(gameObject);
            else Destroy(gameObject);
        }
    }
}
