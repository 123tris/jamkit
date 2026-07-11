using NUnit.Framework;
using UnityEngine;

namespace Metz.JamKit.Tests
{
    public class ScoreServiceTests
    {
        ScoreServiceSO NewService() => ScriptableObject.CreateInstance<ScoreServiceSO>();

        [Test]
        public void Add_Accumulates()
        {
            var s = NewService();
            s.Add(5);
            s.Add(7);
            Assert.AreEqual(12, s.Score);
        }

        [Test]
        public void Set_ClampsBelowZero()
        {
            var s = NewService();
            s.Set(-50);
            Assert.AreEqual(0, s.Score);
        }

        [Test]
        public void HighScore_TracksBestWithoutSaveService()
        {
            var s = NewService();
            s.Add(30);
            s.Set(10);
            Assert.AreEqual(10, s.Score);
            Assert.AreEqual(30, s.HighScore);
        }

        [Test]
        public void ResetScore_KeepsHighScore()
        {
            var s = NewService();
            s.Add(42);
            s.ResetScore();
            Assert.AreEqual(0, s.Score);
            Assert.AreEqual(42, s.HighScore);
        }
    }
}
