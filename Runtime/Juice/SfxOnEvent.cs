using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Plays a one-shot SFX through <see cref="AudioServiceSO"/> on trigger — hit sounds, death
    /// sounds, wave stingers. Multiple clips = random pick per play, and a little pitch variation
    /// is on by default because repeated identical sounds are the fastest way to sound cheap.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SfxOnEvent : JuiceBehaviour
    {
        [Header("Service")]
        public AudioServiceSO AudioService;

        [Header("Sound")]
        [Tooltip("One is played at random each trigger.")]
        public AudioClip[] Clips;
        [Range(0f, 1f)] public float Volume = 1f;
        [Tooltip("Random pitch offset per play (0.08 = ±8%). Kills the machine-gun-same-sample effect.")]
        [Range(0f, 0.5f)] public float PitchVariation = 0.08f;
        [Tooltip("Duck the music underneath this sound — turns any clip into a stinger (wave complete, high score).")]
        public bool DuckMusic = false;

        protected override bool DefaultOnSiblingDamage => true;

        public override void Play(float strength)
        {
            if (AudioService == null || Clips == null || Clips.Length == 0) return;
            var clip = Clips[Random.Range(0, Clips.Length)];
            if (clip == null) return;
            if (DuckMusic) AudioService.PlayStinger(clip, Volume);
            else AudioService.PlaySfx(clip, Volume, PitchVariation);
        }
    }
}
