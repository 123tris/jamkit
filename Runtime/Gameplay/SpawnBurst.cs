using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Spawns a burst of prefabs on trigger — death explosions, asteroid splitting, loot drops,
    /// brick debris. Defaults to sibling death, so dropping it on an enemy prefab with a debris
    /// prefab assigned is the whole setup. <see cref="LaunchSpeed"/> flings spawned rigidbodies
    /// outward from the center; pair spawns with <see cref="AutoDespawn"/> so debris cleans up.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpawnBurst : JuiceBehaviour
    {
        [Header("Service")]
        [Tooltip("Optional. Spawns through the pool when assigned.")]
        public PoolServiceSO PoolService;

        [Header("Spawn")]
        public GameObject Prefab;
        [Min(1)] public int Count = 3;
        [Tooltip("Random offset radius around this transform.")]
        [Min(0f)] public float Scatter = 0.4f;
        [Tooltip("Scatter on the XY plane (2D) instead of a sphere (3D).")]
        public bool Is2D = false;
        [Tooltip("Give each spawn a random rotation.")]
        public bool RandomRotation = true;

        [Header("Launch")]
        [Tooltip("Fling spawned rigidbodies outward at this speed. 0 = leave them still.")]
        [Min(0f)] public float LaunchSpeed = 0f;

        protected override bool DefaultOnSiblingDeath => true;

        public override void Play(float strength)
        {
            if (Prefab == null) return;
            int count = Mathf.Max(1, Mathf.RoundToInt(Count * strength));
            Vector3 center = transform.position;

            for (int i = 0; i < count; i++)
            {
                Vector3 offset = Is2D
                    ? (Vector3)(Random.insideUnitCircle * Scatter)
                    : Random.insideUnitSphere * Scatter;
                Quaternion rot = !RandomRotation ? transform.rotation
                    : Is2D ? Quaternion.Euler(0f, 0f, Random.Range(0f, 360f))
                    : Random.rotation;

                var go = PoolService != null
                    ? PoolService.Spawn(Prefab, center + offset, rot)
                    : Instantiate(Prefab, center + offset, rot);
                if (go == null || LaunchSpeed <= 0f) continue;

                Vector3 dir = offset.sqrMagnitude > 0.0001f
                    ? offset.normalized
                    : (Is2D ? (Vector3)Random.insideUnitCircle.normalized : Random.onUnitSphere);
                var rb2d = go.GetComponent<Rigidbody2D>();
                if (rb2d != null) { rb2d.linearVelocity = dir * LaunchSpeed; continue; }
                var rb = go.GetComponent<Rigidbody>();
                if (rb != null) rb.linearVelocity = dir * LaunchSpeed;
            }
        }
    }
}
