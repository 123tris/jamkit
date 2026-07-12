using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// One-shot countdown driven by scaled or unscaled time. Tick from any Update loop.
    /// </summary>
    public struct Timer
    {
        public float Duration;
        public bool Unscaled;
        float _elapsed;
        bool _running;

        public Timer(float duration, bool unscaled = false)
        {
            Duration = duration;
            Unscaled = unscaled;
            _elapsed = 0f;
            _running = true;
        }

        public bool IsRunning => _running;
        public bool IsDone => !_running;
        public float Elapsed => _elapsed;
        public float Remaining => Mathf.Max(0f, Duration - _elapsed);
        public float Progress01 => Duration <= 0f ? 1f : Mathf.Clamp01(_elapsed / Duration);

        /// <summary>Advance the timer; returns true on the frame it finishes.</summary>
        public bool Tick()
        {
            if (!_running) return false;
            _elapsed += Unscaled ? UnityEngine.Time.unscaledDeltaTime : UnityEngine.Time.deltaTime;
            if (_elapsed >= Duration)
            {
                _running = false;
                return true;
            }
            return false;
        }

        public void Restart()
        {
            _elapsed = 0f;
            _running = true;
        }

        public void Stop() => _running = false;
    }
}
