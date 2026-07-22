using Ripple;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Returns this object to its <see cref="PoolServiceSO"/> (or destroys it) after a delay.
    /// Pool-aware: the timer resets on every spawn via <see cref="IPoolable"/>, so reused
    /// instances get a fresh lifetime. The #1 fix for bullets/particles that leak forever.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AutoDespawn : MonoBehaviour, IPoolable
    {
        [Tooltip("Pool to return to. If null, the object is destroyed instead.")]
        public PoolServiceSO PoolService;
        [Tooltip("Lifetime before despawn/destroy. Constant or a shared Ripple variable.")]
        public FloatReference Seconds = new(3f);
        [Tooltip("Use unscaled time so the lifetime ignores pause/slow-mo.")]
        public bool Unscaled = false;

        float _due;

        void OnEnable() => Arm();
        public void OnSpawn() => Arm();
        public void OnDespawn() { }

        void Arm() => _due = Now() + Seconds.Value;

        void Update()
        {
            if (Now() < _due) return;
            if (PoolService != null) PoolService.Despawn(gameObject);
            else Destroy(gameObject);
        }

        float Now() => Unscaled ? Time.unscaledTime : Time.time;
    }
}
