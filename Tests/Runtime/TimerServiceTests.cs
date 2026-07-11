using NUnit.Framework;
using UnityEngine;

namespace Metz.JamKit.Tests
{
    public class TimerServiceTests
    {
        TimerServiceSO NewService(TimerServiceSO.Mode mode, float duration)
        {
            var t = ScriptableObject.CreateInstance<TimerServiceSO>();
            t.CountMode = mode;
            t.Duration = duration;
            return t;
        }

        [Test]
        public void CountDown_ReachesZeroAndStops()
        {
            var t = NewService(TimerServiceSO.Mode.CountDown, 1f);
            t.StartTimer();
            for (int i = 0; i < 12 && t.IsRunning; i++) t.Tick(0.1f);
            Assert.AreEqual(0f, t.Time, 0.0001f);
            Assert.IsFalse(t.IsRunning);
        }

        [Test]
        public void CountUp_StopsAtCap()
        {
            var t = NewService(TimerServiceSO.Mode.CountUp, 0.5f);
            t.StartTimer();
            for (int i = 0; i < 10 && t.IsRunning; i++) t.Tick(0.1f);
            Assert.AreEqual(0.5f, t.Time, 0.0001f);
            Assert.IsFalse(t.IsRunning);
        }

        [Test]
        public void CountUp_NoCapRunsForever()
        {
            var t = NewService(TimerServiceSO.Mode.CountUp, 0f);
            t.StartTimer();
            for (int i = 0; i < 100; i++) t.Tick(0.1f);
            Assert.IsTrue(t.IsRunning);
            Assert.AreEqual(10f, t.Time, 0.001f);
        }

        [Test]
        public void Pause_HoldsValue()
        {
            var t = NewService(TimerServiceSO.Mode.CountDown, 5f);
            t.StartTimer();
            t.Tick(1f);
            t.Pause();
            t.Tick(1f);
            Assert.AreEqual(4f, t.Time, 0.0001f);
            t.Resume();
            t.Tick(1f);
            Assert.AreEqual(3f, t.Time, 0.0001f);
        }

        [Test]
        public void ResetState_RestoresStartValue()
        {
            var t = NewService(TimerServiceSO.Mode.CountDown, 8f);
            t.StartTimer();
            t.Tick(3f);
            t.ResetState();
            Assert.AreEqual(8f, t.Time, 0.0001f);
            Assert.IsFalse(t.IsRunning);
        }
    }
}
