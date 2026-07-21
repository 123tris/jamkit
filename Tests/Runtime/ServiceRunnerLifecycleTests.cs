using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Metz.JamKit.Tests
{
    internal sealed class TestLifecycleServiceSO : ServiceSO<TestLifecycleRunner>
    {
        public int ResetCount;
        public override void ResetState() => ResetCount++;
    }

    internal sealed class TestLifecycleRunner : ServiceRunner<TestLifecycleServiceSO, TestLifecycleRunner>
    {
        public bool Registered => IsRegistered;
    }

    /// <summary>
    /// Pins the ServiceSO&lt;TRunner&gt;/ServiceRunner registration contract every service builds on.
    /// </summary>
    public class ServiceRunnerLifecycleTests
    {
        TestLifecycleServiceSO _service;

        [SetUp]
        public void SetUp() => _service = ScriptableObject.CreateInstance<TestLifecycleServiceSO>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_service);

        static TestLifecycleRunner MakeRunner(TestLifecycleServiceSO service, bool activate = true, string name = "runner")
        {
            var go = new GameObject(name);
            go.SetActive(false);
            var runner = go.AddComponent<TestLifecycleRunner>();
            runner.Service = service;
            if (activate) go.SetActive(true);
            return runner;
        }

        [Test]
        public void EnableRegistersAndCallsResetStateOnce()
        {
            var runner = MakeRunner(_service);
            Assert.IsTrue(_service.HasRunner);
            Assert.IsTrue(runner.Registered);
            Assert.AreEqual(1, _service.ResetCount); // default OnRunnerRegistered -> ResetState

            Object.DestroyImmediate(runner.gameObject);
        }

        [Test]
        public void DisableUnregisters()
        {
            var runner = MakeRunner(_service);
            runner.gameObject.SetActive(false);
            Assert.IsFalse(_service.HasRunner);
            Assert.IsFalse(runner.Registered);

            Object.DestroyImmediate(runner.gameObject);
        }

        [Test]
        public void UnregisteringAForeignRunnerIsIgnored()
        {
            var registered = MakeRunner(_service, activate: false);
            var foreign = MakeRunner(_service, activate: false, name: "foreign");

            _service.RegisterRunner(registered);
            Assert.IsTrue(_service.HasRunner);

            _service.UnregisterRunner(foreign); // ReferenceEquals guard: not the registered one
            Assert.IsTrue(_service.HasRunner);

            Object.DestroyImmediate(registered.gameObject);
            Object.DestroyImmediate(foreign.gameObject);
        }

        [Test]
        public void SecondRunnerReplacesFirst_AndStaleDisableDoesNotClearIt()
        {
            var first = MakeRunner(_service, name: "first");
            var second = MakeRunner(_service, name: "second");
            Assert.AreEqual(2, _service.ResetCount); // each registration resets by default

            first.gameObject.SetActive(false); // stale runner disables after being replaced
            Assert.IsTrue(_service.HasRunner); // second is still the registered runner

            Object.DestroyImmediate(first.gameObject);
            Object.DestroyImmediate(second.gameObject);
        }

        [Test]
        public void NullServiceWarnsAndStaysUnregistered()
        {
            LogAssert.Expect(LogType.Warning, new Regex(@"\[JamKit\].*has no Service assigned"));

            var go = new GameObject("orphan");
            var runner = go.AddComponent<TestLifecycleRunner>(); // OnEnable runs with Service == null
            Assert.IsFalse(runner.Registered);

            Object.DestroyImmediate(go);
        }
    }
}
