using Ripple;
using UnityEngine;

namespace Metz.JamKit
{
    public sealed class LifecycleSignals : MonoBehaviour
    {
        public Signal OnEnabled = new();
        public Signal OnStarted = new();
        public Signal OnDisabled = new();

        void OnEnable() => OnEnabled.Invoke();

        void Start() => OnStarted.Invoke();

        void OnDisable() => OnDisabled.Invoke();
    }
}
