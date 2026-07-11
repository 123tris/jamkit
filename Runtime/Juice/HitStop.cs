using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Freeze-frame on hit: pushes a near-zero timescale for a few hundredths of a second through
    /// <see cref="TimeServiceSO"/>, which composes safely with pause/slow-mo. Needs a
    /// <see cref="TimeServiceRunner"/> in the scene (JamKitCore has one).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitStop : JuiceBehaviour
    {
        [Header("Service")]
        public TimeServiceSO TimeService;

        [Header("Stop")]
        [Min(0f)] public float Duration = 0.05f;
        [Tooltip("Cap so a huge strength value can't freeze the game for seconds.")]
        [Min(0f)] public float MaxDuration = 0.25f;
        [Range(0f, 1f)] public float TimeScale = 0f;

        protected override bool DefaultOnSiblingDamage => true;

        public override void Play(float strength)
        {
            if (TimeService == null) return;
            TimeService.FreezeForSeconds(Mathf.Min(Duration * strength, MaxDuration), TimeScale);
        }
    }
}
