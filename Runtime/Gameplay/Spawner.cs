using System.Collections.Generic;
using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    public sealed class Spawner : MonoBehaviour
    {
        [Required] public GameObject Prefab;
        public FloatReference Interval = new(1f);
        public bool AutoStart = true;

        [Tooltip("-1 is unlimited.")]
        public IntReference MaxAlive = new(-1);

        [Tooltip("Random offset on the X and Z axes.")]
        public Vector2 Jitter;

        readonly List<GameObject> spawned = new();
        Cooldown cooldown;
        bool running;

        void OnEnable()
        {
            if (AutoStart) Begin();
        }

        public void Begin()
        {
            running = true;
            cooldown.Start(Interval.Value);
        }

        public void Stop() => running = false;

        void Update()
        {
            if (!running || !cooldown.Ready) return;
            cooldown.Start(Interval.Value);
            if (MaxAlive.Value < 0 || CountAlive() < MaxAlive.Value) SpawnOne();
        }

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public GameObject SpawnOne()
        {
            var offset = new Vector3(Random.Range(-Jitter.x, Jitter.x), 0f, Random.Range(-Jitter.y, Jitter.y));
            GameObject instance = Instantiate(Prefab, transform.position + offset, transform.rotation);
            spawned.Add(instance);
            return instance;
        }

        public int CountAlive()
        {
            spawned.RemoveAll(instance => instance == null || !instance.activeInHierarchy);
            return spawned.Count;
        }
    }
}
