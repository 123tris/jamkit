using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Metz.JamKit.Tests
{
    public class GameObjectPoolTests
    {
        GameObject _prefab;

        [SetUp]
        public void SetUp()
        {
            _prefab = new GameObject("prefab");
            _prefab.AddComponent<PoolProbe>();
            _prefab.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var probe in Object.FindObjectsByType<PoolProbe>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(probe.gameObject);
        }

        [Test]
        public void CtorPrewarmFillsIdleWithoutCallbacks()
        {
            var pool = new GameObjectPool(_prefab, parent: null, prewarm: 3);
            Assert.AreEqual(3, pool.CountIdle);

            foreach (var probe in Object.FindObjectsByType<PoolProbe>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (probe.gameObject == _prefab) continue;
                Assert.IsFalse(probe.gameObject.activeSelf);
                Assert.AreEqual(0, probe.Spawned);
                Assert.AreEqual(0, probe.Despawned); // ctor prewarm fires no lifecycle callbacks
            }
        }

        [Test]
        public void SpawnPrefersIdleThenInstantiates()
        {
            var pool = new GameObjectPool(_prefab, parent: null, prewarm: 1);
            var fromIdle = pool.Spawn(new Vector3(1, 0, 0), Quaternion.identity);
            Assert.AreEqual(0, pool.CountIdle);
            Assert.AreEqual(new Vector3(1, 0, 0), fromIdle.transform.position);

            var fresh = pool.Spawn(Vector3.zero, Quaternion.identity);
            Assert.AreNotSame(fromIdle, fresh);
        }

        [UnityTest]
        public IEnumerator MaxIdleOverflowDestroysInsteadOfPooling()
        {
            var pool = new GameObjectPool(_prefab, parent: null, prewarm: 0, maxIdle: 1);
            var a = pool.Spawn(Vector3.zero, Quaternion.identity);
            var b = pool.Spawn(Vector3.zero, Quaternion.identity);

            pool.Despawn(a);
            pool.Despawn(b); // over maxIdle -> Object.Destroy (deferred)
            Assert.AreEqual(1, pool.CountIdle);

            yield return null;
            Assert.IsTrue(a != null);
            Assert.IsTrue(b == null); // Unity fake-null after Destroy
        }
    }
}
