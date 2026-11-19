using NUnit.Framework;
using UnityEngine;

namespace Metz.JamKit.Tests
{
    public class TimeServiceSOTests
    {
        TimeServiceSO time;

        [SetUp]
        public void SetUp() => time = ScriptableObject.CreateInstance<TimeServiceSO>();

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(time);
            Time.timeScale = 1f;
        }

        [Test]
        public void Push_SetsTimeScale_ReleaseRestoresBase()
        {
            TimeModifier slow = time.Push(0.5f);
            Assert.AreEqual(0.5f, Time.timeScale, 0.0001f);

            slow.Release();
            Assert.AreEqual(1f, Time.timeScale, 0.0001f);
        }

        [Test]
        public void LowestModifierWins()
        {
            time.Push(0.5f);
            TimeModifier freeze = time.Push(0f);
            Assert.AreEqual(0f, Time.timeScale, 0.0001f);

            freeze.Release();
            Assert.AreEqual(0.5f, Time.timeScale, 0.0001f);
        }

        [Test]
        public void PauseSurvivesAnotherModifierEnding()
        {
            TimeModifier slow = time.Push(0.3f);
            time.Pause();

            slow.Release();
            Assert.AreEqual(0f, Time.timeScale, 0.0001f);

            time.Resume();
            Assert.AreEqual(1f, Time.timeScale, 0.0001f);
        }

        [Test]
        public void Pause_IsNotNested()
        {
            time.Pause();
            time.Pause();

            time.Resume();

            Assert.IsFalse(time.IsPaused);
            Assert.AreEqual(1f, Time.timeScale, 0.0001f);
        }

        [Test]
        public void NegativeScaleClampsToZero()
        {
            time.Push(-2f);

            Assert.AreEqual(0f, Time.timeScale, 0.0001f);
        }

        [Test]
        public void BaseScaleMultipliesModifiers()
        {
            time.BaseScale = 2f;
            Assert.AreEqual(2f, Time.timeScale, 0.0001f);

            time.Push(0.5f);
            Assert.AreEqual(1f, Time.timeScale, 0.0001f);
        }

        [Test]
        public void ModifierScale_CanChangeWhileActive()
        {
            TimeModifier modifier = time.Push(1f);

            modifier.Scale = 0.25f;

            Assert.AreEqual(0.25f, Time.timeScale, 0.0001f);
        }

        [Test]
        public void ResetState_DropsModifiersAndPause()
        {
            time.Push(0.2f);
            time.Pause();

            time.ResetState();

            Assert.AreEqual(0, time.ActiveModifiers);
            Assert.IsFalse(time.IsPaused);
            Assert.AreEqual(1f, Time.timeScale, 0.0001f);
        }
    }
}
