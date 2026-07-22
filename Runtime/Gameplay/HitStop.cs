using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Freeze-frame on hit: pushes a near-zero timescale for a few hundredths of a second through
    /// <see cref="TimeServiceSO"/>, which composes safely with pause/slow-mo. This is the ONLY
    /// legal way to freeze time — Feel's time feedbacks stomp <c>Time.timeScale</c> directly and
    /// fight the pause stack (see PILLARS.md). Needs a <see cref="TimeServiceRunner"/> in the
    /// scene (JamKitCore has one). Trigger per-instance by wiring <c>Health.OnDamaged →
    /// Play(float)</c>, globally via <see cref="PlayOn"/>, or from code.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitStop : MonoBehaviour
    {
        [Required] public TimeServiceSO TimeService;

        [Header("Stop")]
        [Tooltip("Freeze length at strength 1. Constant or a shared Ripple variable (a global 'game feel' knob).")]
        public FloatReference Duration = new(0.05f);
        [Tooltip("Cap so a huge strength value can't freeze the game for seconds.")]
        [Min(0f)] public float MaxDuration = 0.25f;
        [Range(0f, 1f)] public float TimeScale = 0f;

        [Header("Global Trigger (Ripple, optional)")]
        [Tooltip("Play at strength 1 whenever this global event fires.")]
        public VoidEventSO PlayOn;
        [Tooltip("Play scaled by the event's value (e.g. a shared damage event).")]
        public FloatEvent PlayOnValue;

        void OnEnable()
        {
            if (PlayOn != null) PlayOn.AddListener(Play);
            if (PlayOnValue != null) PlayOnValue.AddListener(Play);
        }

        void OnDisable()
        {
            if (PlayOn != null) PlayOn.RemoveListener(Play);
            if (PlayOnValue != null) PlayOnValue.RemoveListener(Play);
        }

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void Play() => Play(1f);

        /// <summary>Freeze scaled by strength — wire Health.OnDamaged here for damage-scaled stops.</summary>
        public void Play(float strength)
        {
            if (TimeService == null) return;
            TimeService.FreezeForSeconds(Mathf.Min(Duration.Value * strength, MaxDuration), TimeScale);
        }
    }
}
