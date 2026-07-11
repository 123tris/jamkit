using Unity.Cinemachine;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Screen shake through a Cinemachine impulse. Every JamKit camera already carries a
    /// <see cref="CinemachineImpulseListener"/>, so this works out of the box: drop it on the
    /// player/enemy (shakes when they take damage) or anywhere and wire a Ripple event.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public sealed class CameraShake : JuiceBehaviour
    {
        [Header("Shake")]
        [Tooltip("Impulse velocity magnitude at strength 1.")]
        public float Force = 1f;
        [Tooltip("Kick direction. Default is a downward camera kick, the classic hit feel.")]
        public Vector3 Direction = new(0f, -1f, 0f);

        CinemachineImpulseSource _source;

        protected override bool DefaultOnSiblingDamage => true;

        void Awake() => _source = GetComponent<CinemachineImpulseSource>();

        public override void Play(float strength)
        {
            if (_source == null) return;
            var dir = Direction.sqrMagnitude > 0f ? Direction.normalized : Vector3.down;
            _source.GenerateImpulseWithVelocity(dir * (Force * strength));
        }
    }
}
