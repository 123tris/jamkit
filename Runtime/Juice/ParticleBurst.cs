using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Plays (or emits into) assigned ParticleSystems on trigger. Pair with a child particle
    /// system set to not play on awake. With <see cref="EmitCount"/> &gt; 0, emits that many
    /// particles per trigger (scaled by strength) instead of restarting the system.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ParticleBurst : JuiceBehaviour
    {
        [Header("Particles")]
        [Tooltip("Systems to play. Empty = all ParticleSystems on this object and children (found on Awake).")]
        public ParticleSystem[] Systems;
        [Tooltip("0 = Play() the systems. >0 = Emit() this many particles, scaled by strength.")]
        [Min(0)] public int EmitCount = 0;

        protected override bool DefaultOnSiblingDamage => true;

        void Awake()
        {
            if (Systems == null || Systems.Length == 0)
                Systems = GetComponentsInChildren<ParticleSystem>(true);
        }

        public override void Play(float strength)
        {
            for (int i = 0; i < Systems.Length; i++)
            {
                var ps = Systems[i];
                if (ps == null) continue;
                if (EmitCount > 0) ps.Emit(Mathf.Max(1, Mathf.RoundToInt(EmitCount * strength)));
                else ps.Play();
            }
        }
    }
}
