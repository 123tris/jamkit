using NUnit.Framework;
using UnityEngine;

namespace Metz.JamKit.Tests
{
    public class GameTimerTests
    {
        GameObject _go;

        GameTimer NewTimer(GameTimer.Mode mode, float duration)
        {
            _go = new GameObject("timer");
            _go.SetActive(false); // avoid OnEnable/Update; tests drive Tick directly
            var t = _go.AddComponent<GameTimer>();
            t.CountMode = mode;
            t.Duration = new Ripple.FloatReference(duration);
            return t;
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void CountDown_ReachesZeroAndStops()
        {
            var t = NewTimer(GameTimer.Mode.CountDown, 1f);
            t.StartTimer();
            for (int i = 0; i < 12 && t.IsRunning; i++) t.Tick(0.1f);
            Assert.AreEqual(0f, t.CurrentTime, 0.0001f);
            Assert.IsFalse(t.IsRunning);
        }

        [Test]
        public void CountUp_StopsAtCap()
        {
            var t = NewTimer(GameTimer.Mode.CountUp, 0.5f);
            t.StartTimer();
            for (int i = 0; i < 10 && t.IsRunning; i++) t.Tick(0.1f);
            Assert.AreEqual(0.5f, t.CurrentTime, 0.0001f);
            Assert.IsFalse(t.IsRunning);
        }

        [Test]
        public void CountUp_NoCapRunsForever()
        {
            var t = NewTimer(GameTimer.Mode.CountUp, 0f);
            t.StartTimer();
            for (int i = 0; i < 100; i++) t.Tick(0.1f);
            Assert.IsTrue(t.IsRunning);
            Assert.AreEqual(10f, t.CurrentTime, 0.001f);
        }

        [Test]
        public void Pause_HoldsValue()
        {
            var t = NewTimer(GameTimer.Mode.CountDown, 5f);
            t.StartTimer();
            t.Tick(1f);
            t.Pause();
            t.Tick(1f);
            Assert.AreEqual(4f, t.CurrentTime, 0.0001f);
            t.Resume();
            t.Tick(1f);
            Assert.AreEqual(3f, t.CurrentTime, 0.0001f);
        }

        [Test]
        public void ResetTimer_RestoresStartValueWithoutRunning()
        {
            var t = NewTimer(GameTimer.Mode.CountDown, 8f);
            t.StartTimer();
            t.Tick(3f);
            t.ResetTimer();
            Assert.AreEqual(8f, t.CurrentTime, 0.0001f);
            Assert.IsFalse(t.IsRunning);
        }

        [Test]
        public void Completed_FiresOncePerRun()
        {
            var t = NewTimer(GameTimer.Mode.CountDown, 0.2f);
            int fired = 0;
            t.Completed = new UltEvents.UltEvent();
            t.Completed.DynamicCalls += () => fired++;
            t.StartTimer();
            for (int i = 0; i < 5; i++) t.Tick(0.1f);
            Assert.AreEqual(1, fired);
        }
    }
}
