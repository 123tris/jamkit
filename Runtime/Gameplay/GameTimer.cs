using Ripple;
using Sirenix.OdinInspector;
using UltEvents;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// A countdown or count-up clock for jam loops (survival time, round timer, speedrun).
    /// A round timer is scene state, not app state — it lives on a scene object and dies with
    /// the scene, per "scenes as clean slates" (PILLARS.md). Bind <see cref="TimeVariable"/> to
    /// a HUD LabelBinding; <see cref="OnComplete"/> (global) and <see cref="Completed"/>
    /// (this instance) fire at the boundary.
    /// </summary>
    public sealed class GameTimer : MonoBehaviour
    {
        public enum Mode { CountDown, CountUp }

        [BoxGroup("Config")] public Mode CountMode = Mode.CountDown;
        [BoxGroup("Config"), Tooltip("Countdown start, or count-up cap (0 = no cap). Constant or a shared Ripple variable.")]
        public FloatReference Duration = new(60f);
        [BoxGroup("Config"), Tooltip("Use unscaled time so the clock ignores pause/slow-mo.")]
        public bool Unscaled;
        [BoxGroup("Config"), Tooltip("Start ticking on enable. Re-enabling restarts the clock.")]
        public bool AutoStart = true;

        [FoldoutGroup("Broadcast (Ripple, global)")]
        [Tooltip("Optional — mirrors current time in seconds, for HUD binding.")]
        public FloatVariableSO TimeVariable;
        [FoldoutGroup("Broadcast (Ripple, global)")]
        public VoidEventSO OnComplete;

        [FoldoutGroup("Events (this instance)")]
        [Tooltip("Per-instance completion — wire scene reactions here (load GameOver, open a gate).")]
        public UltEvent Completed;

        float _time;
        bool _running;

        public float CurrentTime => _time;
        public bool IsRunning => _running;

        [ShowInInspector, ReadOnly, ProgressBar(0f, 1f), FoldoutGroup("Debug")]
        public float Progress01 => Duration.Value <= 0f
            ? 0f
            : Mathf.Clamp01(CountMode == Mode.CountDown ? 1f - _time / Duration.Value : _time / Duration.Value);

        void OnEnable()
        {
            ResetTimer();
            _running = AutoStart;
        }

        void Update() => Tick(Unscaled ? UnityEngine.Time.unscaledDeltaTime : UnityEngine.Time.deltaTime);

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void StartTimer()
        {
            ResetTimer();
            _running = true;
        }

        /// <summary>Start with a one-off duration (becomes the new constant).</summary>
        public void StartTimer(float duration)
        {
            Duration = new FloatReference(Mathf.Max(0f, duration));
            StartTimer();
        }

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void Pause() => _running = false;

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void Resume() => _running = true;

        public void Stop() => _running = false;

        /// <summary>Snap back to the starting value without starting the clock.</summary>
        public void ResetTimer()
        {
            _time = CountMode == Mode.CountDown ? Duration.Value : 0f;
            _running = false;
            Publish();
        }

        /// <summary>Advance the clock. Internal so tests can drive it without frames.</summary>
        internal void Tick(float dt)
        {
            if (!_running) return;

            if (CountMode == Mode.CountDown)
            {
                _time -= dt;
                if (_time <= 0f) { Complete(0f); return; }
            }
            else
            {
                _time += dt;
                if (Duration.Value > 0f && _time >= Duration.Value) { Complete(Duration.Value); return; }
            }
            Publish();
        }

        void Complete(float endValue)
        {
            _time = endValue;
            _running = false;
            Publish();
            if (OnComplete != null) OnComplete.Invoke();
            Completed?.Invoke();
        }

        void Publish() { if (TimeVariable != null) TimeVariable.SetCurrentValue(_time); }
    }
}
