using NUnit.Framework;
using UnityEngine;

namespace Metz.JamKit.Tests
{
    /// <summary>
    /// Characterization tests for the timescale stack — TimeServiceSO is the single owner of
    /// Time.timeScale, so every test restores it in teardown.
    /// </summary>
    public class TimeServiceSOTests
    {
        TimeServiceSO _time;

        [SetUp]
        public void SetUp()
        {
            _time = ScriptableObject.CreateInstance<TimeServiceSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_time);
            Time.timeScale = 1f;
        }

        [Test]
        public void PushSetsTimeScale_PopRestoresBase()
        {
            _time.Push(0.5f);
            Assert.AreEqual(0.5f, Time.timeScale, 0.0001f);
            _time.Pop();
            Assert.AreEqual(1f, Time.timeScale, 0.0001f);
        }

        [Test]
        public void TopOfStackWins()
        {
            _time.Push(0.5f);
            _time.Push(0f);
            Assert.AreEqual(0f, Time.timeScale, 0.0001f);
            _time.Pop();
            Assert.AreEqual(0.5f, Time.timeScale, 0.0001f);
        }

        [Test]
        public void NegativePushClampsToZero()
        {
            _time.Push(-2f);
            Assert.AreEqual(0f, Time.timeScale, 0.0001f);
        }

        [Test]
        public void BaseScaleAppliesOnlyWhenStackEmpty()
        {
            _time.BaseScale = 0.25f;
            Assert.AreEqual(0.25f, Time.timeScale, 0.0001f);
            _time.Push(1f);
            Assert.AreEqual(1f, Time.timeScale, 0.0001f);
            _time.Pop();
            Assert.AreEqual(0.25f, Time.timeScale, 0.0001f);
        }

        [Test]
        public void PopOnEmptyStackIsSafe()
        {
            _time.Pop();
            Assert.AreEqual(0, _time.StackDepth);
            Assert.AreEqual(1f, Time.timeScale, 0.0001f);
        }

        [Test]
        public void ClearDropsAllModifiers()
        {
            _time.Push(0f);
            _time.Push(0.5f);
            _time.Clear();
            Assert.AreEqual(0, _time.StackDepth);
            Assert.AreEqual(1f, Time.timeScale, 0.0001f);
        }

        [Test]
        public void PauseAndResumeComposeViaStack()
        {
            _time.Pause();
            Assert.AreEqual(0f, Time.timeScale, 0.0001f);
            Assert.AreEqual(1, _time.StackDepth);
            _time.Resume();
            Assert.AreEqual(1f, Time.timeScale, 0.0001f);
            Assert.AreEqual(0, _time.StackDepth);
        }

        [Test]
        public void FreezeForSecondsWithoutRunnerReturnsNull()
        {
            Assert.IsNull(_time.FreezeForSeconds(0.5f));
        }

        [Test]
        public void RegisterRunnerResetsStack()
        {
            _time.Push(0f);
            var go = new GameObject("core");
            go.SetActive(false);
            var runner = go.AddComponent<TimeServiceRunner>();
            runner.Service = _time;
            go.SetActive(true); // OnEnable -> RegisterRunner -> OnRunnerRegistered -> ResetState

            Assert.AreEqual(0, _time.StackDepth);
            Assert.AreEqual(1f, Time.timeScale, 0.0001f);
            Object.DestroyImmediate(go);
        }
    }
}
