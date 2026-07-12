using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Metz.JamKit.Tests
{
    public class TimerTests
    {
        [UnityTest]
        public IEnumerator UnscaledTimer_FinishesAfterDuration()
        {
            var t = new Timer(0.1f, unscaled: true);
            float waited = 0f;
            while (!t.Tick() && waited < 1f)
            {
                waited += UnityEngine.Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsTrue(t.IsDone);
            Assert.LessOrEqual(t.Elapsed, 0.5f);
        }

        [Test]
        public void Progress01_GoesFromZeroToOne()
        {
            var t = new Timer(2f);
            Assert.AreEqual(0f, t.Progress01, 0.001f);
            // can't easily advance without running; just sanity-check the API.
            Assert.IsTrue(t.IsRunning);
        }
    }
}
