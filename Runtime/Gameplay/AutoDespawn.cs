using Ripple;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Destroys this object after a delay. The #1 fix for bullets/particles that leak forever.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AutoDespawn : MonoBehaviour
    {
        [Tooltip("Lifetime before destroy. Constant or a shared Ripple variable.")]
        public FloatReference Seconds = new(3f);
        [Tooltip("Use unscaled time so the lifetime ignores pause/slow-mo.")]
        public bool Unscaled = false;

        float _due;

        void OnEnable() => Arm();

        void Arm() => _due = Now() + Seconds.Value;

        void Update()
        {
            if (Now() < _due) return;
            Destroy(gameObject);
        }

        float Now() => Unscaled ? Time.unscaledTime : Time.time;
    }
}
