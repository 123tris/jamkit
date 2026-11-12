using System.Collections.Generic;
using NUnit.Framework;
using Ripple;
using UnityEngine;

namespace Metz.JamKit.Tests
{
    public class GameFlowTests
    {
        GameObject host;
        GameFlow flow;
        SceneServiceSO scenes;
        VoidEventSO everywhere;
        VoidEventSO elsewhere;

        [SetUp]
        public void SetUp()
        {
            everywhere = ScriptableObject.CreateInstance<VoidEventSO>();
            elsewhere = ScriptableObject.CreateInstance<VoidEventSO>();
            scenes = ScriptableObject.CreateInstance<SceneServiceSO>();

            flow = ScriptableObject.CreateInstance<GameFlow>();
            flow.Transitions = new List<GameFlow.Transition>
            {
                new() { On = everywhere, To = new SceneRef("Game") },
                new() { From = new SceneRef("SomeOtherScene"), On = elsewhere, To = new SceneRef("Game") },
            };

            host = new GameObject("driver");
            host.SetActive(false);
            var driver = host.AddComponent<GameFlowDriver>();
            driver.Flow = flow;
            driver.SceneService = scenes;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(flow);
            Object.DestroyImmediate(scenes);
            Object.DestroyImmediate(everywhere);
            Object.DestroyImmediate(elsewhere);
        }

        [Test]
        public void ListensOnlyToTransitionsThatApplyInThisScene()
        {
            host.SetActive(true);

            Assert.IsTrue(everywhere.HasListeners);
            Assert.IsFalse(elsewhere.HasListeners);
        }

        [Test]
        public void StopsListeningWhenDisabled()
        {
            host.SetActive(true);

            host.SetActive(false);

            Assert.IsFalse(everywhere.HasListeners);
        }
    }
}
