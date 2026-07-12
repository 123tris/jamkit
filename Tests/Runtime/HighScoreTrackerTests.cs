using NUnit.Framework;
using Ripple;
using UnityEngine;

namespace Metz.JamKit.Tests
{
    public class HighScoreTrackerTests
    {
        GameObject _go;
        FloatVariableSO _score, _high;
        HighScoreTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _score = ScriptableObject.CreateInstance<FloatVariableSO>();
            _high = ScriptableObject.CreateInstance<FloatVariableSO>();
            _go = new GameObject("tracker");
            _go.SetActive(false); // avoid OnEnable subscription; tests drive OnScoreChanged directly
            _tracker = _go.AddComponent<HighScoreTracker>();
            _tracker.Score = _score;
            _tracker.HighScore = _high;
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_score);
            Object.DestroyImmediate(_high);
        }

        [Test]
        public void RaisesHighScoreWhenBeaten()
        {
            _tracker.OnScoreChanged(30f);
            Assert.AreEqual(30f, _high.CurrentValue, 0.0001f);
            _tracker.OnScoreChanged(10f);
            Assert.AreEqual(30f, _high.CurrentValue, 0.0001f);
            _tracker.OnScoreChanged(45f);
            Assert.AreEqual(45f, _high.CurrentValue, 0.0001f);
        }

        [Test]
        public void DoesNotLowerHighScore()
        {
            _high.SetCurrentValue(100f);
            _tracker.OnScoreChanged(50f);
            Assert.AreEqual(100f, _high.CurrentValue, 0.0001f);
        }
    }
}
