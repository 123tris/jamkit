using System.Collections.Generic;
using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Spawns a prefab at this transform on a fixed interval. Routes through a <see cref="PoolServiceSO"/>
    /// for reuse. If no pool service is assigned, falls back to <see cref="Object.Instantiate"/> per spawn.
    /// </summary>
    public sealed class Spawner : MonoBehaviour
    {
        [Header("Service")]
        public PoolServiceSO PoolService;

        [Header("Spawn")]
        [Required] public GameObject Prefab;
        [Tooltip("Seconds between spawns. Constant or a shared Ripple variable (difficulty ramp).")]
        public FloatReference Interval = new(1f);
        public bool AutoStart = true;
        [Tooltip("Max instances alive at once. -1 = unlimited. Counts active spawned instances; despawned/destroyed ones free a slot. Constant or a shared Ripple variable.")]
        public IntReference MaxAlive = new(-1);
        public Vector2 Jitter = Vector2.zero;

        float _next;
        bool _running;
        readonly List<GameObject> _spawned = new();

        void OnEnable() { if (AutoStart) Begin(); }

        public void Begin() { _running = true; _next = Time.time + Interval.Value; }
        public void Stop()  { _running = false; }

        void Update()
        {
            if (!_running || Prefab == null) return;
            if (MaxAlive.Value >= 0 && CountAlive() >= MaxAlive.Value) { _next = Time.time + Interval.Value; return; }
            if (Time.time < _next) return;
            SpawnOne();
            _next = Time.time + Interval.Value;
        }

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public GameObject SpawnOne()
        {
            var pos = transform.position + new Vector3(Random.Range(-Jitter.x, Jitter.x), 0f, Random.Range(-Jitter.y, Jitter.y));
            var go = PoolService != null
                ? PoolService.Spawn(Prefab, pos, transform.rotation)
                : Instantiate(Prefab, pos, transform.rotation);
            if (go != null) _spawned.Add(go);
            return go;
        }

        /// <summary>Live count of spawned instances, pruning any that were destroyed or despawned (inactive).</summary>
        public int CountAlive()
        {
            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                var go = _spawned[i];
                if (go == null || !go.activeInHierarchy) _spawned.RemoveAt(i);
            }
            return _spawned.Count;
        }
    }
}
