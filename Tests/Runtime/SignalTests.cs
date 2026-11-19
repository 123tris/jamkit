using NUnit.Framework;
using Ripple;
using UnityEngine;

namespace Metz.JamKit.Tests
{
    public class SignalTests
    {
        [Test]
        public void Invoke_CallsListeners()
        {
            var signal = new Signal();
            int calls = 0;
            signal.AddListener(() => calls++);

            signal.Invoke();
            signal.Invoke();

            Assert.AreEqual(2, calls);
        }

        [Test]
        public void Operators_SubscribeAndUnsubscribe()
        {
            var signal = new Signal<int>();
            int received = 0;
            void Listener(int value) => received += value;

            signal += Listener;
            signal.Invoke(3);
            signal -= Listener;
            signal.Invoke(5);

            Assert.AreEqual(3, received);
            Assert.IsFalse(signal.HasListeners);
        }

        [Test]
        public void ClearListeners_DropsCodeSubscribers()
        {
            var signal = new Signal();
            int calls = 0;
            signal.AddListener(() => calls++);

            signal.ClearListeners();
            signal.Invoke();

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void GameEvent_RaiseReachesListeners()
        {
            var gameEvent = ScriptableObject.CreateInstance<FloatEvent>();
            float received = 0f;
            gameEvent.AddListener(value => received = value);

            gameEvent.Raise(4f);

            Assert.AreEqual(4f, received);
            Object.DestroyImmediate(gameEvent);
        }

        [Test]
        public void Variable_SetterNotifiesListeners()
        {
            var variable = ScriptableObject.CreateInstance<FloatVariableSO>();
            float received = 0f;
            variable.OnValueChanged += value => received = value;

            variable.CurrentValue = 7f;

            Assert.AreEqual(7f, received);
            Assert.AreEqual(7f, variable.CurrentValue);
            Object.DestroyImmediate(variable);
        }
    }
}
