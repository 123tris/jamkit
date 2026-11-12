using System;
using NUnit.Framework;
using UnityEngine;

namespace Metz.JamKit.Tests
{
    public class SceneRefTests
    {
        [Serializable]
        class Holder { public SceneRef Scene; }

        [Test]
        public void NamedRefExposesItsName()
        {
            var r = new SceneRef("Level1");
            Assert.AreEqual("Level1", r.Name);
            Assert.IsTrue(r.HasValue);
            Assert.AreEqual("Level1", (string)r);
            Assert.AreEqual("Level1", r.ToString());
        }

        [Test]
        public void DefaultRefHasNoValue()
        {
            SceneRef r = default;
            Assert.IsFalse(r.HasValue);
            Assert.IsNull((string)r);
        }

        [Test]
        public void EmptyNameHasNoValue()
        {
            Assert.IsFalse(new SceneRef("").HasValue);
        }

        [Test]
        public void SurvivesJsonRoundTrip()
        {
            var json = JsonUtility.ToJson(new Holder { Scene = new SceneRef("Boss") });
            var back = JsonUtility.FromJson<Holder>(json);
            Assert.AreEqual("Boss", back.Scene.Name);
            Assert.IsTrue(back.Scene.HasValue);
        }
    }
}
