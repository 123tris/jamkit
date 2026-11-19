using System.Collections.Generic;
using Ripple;
using UnityEngine;

namespace Metz.JamKit
{
    public sealed class Damager : MonoBehaviour
    {
        public FloatReference Damage = new(1f);

        [Tooltip("Kill outright, ignoring Damage.")]
        public bool Kill;

        public LayerMask TargetLayers = ~0;

        [Tooltip("Hit each Health once until this component is re-enabled.")]
        public bool OncePerTarget;

        public bool DestroyOnHit;

        readonly HashSet<Health> hit = new();

        void OnEnable() => hit.Clear();

        public void ResetHits() => hit.Clear();

        void OnCollisionEnter(Collision collision) => TryHit(collision.gameObject);
        void OnTriggerEnter(Collider other) => TryHit(other.gameObject);
        void OnCollisionEnter2D(Collision2D collision) => TryHit(collision.gameObject);
        void OnTriggerEnter2D(Collider2D other) => TryHit(other.gameObject);

        void TryHit(GameObject other)
        {
            if (!TargetLayers.Contains(other.layer)) return;
            var health = other.GetComponentInParent<Health>();
            if (health == null || (OncePerTarget && !hit.Add(health))) return;

            if (Kill) health.Kill();
            else health.Damage(Damage.Value);

            if (DestroyOnHit) Destroy(gameObject);
        }
    }
}
