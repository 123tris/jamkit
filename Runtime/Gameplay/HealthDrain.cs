using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Turns a <see cref="Health"/> into a life-timer: current HP drains by
    /// <see cref="DrainPerSecond"/> every second, and hitting zero is death. Drains through
    /// <see cref="Health.SetCurrent"/> — silent per tick (no OnDamaged feedback spam every frame),
    /// but <see cref="Health.OnDied"/> and the CurrentVariable HUD mirror still fire, so "health
    /// is a timer" costs zero extra wiring: bind the bar, heal to add seconds, damage to steal them.
    /// Scaled time, so pause/hit-stop/slow-mo stop the bleeding too.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HealthDrain : MonoBehaviour
    {
        [Required, Tooltip("The Health that bleeds — normally the sibling on this object.")]
        public Health Health;

        [Tooltip("HP (seconds of life) removed per second. Constant or a shared Ripple variable (difficulty ramps the bleed).")]
        public FloatReference DrainPerSecond = new(1f);

        [Tooltip("Start draining on enable. Off = wait for a Resume() call (grace periods, cutscenes).")]
        public bool AutoStart = true;

        bool _running;

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")] public bool IsDraining => _running;
        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")]
        public float SecondsRemaining =>
            Health == null || DrainPerSecond.Value <= 0f ? float.PositiveInfinity : Health.Current / DrainPerSecond.Value;

        void OnEnable() => _running = AutoStart;

        void Update() => Tick(Time.deltaTime);

        [Button, DisableInEditorMode, FoldoutGroup("Debug"), HorizontalGroup("Debug/Row")]
        public void Pause() => _running = false;

        [Button, DisableInEditorMode, HorizontalGroup("Debug/Row")]
        public void Resume() => _running = true;

        /// <summary>Advance the drain. Internal so tests can drive it without frames.</summary>
        internal void Tick(float dt)
        {
            if (!_running || dt <= 0f) return;
            if (Health == null || Health.IsDead) return;
            float rate = DrainPerSecond.Value;
            if (rate <= 0f) return;
            Health.SetCurrent(Health.Current - rate * dt);
        }
    }
}
