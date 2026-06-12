using System.Collections.Generic;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Pool service. Holds one pool per prefab. A scene-side <see cref="PoolServiceRunner"/>
    /// provides the parent transform that idle instances live under (with DontDestroyOnLoad if you want).
    /// Without a runner, Spawn still works but instances are parented to no transform (the active scene root).
    /// </summary>
    [CreateAssetMenu(menuName = "JamKit/Services/Pool Service", fileName = "PoolService")]
    public sealed class PoolServiceSO : ScriptableObject
    {
        readonly Dictionary<GameObject, GameObjectPool> _pools = new();
        readonly Dictionary<GameObject, GameObjectPool> _instanceToPool = new();
        Transform _root;

        internal void SetRoot(Transform t) { _root = t; }
        internal void ClearRoot(Transform t) { if (_root == t) _root = null; }

        /// <summary>
        /// Forget all pools. The runner calls this each play session so stale entries (whose
        /// instances were destroyed with the previous scene/session) don't linger when Domain
        /// Reload is disabled. Does not destroy live instances.
        /// </summary>
        public void ResetState() { _pools.Clear(); _instanceToPool.Clear(); }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;
            if (!_pools.TryGetValue(prefab, out var pool))
            {
                pool = new GameObjectPool(prefab, _root);
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
                pool = new GameObjectPool(prefab, _root, prewarm: count);
                _pools[prefab] = pool;
            }
            else
            {
                for (int i = 0; i < count; i++) pool.Despawn(Object.Instantiate(prefab, _root));
            }
        }

        void OnDisable()
        {
            _pools.Clear();
            _instanceToPool.Clear();
            _root = null;
        }
    }
}
