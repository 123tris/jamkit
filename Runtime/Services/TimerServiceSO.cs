using Ripple;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// A countdown or count-up clock for jam loops (survival time, round timer, speedrun).
    /// Requires a scene-side <see cref="TimerServiceRunner"/> to advance — without one the
    /// timer holds its value (handy for tests/menus).
    /// Bind <see cref="TimeVariable"/> to a HUD LabelBinding; <see cref="OnTimerComplete"/> fires at the boundary.
    /// </summary>
    [CreateAssetMenu(menuName = "JamKit/Services/Timer Service", fileName = "TimerService")]
    public sealed class TimerServiceSO : ScriptableObject
    {
        public enum Mode { CountDown, CountUp }

        [Header("Config")]
        public Mode CountMode = Mode.CountDown;
        [Min(0f), Tooltip("Countdown start, or count-up cap (0 = no cap).")]
        public float Duration = 60f;
        [Tooltip("Use unscaled time so the clock ignores pause/slow-mo.")]
        public bool Unscaled = false;

        [Header("Broadcast (Ripple)")]
        [Tooltip("Optional — current time in seconds, for HUD binding.")]
        public FloatVariableSO TimeVariable;
        public VoidEventSO OnTimerComplete;

        float _time;
        bool _running;

        public float Time => _time;
        public bool IsRunning => _running;

        // Reset fields when the asset (re)loads, but don't touch the shared Ripple variable at
        // edit/import time — the runtime runner calls ResetState() to publish.
        void OnEnable()
        {
            _running = false;
            _time = CountMode == Mode.CountDown ? Duration : 0f;
        }

        /// <summary>Stop the clock and snap to the starting value. Called by the runner each play session.</summary>
        public void ResetState()
        {
            _running = false;
            _time = CountMode == Mode.CountDown ? Duration : 0f;
            Publish();
        }

        public void StartTimer() => StartTimer(Duration);

        public void StartTimer(float duration)
        {
            Duration = Mathf.Max(0f, duration);
            _time = CountMode == Mode.CountDown ? Duration : 0f;
            _running = true;
            Publish();
        }

        public void Pause()  => _running = false;
        public void Resume() => _running = true;
        public void Stop()   => _running = false;

        /// <summary>Advance the clock. Called by <see cref="TimerServiceRunner"/>; pass scaled or unscaled dt per <see cref="Unscaled"/>.</summary>
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
                if (Duration > 0f && _time >= Duration) { Complete(Duration); return; }
            }
            Publish();
        }

        void Complete(float endValue)
        {
            _time = endValue;
            _running = false;
            Publish();
            if (OnTimerComplete != null) OnTimerComplete.Invoke();
        }

        void Publish() { if (TimeVariable != null) TimeVariable.SetCurrentValue(_time); }
    }
}
