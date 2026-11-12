using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    [DisallowMultipleComponent]
    public sealed class HealthDrain : MonoBehaviour
    {
        [Required] public Health Health;
        public FloatReference DrainPerSecond = new(1f);
        public bool AutoStart = true;

        [ShowInInspector, ReadOnly, HideInEditorMode, FoldoutGroup("Debug")]
        public bool IsDraining { get; private set; }

        [ShowInInspector, ReadOnly, HideInEditorMode, FoldoutGroup("Debug")]
        public float SecondsRemaining
            => DrainPerSecond.Value <= 0f ? float.PositiveInfinity : Health.Current / DrainPerSecond.Value;

        void OnEnable() => IsDraining = AutoStart;

        void Update() => Tick(Time.deltaTime);

        [Button, DisableInEditorMode, FoldoutGroup("Debug"), HorizontalGroup("Debug/Row")]
        public void Pause() => IsDraining = false;

        [Button, DisableInEditorMode, HorizontalGroup("Debug/Row")]
        public void Resume() => IsDraining = true;

        internal void Tick(float deltaTime)
        {
            if (!IsDraining || Health.IsDead) return;
            Health.SetCurrent(Health.Current - DrainPerSecond.Value * deltaTime);
        }
    }
}
