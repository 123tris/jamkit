using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Spawns a burst of prefabs on demand — death explosions, asteroid splitting, loot drops,
    /// brick debris. A plain spawner with no trigger opinions: wire <c>Health.OnDied →
    /// Burst()</c> for death debris, a Ripple EventListener for global triggers, or call from
    /// code. <see cref="LaunchSpeed"/> flings spawned rigidbodies outward from the center; pair
    /// spawns with <see cref="AutoDespawn"/> so debris cleans up.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpawnBurst : MonoBehaviour
    {
        [Header("Service")]
        [Tooltip("Optional. Spawns through the pool when assigned.")]
        public PoolServiceSO PoolService;

        [Header("Spawn")]
        public GameObject Prefab;
        [Tooltip("How many to spawn. Constant or a shared Ripple variable.")]
        public IntReference Count = new(3);
        [Tooltip("Random offset radius around this transform. Constant or a shared Ripple variable.")]
        public FloatReference Scatter = new(0.4f);
        [Tooltip("Scatter on the XY plane (2D) instead of a sphere (3D).")]
        public bool Is2D = false;
        [Tooltip("Give each spawn a random rotation.")]
        public bool RandomRotation = true;

        [Header("Launch")]
        [Tooltip("Fling spawned rigidbodies outward at this speed. 0 = leave them still. Constant or a shared Ripple variable.")]
        public FloatReference LaunchSpeed = new(0f);

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void Burst() => Burst(1f);

        /// <summary>Burst with a count multiplier (wire a FloatEvent or damage amount here for bigger bangs).</summary>
        public void Burst(float countMultiplier)
        {
            if (Prefab == null) return;
            int count = Mathf.Max(1, Mathf.RoundToInt(Count.Value * countMultiplier));
            Vector3 center = transform.position;

            for (int i = 0; i < count; i++)
            {
                Vector3 offset = Is2D
                    ? (Vector3)(Random.insideUnitCircle * Scatter.Value)
                    : Random.insideUnitSphere * Scatter.Value;
                Quaternion rot = !RandomRotation ? transform.rotation
                    : Is2D ? Quaternion.Euler(0f, 0f, Random.Range(0f, 360f))
                    : Random.rotation;

                var go = PoolService != null
                    ? PoolService.Spawn(Prefab, center + offset, rot)
                    : Instantiate(Prefab, center + offset, rot);
                if (go == null || LaunchSpeed.Value <= 0f) continue;

                Vector3 dir = offset.sqrMagnitude > 0.0001f
                    ? offset.normalized
                    : (Is2D ? (Vector3)Random.insideUnitCircle.normalized : Random.onUnitSphere);
                Motor.LaunchBody(go, dir * LaunchSpeed.Value);
            }
        }
    }
}
