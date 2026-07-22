using Ripple;
using Sirenix.OdinInspector;
using UltEvents;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Instant-hit gun: raycasts from <see cref="AimOrigin"/> along +forward and damages the first
    /// <see cref="Health"/> hit — the FPS/immersive-sim counterpart to <see cref="ProjectileShooter"/>
    /// (which stays the right tool for visible, dodgeable shots). Point <see cref="AimOrigin"/> at
    /// the first-person camera for center-screen aim; leave it null on a turret to fire along the
    /// muzzle. Spawns <see cref="ImpactPrefab"/> at the hit (pooled when a pool is assigned —
    /// pair it with <see cref="AutoDespawn"/>). 3D physics.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitscanShooter : MonoBehaviour
    {
        [Header("Services")]
        [Tooltip("Optional. Impact prefabs spawn through the pool when assigned.")]
        public PoolServiceSO PoolService;
        [Tooltip("Required only when UseAttackInput is true.")]
        public InputServiceSO InputService;

        [Header("Aim")]
        [Tooltip("Ray origin + direction (+forward). The FPS camera for center-screen aim. Defaults to this transform.")]
        public Transform AimOrigin;
        [Tooltip("Max ray distance. Constant or a shared Ripple variable.")]
        public FloatReference MaxDistance = new(80f);
        [Tooltip("What the ray can hit — include walls so cover blocks shots. Trigger colliders are ignored.")]
        public LayerMask HitLayers = ~0;

        [Header("Gun")]
        [Tooltip("Damage per hit. Constant or a shared Ripple variable (weapon upgrades).")]
        public FloatReference Damage = new(1f);
        [Tooltip("Seconds between shots. Constant or a shared Ripple variable (fire-rate buffs).")]
        public FloatReference FireInterval = new(0.14f);
        [Tooltip("Hold to keep firing. Off = one shot per press.")]
        public bool FullAuto = true;
        [Tooltip("Fire from the Attack action. Off = only via Fire() calls (turrets, wired triggers).")]
        public bool UseAttackInput = true;

        [Header("Impact")]
        [Tooltip("Optional — spawned at the hit point facing out along the surface normal. Pair with AutoDespawn.")]
        public GameObject ImpactPrefab;

        [FoldoutGroup("Events (this instance)")]
        [Tooltip("Every shot — wire muzzle flash / kick feedbacks (MMF_Player.PlayFeedbacks), SFX.")]
        public UltEvent OnFired;
        [FoldoutGroup("Events (this instance)")]
        [Tooltip("A shot damaged a Health (amount) — hit-marker feedback.")]
        public UltEvent<float> OnHit;

        [FoldoutGroup("Broadcast (Ripple, global)")]
        [Tooltip("Optional — any shot sharing this event (ammo counters, global SFX).")]
        public VoidEventSO BroadcastFired;

        float _next;

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")] string _lastShot = "(none)";

        void Update()
        {
            if (!UseAttackInput) return;
            var attack = InputService != null ? InputService.Attack : null;
            if (attack == null) return;

            bool wantFire = FullAuto ? attack.IsPressed() : attack.WasPressedThisFrame();
            if (!wantFire || Time.time < _next) return;
            Fire();
            _next = Time.time + FireInterval.Value;
        }

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void Fire()
        {
            var origin = AimOrigin != null ? AimOrigin : transform;

            OnFired?.Invoke();
            if (BroadcastFired != null) BroadcastFired.Invoke();

            if (!Physics.Raycast(origin.position, origin.forward, out var hit, MaxDistance.Value,
                    HitLayers, QueryTriggerInteraction.Ignore))
            {
                _lastShot = "miss";
                return;
            }

            _lastShot = $"{hit.collider.name} @ {hit.distance:0.0}m";

            var health = hit.collider.GetComponent<Health>();
            if (health == null) health = hit.collider.GetComponentInParent<Health>();
            if (health != null)
            {
                health.Damage(Damage.Value);
                OnHit?.Invoke(Damage.Value);
            }

            if (ImpactPrefab == null) return;
            var rot = Quaternion.LookRotation(hit.normal);
            var pos = hit.point + hit.normal * 0.01f;
            if (PoolService != null) PoolService.Spawn(ImpactPrefab, pos, rot);
            else Instantiate(ImpactPrefab, pos, rot);
        }
    }
}
