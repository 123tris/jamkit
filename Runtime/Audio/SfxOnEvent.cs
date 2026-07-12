using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Plays a one-shot SFX through <see cref="AudioServiceSO"/> (Unity-mixer backend) — hit
    /// sounds, death sounds, wave stingers. Audio glue, not a feedback: Feel can't reach the
    /// audio layer, so this component is the bridge. Multiple clips = random pick per play, and
    /// a little pitch variation is on by default because repeated identical sounds are the
    /// fastest way to sound cheap. Trigger per-instance by wiring <c>Health.OnDamaged → Play</c>,
    /// globally via <see cref="PlayOn"/>, or from code.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SfxOnEvent : MonoBehaviour
    {
        [Required] public AudioServiceSO AudioService;

        [Header("Sound")]
        [Tooltip("One is played at random each trigger.")]
        public AudioClip[] Clips;
        [Range(0f, 1f)] public float Volume = 1f;
        [Tooltip("Random pitch offset per play (0.08 = ±8%). Kills the machine-gun-same-sample effect.")]
        [Range(0f, 0.5f)] public float PitchVariation = 0.08f;
        [Tooltip("Duck the music underneath this sound — turns any clip into a stinger (wave complete, high score).")]
        public bool DuckMusic = false;

        [Header("Global Trigger (Ripple, optional)")]
        [Tooltip("Play whenever this global event fires.")]
        public VoidEventSO PlayOn;

        void OnEnable() { if (PlayOn != null) PlayOn.AddListener(Play); }
        void OnDisable() { if (PlayOn != null) PlayOn.RemoveListener(Play); }

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void Play()
        {
            if (AudioService == null || Clips == null || Clips.Length == 0) return;
            var clip = Clips[Random.Range(0, Clips.Length)];
            if (clip == null) return;
            if (DuckMusic) AudioService.PlayStinger(clip, Volume);
            else AudioService.PlaySfx(clip, Volume, PitchVariation);
        }
    }
}
