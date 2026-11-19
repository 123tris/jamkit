using Metz.JamKit.Inspector;
using Ripple;
using UnityEngine;

namespace Metz.JamKit
{
    [DisallowMultipleComponent]
    public sealed class HealthDrain : MonoBehaviour
    {
        [Required] public Health Health;
        public FloatReference DrainPerSecond = new(1f);
        public bool AutoStart = true;

        [ShowInInspector]
        public bool IsDraining { get; private set; }

        [ShowInInspector]
        public float SecondsRemaining
            => DrainPerSecond.Value <= 0f ? float.PositiveInfinity : Health.Current / DrainPerSecond.Value;

        void OnEnable() => IsDraining = AutoStart;

        void Update() => Tick(Time.deltaTime);

        [Button]
        public void Pause() => IsDraining = false;

        [Button]
        public void Resume() => IsDraining = true;

        internal void Tick(float deltaTime)
        {
            if (!IsDraining || Health.IsDead) return;
            Health.SetCurrent(Health.Current - DrainPerSecond.Value * deltaTime);
        }
    }
}
