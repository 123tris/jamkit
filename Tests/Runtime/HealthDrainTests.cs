using NUnit.Framework;
using Ripple;
using UnityEngine;

namespace Metz.JamKit.Tests
{
    public class HealthDrainTests
    {
        GameObject host;

        (Health health, HealthDrain drain) NewDrained(float max, float perSecond)
        {
            host = new GameObject("drain");
            host.SetActive(false);
            var health = host.AddComponent<Health>();
            health.Max = new FloatReference(max);
            health.Current = max;
            var drain = host.AddComponent<HealthDrain>();
            drain.Health = health;
            drain.DrainPerSecond = new FloatReference(perSecond);
            drain.Resume();
            return (health, drain);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(host);

        [Test]
        public void Tick_DrainsAtRate()
        {
            (Health health, HealthDrain drain) = NewDrained(10f, 2f);

            drain.Tick(1.5f);

            Assert.AreEqual(7f, health.Current, 0.0001f);
        }

        [Test]
        public void Tick_IsSilent_ButDiesAtZero()
        {
            (Health health, HealthDrain drain) = NewDrained(2f, 1f);
            int damaged = 0, died = 0;
            health.OnDamaged.AddListener(_ => damaged++);
            health.OnDied.AddListener(() => died++);

            drain.Tick(1f);
            Assert.AreEqual(0, damaged);
            Assert.AreEqual(0, died);

            drain.Tick(5f);
            Assert.AreEqual(0, damaged);
            Assert.AreEqual(1, died);

            drain.Tick(1f);
            Assert.AreEqual(1, died);
        }

        [Test]
        public void Pause_StopsDrain_ResumeRestarts()
        {
            (Health health, HealthDrain drain) = NewDrained(10f, 1f);

            drain.Pause();
            drain.Tick(3f);
            Assert.AreEqual(10f, health.Current, 0.0001f);

            drain.Resume();
            drain.Tick(3f);
            Assert.AreEqual(7f, health.Current, 0.0001f);
        }

        [Test]
        public void SecondsRemaining_FollowsHealthAndRate()
        {
            (Health health, HealthDrain drain) = NewDrained(10f, 2f);

            Assert.AreEqual(5f, drain.SecondsRemaining, 0.0001f);
            health.Heal(0f);
            drain.Tick(1f);
            Assert.AreEqual(4f, drain.SecondsRemaining, 0.0001f);
        }
    }
}
