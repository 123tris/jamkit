using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Pool service. Holds one pool per prefab. A scene-side <see cref="PoolServiceRunner"/>
    /// provides the parent transform that idle instances live under.
    /// Without a runner, Spawn still works but instances are parented to the active scene root.
    /// </summary>
    [CreateAssetMenu(menuName = "JamKit/Services/Pool Service", fileName = "PoolService")]
    public sealed class PoolServiceSO : ServiceSO<PoolServiceRunner>
    {
        readonly Dictionary<GameObject, GameObjectPool> _pools = new();
        readonly Dictionary<GameObject, GameObjectPool> _instanceToPool = new();

        Transform Root => Runner != null ? Runner.transform : null;

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")]
        public int PoolCount => _pools.Count;

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")]
        public int TrackedInstances => _instanceToPool.Count;

        /// <summary>
        /// Forget all pools. Runs each play session and on runner (re)enable so stale entries
        /// (whose instances died with the previous scene/session) don't linger when Domain
        /// Reload is disabled. Does not destroy live instances.
        /// </summary>
        public override void ResetState() { _pools.Clear(); _instanceToPool.Clear(); }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;
            if (!_pools.TryGetValue(prefab, out var pool))
            {
                pool = new GameObjectPool(prefab, Root);
                _pools[prefab] = pool;
            }
            var inst = pool.Spawn(position, rotation);
            _instanceToPool[inst] = pool;
            return inst;
        }

        public GameObject Spawn(GameObject prefab) => Spawn(prefab, Vector3.zero, Quaternion.identity);

        public void Despawn(GameObject instance)
        {
            if (instance == null) return;
            if (_instanceToPool.TryGetValue(instance, out var pool))
            {
                pool.Despawn(instance);
                return;
            }
            instance.SetActive(false);
        }

        public void Prewarm(GameObject prefab, int count)
        {
            if (!_pools.TryGetValue(prefab, out var pool))
            {
                pool = new GameObjectPool(prefab, Root, prewarm: count);
                _pools[prefab] = pool;
            }
            else
            {
                for (int i = 0; i < count; i++) pool.Despawn(Object.Instantiate(prefab, Root));
            }
        }

        protected override void OnDisable()
        {
            _pools.Clear();
            _instanceToPool.Clear();
            base.OnDisable();
        }
    }
}
