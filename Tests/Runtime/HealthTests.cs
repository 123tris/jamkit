using NUnit.Framework;
using Ripple;
using UnityEngine;

namespace Metz.JamKit.Tests
{
    public class HealthTests
    {
        GameObject _go;
        FloatVariableSO _current;

        // SetActive(false) skips OnEnable so tests drive the API directly (matches GameTimerTests).
        Health NewHealth(float max)
        {
            _go = new GameObject("health");
            _go.SetActive(false);
            var h = _go.AddComponent<Health>();
            h.Max = new FloatReference(max);
            h.Current = max;
            return h;
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            if (_current != null) Object.DestroyImmediate(_current);
        }

        [Test]
        public void Damage_ReducesCurrent_AndDiesOnceAtZero()
        {
            var h = NewHealth(10f);
            int died = 0;
            h.OnDied = new UltEvents.UltEvent();
            h.OnDied.DynamicCalls += () => died++;

            h.Damage(4f);
            Assert.AreEqual(6f, h.Current, 0.0001f);
            Assert.IsFalse(h.IsDead);
            Assert.AreEqual(0, died);

            h.Damage(6f);
            Assert.AreEqual(0f, h.Current, 0.0001f);
            Assert.IsTrue(h.IsDead);
            Assert.AreEqual(1, died);

            h.Damage(5f); // already dead: no-op, no second OnDied
            Assert.AreEqual(1, died);
        }

        [Test]
        public void Heal_ClampsToMax()
        {
            var h = NewHealth(10f);
            h.Damage(8f);      // Current = 2
            h.Heal(100f);
            Assert.AreEqual(10f, h.Current, 0.0001f);
        }

        [Test]
        public void SetCurrent_ClampsToRange_AndKillsAtZero()
        {
            var h = NewHealth(10f);
            int died = 0;
            h.OnDied = new UltEvents.UltEvent();
            h.OnDied.DynamicCalls += () => died++;

            h.SetCurrent(999f);
            Assert.AreEqual(10f, h.Current, 0.0001f); // clamped to Max
            Assert.AreEqual(0, died);

            h.SetCurrent(0f);
            Assert.IsTrue(h.IsDead);
            Assert.AreEqual(1, died);
        }

        [Test]
        public void Damage_MirrorsCurrentToVariable()
        {
            _current = ScriptableObject.CreateInstance<FloatVariableSO>();
            var h = NewHealth(10f);
            h.CurrentVariable = _current;

            h.Damage(3f);
            Assert.AreEqual(7f, _current.CurrentValue, 0.0001f);
        }

        [Test]
        public void ExternalVariableWrite_DrivesHealth_ClampsAndKills()
        {
            _current = ScriptableObject.CreateInstance<FloatVariableSO>();
            var h = NewHealth(10f);
            h.CurrentVariable = _current;
            int died = 0;
            h.OnDied = new UltEvents.UltEvent();
            h.OnDied.DynamicCalls += () => died++;

            // An external write arriving through the two-way link.
            h.OnCurrentVariableChanged(3f);
            Assert.AreEqual(3f, h.Current, 0.0001f);
            Assert.AreEqual(0, died);

            // Out of range → clamped to Max and reflected back into the variable.
            h.OnCurrentVariableChanged(999f);
            Assert.AreEqual(10f, h.Current, 0.0001f);
            Assert.AreEqual(10f, _current.CurrentValue, 0.0001f);

            h.OnCurrentVariableChanged(0f);
            Assert.IsTrue(h.IsDead);
            Assert.AreEqual(1, died);
        }

        [Test]
        public void MaxValue_ResolvesReference_AndResetFull_Refills()
        {
            var h = NewHealth(7f);
            Assert.AreEqual(7f, h.MaxValue, 0.0001f);
            h.Damage(5f);
            h.ResetFull();
            Assert.AreEqual(7f, h.Current, 0.0001f);
        }
    }
}
