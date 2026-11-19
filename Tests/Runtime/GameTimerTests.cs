using NUnit.Framework;
using Ripple;
using UnityEngine;

namespace Metz.JamKit.Tests
{
    public class GameTimerTests
    {
        GameObject host;

        GameTimer NewTimer(GameTimer.Mode mode, float duration)
        {
            host = new GameObject("timer");
            host.SetActive(false);
            var timer = host.AddComponent<GameTimer>();
            timer.CountMode = mode;
            timer.Duration = new FloatReference(duration);
            timer.StartTimer();
            return timer;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(host);

        [Test]
        public void CountDown_ReachesZeroAndStops()
        {
            GameTimer timer = NewTimer(GameTimer.Mode.CountDown, 3f);

            timer.Tick(2f);
            Assert.AreEqual(1f, timer.CurrentTime, 0.0001f);
            Assert.IsTrue(timer.IsRunning);

            timer.Tick(2f);
            Assert.AreEqual(0f, timer.CurrentTime, 0.0001f);
            Assert.IsFalse(timer.IsRunning);
        }

        [Test]
        public void CountUp_StopsAtCap()
        {
            GameTimer timer = NewTimer(GameTimer.Mode.CountUp, 3f);

            timer.Tick(5f);

            Assert.AreEqual(3f, timer.CurrentTime, 0.0001f);
            Assert.IsFalse(timer.IsRunning);
        }

        [Test]
        public void CountUp_NoCapRunsForever()
        {
            GameTimer timer = NewTimer(GameTimer.Mode.CountUp, 0f);

            timer.Tick(500f);

            Assert.AreEqual(500f, timer.CurrentTime, 0.0001f);
            Assert.IsTrue(timer.IsRunning);
        }

        [Test]
        public void Pause_HoldsValue()
        {
            GameTimer timer = NewTimer(GameTimer.Mode.CountDown, 10f);
            timer.Tick(4f);

            timer.Pause();
            timer.Tick(4f);
            Assert.AreEqual(6f, timer.CurrentTime, 0.0001f);

            timer.Resume();
            timer.Tick(1f);
            Assert.AreEqual(5f, timer.CurrentTime, 0.0001f);
        }

        [Test]
        public void ResetTimer_RestoresStartValueWithoutRunning()
        {
            GameTimer timer = NewTimer(GameTimer.Mode.CountDown, 10f);
            timer.Tick(4f);

            timer.ResetTimer();

            Assert.AreEqual(10f, timer.CurrentTime, 0.0001f);
            Assert.IsFalse(timer.IsRunning);
        }

        [Test]
        public void OnCompleted_FiresOncePerRun()
        {
            GameTimer timer = NewTimer(GameTimer.Mode.CountDown, 1f);
            int completed = 0;
            timer.OnCompleted.AddListener(() => completed++);

            timer.Tick(2f);
            timer.Tick(2f);

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void TimeVariable_MirrorsCurrentTime()
        {
            GameTimer timer = NewTimer(GameTimer.Mode.CountDown, 10f);
            var mirror = ScriptableObject.CreateInstance<FloatVariableSO>();
            timer.TimeVariable = mirror;

            timer.Tick(4f);

            Assert.AreEqual(6f, mirror.CurrentValue, 0.0001f);
            Object.DestroyImmediate(mirror);
        }
    }
}
