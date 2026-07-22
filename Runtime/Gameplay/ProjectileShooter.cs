using Ripple;
using Sirenix.OdinInspector;
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
        [Required] public GameObject ProjectilePrefab;
        [Tooltip("Spawn point + direction. Defaults to this transform. 3D fires along +forward, 2D along +right.")]
        public Transform Muzzle;
        [Tooltip("Muzzle velocity. Constant or a shared Ripple variable (weapon upgrades).")]
        public FloatReference Speed = new(12f);
        [Tooltip("Seconds between shots. Constant or a shared Ripple variable (fire-rate buffs).")]
        public FloatReference FireInterval = new(0.2f);

        [Header("Mode")]
        [Tooltip("Fire while the Attack action is held. When false, fires automatically on the interval.")]
        public bool UseAttackInput = true;
        [Tooltip("Drive a Rigidbody2D (true) or a 3D Rigidbody (false).")]
        public bool Is2D = false;

        [FoldoutGroup("Broadcast (Ripple, global)")]
        [Tooltip("Optional — any shot sharing this event (muzzle SFX, ammo counters).")]
        public VoidEventSO BroadcastFired;

        float _next;

        void Update()
        {
            bool wantFire = !UseAttackInput
                || (InputService != null && InputService.Attack != null && InputService.Attack.IsPressed());
            if (!wantFire || ProjectilePrefab == null) return;
            if (Time.time < _next) return;
            Fire();
            _next = Time.time + FireInterval.Value;
        }

        public GameObject Fire()
        {
            var muzzle = Muzzle != null ? Muzzle : transform;
            var go = PoolService != null
                ? PoolService.Spawn(ProjectilePrefab, muzzle.position, muzzle.rotation)
                : Instantiate(ProjectilePrefab, muzzle.position, muzzle.rotation);

            // Is2D picks the facing axis (muzzle.right vs muzzle.forward); the motor applies it to
            // whichever body the projectile has.
            if (go != null)
                Motor.LaunchBody(go, (Is2D ? muzzle.right : muzzle.forward) * Speed.Value);

            if (BroadcastFired != null) BroadcastFired.Invoke();
            return go;
        }
    }
}
