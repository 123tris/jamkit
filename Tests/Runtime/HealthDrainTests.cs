using NUnit.Framework;
using Ripple;
using UnityEngine;

namespace Metz.JamKit.Tests
{
    public class HealthDrainTests
    {
        GameObject _go;

        // SetActive(false) skips OnEnable so tests drive the API directly (matches HealthTests).
        (Health health, HealthDrain drain) NewDrained(float max, float perSecond)
        {
            _go = new GameObject("drain");
            _go.SetActive(false);
            var h = _go.AddComponent<Health>();
            h.Max = new FloatReference(max);
            h.Current = max;
            var d = _go.AddComponent<HealthDrain>();
            d.Health = h;
            d.DrainPerSecond = new FloatReference(perSecond);
            d.Resume();
            return (h, d);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void Tick_DrainsAtRate()
        {
            var (h, d) = NewDrained(60f, 1f);
            d.Tick(2.5f);
            Assert.AreEqual(57.5f, h.Current, 0.0001f);
        }

        [Test]
        public void Tick_IsSilent_NoDamagedEvent_ButDiesAtZero()
        {
            var (h, d) = NewDrained(3f, 1f);
            int damaged = 0, died = 0;
            h.OnDamaged = new UltEvents.UltEvent<float>();
            h.OnDamaged.DynamicCalls += _ => damaged++;
            h.OnDied = new UltEvents.UltEvent();
            h.OnDied.DynamicCalls += () => died++;

            d.Tick(1f);
            d.Tick(1f);
            Assert.AreEqual(0, damaged, "drain must not spam OnDamaged");
            Assert.AreEqual(0, died);

            d.Tick(5f); // past zero — clamps and dies exactly once
            Assert.AreEqual(0f, h.Current, 0.0001f);
            Assert.IsTrue(h.IsDead);
            Assert.AreEqual(1, died);
            Assert.AreEqual(0, damaged);

            d.Tick(1f); // dead: no further writes, no double death
            Assert.AreEqual(1, died);
        }

        [Test]
        public void Pause_StopsDrain_ResumeRestarts()
        {
            var (h, d) = NewDrained(10f, 2f);
            d.Pause();
            d.Tick(3f);
            Assert.AreEqual(10f, h.Current, 0.0001f);
            d.Resume();
            d.Tick(3f);
            Assert.AreEqual(4f, h.Current, 0.0001f);
        }

        [Test]
        public void Heal_AddsSecondsBack_UpToMax()
        {
            var (h, d) = NewDrained(60f, 1f);
            d.Tick(30f);          // 30 left
            h.Heal(5f);           // pickup adds 5 seconds
            Assert.AreEqual(35f, h.Current, 0.0001f);
            h.Heal(100f);         // clamped to the cap
            Assert.AreEqual(60f, h.Current, 0.0001f);
        }

        [Test]
        public void ZeroOrNegativeRate_IsANoOp()
        {
            var (h, d) = NewDrained(10f, 0f);
            d.Tick(5f);
            Assert.AreEqual(10f, h.Current, 0.0001f);
        }
    }
}
