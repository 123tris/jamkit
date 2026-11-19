using UnityEngine;

namespace Metz.JamKit
{
    public struct Cooldown
    {
        readonly bool unscaled;
        float readyAt;

        public Cooldown(bool unscaled)
        {
            this.unscaled = unscaled;
            readyAt = 0f;
        }

        float Now => unscaled ? Time.unscaledTime : Time.time;

        public bool Ready => Now >= readyAt;

        public float Remaining => Mathf.Max(0f, readyAt - Now);

        public void Start(float seconds) => readyAt = Now + seconds;

        public bool TryUse(float seconds)
        {
            if (!Ready) return false;
            Start(seconds);
            return true;
        }
    }
}
