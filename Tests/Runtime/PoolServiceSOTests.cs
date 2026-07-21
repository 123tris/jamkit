using NUnit.Framework;
using UnityEngine;

namespace Metz.JamKit.Tests
{
    /// <summary>Counts pool lifecycle callbacks on instances cloned from the test prefab.</summary>
    internal sealed class PoolProbe : MonoBehaviour, IPoolable
    {
        public int Spawned, Despawned;
        public void OnSpawn() => Spawned++;
        public void OnDespawn() => Despawned++;
    }

    public class PoolServiceSOTests
    {
        PoolServiceSO _pool;
        GameObject _prefab;

        [SetUp]
        public void SetUp()
        {
            _pool = ScriptableObject.CreateInstance<PoolServiceSO>();
            _prefab = new GameObject("prefab");
            _prefab.AddComponent<PoolProbe>();
            _prefab.SetActive(false); // template only; clones are activated by the pool
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var probe in Object.FindObjectsByType<PoolProbe>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(probe.gameObject);
            Object.DestroyImmediate(_pool);
        }

        [Test]
        public void SpawnNullReturnsNull()
        {
            Assert.IsNull(_pool.Spawn(null));
            Assert.AreEqual(0, _pool.PoolCount);
        }

        [Test]
        public void SpawnDespawnSpawnReusesInstance()
        {
            var first = _pool.Spawn(_prefab, new Vector3(1, 2, 3), Quaternion.identity);
            Assert.IsTrue(first.activeSelf);
            _pool.Despawn(first);
            Assert.IsFalse(first.activeSelf);

            var second = _pool.Spawn(_prefab, new Vector3(4, 5, 6), Quaternion.identity);
            Assert.AreSame(first, second);
            Assert.IsTrue(second.activeSelf);
            Assert.AreEqual(new Vector3(4, 5, 6), second.transform.position);
        }

        [Test]
        public void DespawnUnknownInstanceDeactivatesIt()
        {
            var stray = new GameObject("stray");
            _pool.Despawn(stray);
            Assert.IsFalse(stray.activeSelf);
            Object.DestroyImmediate(stray);
        }

        [Test]
        public void CountsTrackPoolsAndInstances()
        {
            var otherPrefab = new GameObject("prefab2");
            otherPrefab.AddComponent<PoolProbe>();
            otherPrefab.SetActive(false);

            _pool.Spawn(_prefab);
            _pool.Spawn(_prefab);
            _pool.Spawn(otherPrefab);

            Assert.AreEqual(2, _pool.PoolCount);
            Assert.AreEqual(3, _pool.TrackedInstances);
        }

        [Test]
        public void PoolableCallbacksFireOnSpawnAndDespawn()
        {
            var inst = _pool.Spawn(_prefab);
            var probe = inst.GetComponent<PoolProbe>();
            Assert.AreEqual(1, probe.Spawned);
            Assert.AreEqual(0, probe.Despawned);

            _pool.Despawn(inst);
            Assert.AreEqual(1, probe.Despawned);
        }

        [Test]
        public void ResetStateForgetsPoolsAndTracking()
        {
            var inst = _pool.Spawn(_prefab);
            _pool.ResetState();
            Assert.AreEqual(0, _pool.PoolCount);
            Assert.AreEqual(0, _pool.TrackedInstances);

            // Previously-tracked instance now takes the unknown-instance path.
            _pool.Despawn(inst);
            Assert.IsFalse(inst.activeSelf);
        }

        [Test]
        public void SpawnParentsUnderRegisteredRunner()
        {
            var go = new GameObject("core");
            go.SetActive(false);
            var runner = go.AddComponent<PoolServiceRunner>();
            runner.Service = _pool;
            go.SetActive(true);

            var inst = _pool.Spawn(_prefab);
            Assert.AreSame(runner.transform, inst.transform.parent);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void PrewarmCreatesInactiveInstancesWithoutSpawnCallbacks()
        {
            _pool.Prewarm(_prefab, 2);
            Assert.AreEqual(1, _pool.PoolCount);
            Assert.AreEqual(0, _pool.TrackedInstances); // prewarmed instances are idle, not tracked

            var spawned = _pool.Spawn(_prefab);
            Assert.AreEqual(1, spawned.GetComponent<PoolProbe>().Spawned);
        }

        // Current behavior, changes in 0.10 (see plan): Prewarm lacks Spawn's null guard and
        // throws from the Dictionary lookup. Will become a no-op.
        [Test]
        public void PrewarmNullPrefabThrows_CurrentBehavior()
        {
            Assert.Throws<System.ArgumentNullException>(() => _pool.Prewarm(null, 1));
        }

        // Current behavior, changes in 0.10 (see plan): prewarming an EXISTING pool routes fresh
        // instances through pool.Despawn, firing a spurious OnDespawn on never-spawned clones
        // (the ctor prewarm path fires none).
        [Test]
        public void PrewarmOnExistingPoolFiresSpuriousDespawn_CurrentBehavior()
        {
            _pool.Despawn(_pool.Spawn(_prefab)); // create the pool
            _pool.Prewarm(_prefab, 1);

            int spuriousDespawns = 0;
            foreach (var probe in Object.FindObjectsByType<PoolProbe>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (probe.Spawned == 0 && probe.Despawned > 0 && probe.gameObject != _prefab)
                    spuriousDespawns++;
            Assert.AreEqual(1, spuriousDespawns);
        }
    }
}
