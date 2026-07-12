using FMODUnity;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// FMOD flavor of <see cref="SfxOnEvent"/>: plays an FMOD event one-shot through
    /// <see cref="FmodAudioServiceSO"/> on trigger — hit sounds, death sounds, wave stingers.
    /// No clip array or pitch variation here: multi-sound randomization and pitch wobble are
    /// authored on the event in FMOD Studio, where they belong.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FmodSfxOnEvent : JuiceBehaviour
    {
        [Header("Service")]
        public FmodAudioServiceSO AudioService;

        [Header("Sound")]
        [Tooltip("The FMOD event to fire. Randomization/pitch variation live on the event in Studio.")]
        public EventReference Event;
        [Tooltip("Fire the one-shot at this object's position so 3D events pan/attenuate correctly.")]
        public bool AtThisPosition = true;
        [Tooltip("Duck the music underneath this sound — turns any event into a stinger (wave complete, high score).")]
        public bool DuckMusic = false;

        protected override bool DefaultOnSiblingDamage => true;

        public override void Play(float strength)
        {
            if (AudioService == null || Event.IsNull) return;
            if (DuckMusic) AudioService.PlayStinger(Event);
            else AudioService.PlaySfx(Event, AtThisPosition ? transform.position : Vector3.zero);
        }
    }
}
