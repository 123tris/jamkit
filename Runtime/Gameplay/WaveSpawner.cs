using System;
using System.Collections;
using System.Collections.Generic;
using Ripple;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Plays through a sequence of waves; each wave spawns N copies of a prefab over a duration,
    /// then waits before the next wave. Fires Ripple events on wave boundaries.
    /// </summary>
    public sealed class WaveSpawner : MonoBehaviour
    {
        [Serializable]
        public struct Wave
        {
            public GameObject Prefab;
            [Min(1)] public int Count;
            [Min(0f)] public float Duration;
            [Min(0f)] public float DelayBefore;
        }

        [Header("Service")]
        public PoolServiceSO PoolService;

        [Header("Setup")]
        public List<Wave> Waves = new();
        public Transform SpawnPoint;
        public Vector2 Jitter = Vector2.zero;
        public bool AutoStart = true;

        [Header("Events (Ripple)")]
        public IntEvent OnWaveStarted;
        public IntEvent OnWaveEnded;
        public VoidEventSO OnAllWavesDone;

        Coroutine _routine;

        void OnEnable() { if (AutoStart) Begin(); }

        public void Begin()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(Run());
        }

        public void StopWaves()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
        }

        IEnumerator Run()
        {
            var origin = SpawnPoint != null ? SpawnPoint : transform;
            for (int i = 0; i < Waves.Count; i++)
            {
                var w = Waves[i];
                if (w.DelayBefore > 0f) yield return new WaitForSeconds(w.DelayBefore);
                if (OnWaveStarted != null) OnWaveStarted.Invoke(i);

                if (w.Prefab != null && w.Count > 0)
                {
                    float perSpawn = w.Duration <= 0f ? 0f : w.Duration / w.Count;
                    for (int c = 0; c < w.Count; c++)
                    {
                        var pos = origin.position + new Vector3(UnityEngine.Random.Range(-Jitter.x, Jitter.x), 0f, UnityEngine.Random.Range(-Jitter.y, Jitter.y));
                        if (PoolService != null) PoolService.Spawn(w.Prefab, pos, origin.rotation);
                        else Instantiate(w.Prefab, pos, origin.rotation);
                        if (perSpawn > 0f) yield return new WaitForSeconds(perSpawn);
                    }
                }

                if (OnWaveEnded != null) OnWaveEnded.Invoke(i);
            }
            if (OnAllWavesDone != null) OnAllWavesDone.Invoke();
            _routine = null;
        }
    }
}
