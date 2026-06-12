using Ripple;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Fires a projectile prefab on an interval, either while the Attack input is held or
    /// automatically. Spawns through a <see cref="PoolServiceSO"/> when assigned (pair the
    /// projectile with <see cref="AutoDespawn"/> so it returns to the pool). Sets the spawned
    /// Rigidbody's velocity along the muzzle's facing.
    /// </summary>
    public sealed class ProjectileShooter : MonoBehaviour
    {
        [Header("Services")]
        public PoolServiceSO PoolService;
        [Tooltip("Required only when UseAttackInput is true.")]
        public InputServiceSO InputService;

        [Header("Projectile")]
        public GameObject ProjectilePrefab;
        [Tooltip("Spawn point + direction. Defaults to this transform. 3D fires along +forward, 2D along +right.")]
        public Transform Muzzle;
        public float Speed = 12f;
        [Min(0.01f)] public float FireInterval = 0.2f;

        [Header("Mode")]
        [Tooltip("Fire while the Attack action is held. When false, fires automatically on the interval.")]
        public bool UseAttackInput = true;
        [Tooltip("Drive a Rigidbody2D (true) or a 3D Rigidbody (false).")]
        public bool Is2D = false;

        [Header("Events (Ripple)")]
        public VoidEventSO OnFired;

        float _next;

        void Update()
        {
            bool wantFire = !UseAttackInput
                || (InputService != null && InputService.Attack != null && InputService.Attack.IsPressed());
            if (!wantFire || ProjectilePrefab == null) return;
            if (Time.time < _next) return;
            Fire();
            _next = Time.time + FireInterval;
        }

        public GameObject Fire()
        {
            var muzzle = Muzzle != null ? Muzzle : transform;
            var go = PoolService != null
                ? PoolService.Spawn(ProjectilePrefab, muzzle.position, muzzle.rotation)
                : Instantiate(ProjectilePrefab, muzzle.position, muzzle.rotation);

            if (go != null)
            {
                if (Is2D)
                {
                    var rb = go.GetComponent<Rigidbody2D>();
                    if (rb != null) rb.linearVelocity = (Vector2)muzzle.right * Speed;
                }
                else
                {
                    var rb = go.GetComponent<Rigidbody>();
                    if (rb != null) rb.linearVelocity = muzzle.forward * Speed;
                }
            }

            if (OnFired != null) OnFired.Invoke();
            return go;
        }
    }
}
