using System.Collections.Generic;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// One pool per prefab. Spawned instances re-use prior copies; despawn keeps them inactive.
    /// Implementing <see cref="IPoolable"/> on any component opts into spawn/despawn callbacks.
    /// </summary>
    public sealed class GameObjectPool
    {
        readonly GameObject _prefab;
        readonly Transform _parent;
        readonly Stack<GameObject> _idle = new();
        readonly int _maxIdle;
        static readonly List<IPoolable> _scratch = new();

        public GameObject Prefab => _prefab;
        public int CountIdle => _idle.Count;

        public GameObjectPool(GameObject prefab, Transform parent = null, int prewarm = 0, int maxIdle = 256)
        {
            _prefab = prefab;
            _parent = parent;
            _maxIdle = maxIdle;
            for (int i = 0; i < prewarm; i++)
            {
                var go = Object.Instantiate(prefab, _parent);
                go.SetActive(false);
                _idle.Push(go);
            }
        }

        public GameObject Spawn(Vector3 position, Quaternion rotation)
        {
            GameObject go;
            if (_idle.Count > 0)
            {
                go = _idle.Pop();
                go.transform.SetPositionAndRotation(position, rotation);
            }
            else
            {
                go = Object.Instantiate(_prefab, position, rotation, _parent);
            }
            go.SetActive(true);
            InvokeSpawn(go);
            return go;
        }

        public void Despawn(GameObject go)
        {
            if (go == null) return;
            InvokeDespawn(go);
            go.SetActive(false);
            if (_idle.Count < _maxIdle) _idle.Push(go);
            else Object.Destroy(go);
        }

        static void InvokeSpawn(GameObject go)
        {
            go.GetComponents(_scratch);
            for (int i = 0; i < _scratch.Count; i++) _scratch[i].OnSpawn();
            _scratch.Clear();
        }

        static void InvokeDespawn(GameObject go)
        {
            go.GetComponents(_scratch);
            for (int i = 0; i < _scratch.Count; i++) _scratch[i].OnDespawn();
            _scratch.Clear();
        }
    }
}
