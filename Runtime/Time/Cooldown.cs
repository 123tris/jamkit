using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Cooldown timer: <see cref="TryUse"/> returns true at most once per <paramref name="duration"/> seconds.
    /// Driven by <see cref="UnityEngine.Time.time"/> (scaled). Use <see cref="UnscaledCooldown"/> if you need pause-immune timing.
    /// </summary>
    public struct Cooldown
    {
        public float Duration;
        float _nextReady;

        public Cooldown(float duration)
        {
            Duration = duration;
            _nextReady = 0f;
        }

        public bool Ready => UnityEngine.Time.time >= _nextReady;
        public float Remaining => Mathf.Max(0f, _nextReady - UnityEngine.Time.time);

        public bool TryUse()
        {
            if (!Ready) return false;
            _nextReady = UnityEngine.Time.time + Duration;
            return true;
        }

        public void Reset() => _nextReady = UnityEngine.Time.time + Duration;
        public void Clear() => _nextReady = 0f;
    }

    /// <summary>Unscaled-time variant of <see cref="Cooldown"/>; immune to pause/slow-mo.</summary>
    public struct UnscaledCooldown
    {
        public float Duration;
        float _nextReady;

        public UnscaledCooldown(float duration)
        {
            Duration = duration;
            _nextReady = 0f;
        }

        public bool Ready => UnityEngine.Time.unscaledTime >= _nextReady;
        public float Remaining => Mathf.Max(0f, _nextReady - UnityEngine.Time.unscaledTime);

        public bool TryUse()
        {
            if (!Ready) return false;
            _nextReady = UnityEngine.Time.unscaledTime + Duration;
            return true;
        }

        public void Reset() => _nextReady = UnityEngine.Time.unscaledTime + Duration;
        public void Clear() => _nextReady = 0f;
    }
}
