using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Deals damage to any <see cref="Health"/> on objects that collide or trigger with this one.
    /// Works for 3D physics (default). For 2D, use <see cref="Damager2D"/>.
    /// </summary>
    public sealed class Damager : MonoBehaviour
    {
        public float Damage = 1f;
        public LayerMask TargetLayers = ~0;
        public bool DestroyOnHit = false;

        void OnCollisionEnter(Collision c) => TryHit(c.collider.gameObject);
        void OnTriggerEnter(Collider c) => TryHit(c.gameObject);

        void TryHit(GameObject other)
        {
            if (((1 << other.layer) & TargetLayers) == 0) return;
            var h = other.GetComponent<Health>();
            if (h == null) h = other.GetComponentInParent<Health>();
            if (h == null) return;
            h.Damage(Damage);
            if (DestroyOnHit) Destroy(gameObject);
        }
    }

    /// <summary>2D variant of <see cref="Damager"/>.</summary>
    public sealed class Damager2D : MonoBehaviour
    {
        public float Damage = 1f;
        public LayerMask TargetLayers = ~0;
        public bool DestroyOnHit = false;

        void OnCollisionEnter2D(Collision2D c) => TryHit(c.collider.gameObject);
        void OnTriggerEnter2D(Collider2D c) => TryHit(c.gameObject);

        void TryHit(GameObject other)
        {
            if (((1 << other.layer) & TargetLayers) == 0) return;
            var h = other.GetComponent<Health>();
            if (h == null) h = other.GetComponentInParent<Health>();
            if (h == null) return;
            h.Damage(Damage);
            if (DestroyOnHit) Destroy(gameObject);
        }
    }
}
