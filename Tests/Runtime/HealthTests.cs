using NUnit.Framework;
using Ripple;
using UnityEngine;

namespace Metz.JamKit.Tests
{
    public class HealthTests
    {
        GameObject host;
        FloatVariableSO mirror;

        Health NewHealth(float max)
        {
            host = new GameObject("health");
            host.SetActive(false);
            var health = host.AddComponent<Health>();
            health.Max = new FloatReference(max);
            health.Current = max;
            return health;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
            if (mirror != null) Object.DestroyImmediate(mirror);
        }

        [Test]
        public void Damage_ReducesCurrent_AndDiesOnceAtZero()
        {
            Health health = NewHealth(10f);
            int died = 0;
            health.OnDied.AddListener(() => died++);

            health.Damage(4f);
            Assert.AreEqual(6f, health.Current, 0.0001f);
            Assert.IsFalse(health.IsDead);
            Assert.AreEqual(0, died);

            health.Damage(6f);
            Assert.IsTrue(health.IsDead);
            Assert.AreEqual(1, died);

            health.Damage(5f);
            Assert.AreEqual(1, died);
        }

        [Test]
        public void Damage_ReportsAmount()
        {
            Health health = NewHealth(10f);
            float reported = 0f;
            health.OnDamaged.AddListener(amount => reported = amount);

            health.Damage(3f);

            Assert.AreEqual(3f, reported, 0.0001f);
        }

        [Test]
        public void Heal_ClampsToMax()
        {
            Health health = NewHealth(10f);
            health.Damage(8f);

            health.Heal(100f);

            Assert.AreEqual(10f, health.Current, 0.0001f);
        }

        [Test]
        public void SetCurrent_ClampsToRange_AndKillsAtZero()
        {
            Health health = NewHealth(10f);
            int died = 0;
            health.OnDied.AddListener(() => died++);

            health.SetCurrent(50f);
            Assert.AreEqual(10f, health.Current, 0.0001f);

            health.SetCurrent(-5f);
            Assert.AreEqual(0f, health.Current, 0.0001f);
            Assert.AreEqual(1, died);
        }

        [Test]
        public void Damage_MirrorsCurrentToVariable()
        {
            Health health = NewHealth(10f);
            mirror = ScriptableObject.CreateInstance<FloatVariableSO>();
            health.CurrentVariable = mirror;

            health.Damage(3f);

            Assert.AreEqual(7f, mirror.CurrentValue, 0.0001f);
        }

        [Test]
        public void ResetFull_RefillsToTheResolvedMax()
        {
            Health health = NewHealth(10f);
            health.Max = new FloatReference(25f);

            health.ResetFull();

            Assert.AreEqual(25f, health.Current, 0.0001f);
            Assert.AreEqual(1f, health.Ratio01, 0.0001f);
        }
    }
}
