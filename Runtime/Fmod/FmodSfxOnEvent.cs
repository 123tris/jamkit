using FMODUnity;
using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// FMOD flavor of <see cref="SfxOnEvent"/>: plays an FMOD event one-shot through
    /// <see cref="FmodAudioServiceSO"/> — hit sounds, death sounds, wave stingers. Audio glue,
    /// not a feedback: Feel can't drive FMOD, so this component is the bridge. No clip array or
    /// pitch variation here: multi-sound randomization and pitch wobble are authored on the
    /// event in FMOD Studio, where they belong. Trigger per-instance by wiring
    /// <c>Health.OnDamaged → Play</c>, globally via <see cref="PlayOn"/>, or from code.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FmodSfxOnEvent : MonoBehaviour
    {
        [Required] public FmodAudioServiceSO AudioService;

        [Header("Sound")]
        [Tooltip("The FMOD event to fire. Randomization/pitch variation live on the event in Studio.")]
        public EventReference Event;
        [Tooltip("Fire the one-shot at this object's position so 3D events pan/attenuate correctly.")]
        public bool AtThisPosition = true;
        [Tooltip("Duck the music underneath this sound — turns any event into a stinger (wave complete, high score).")]
        public bool DuckMusic = false;

        [Header("Global Trigger (Ripple, optional)")]
        [Tooltip("Play whenever this global event fires.")]
        public VoidEventSO PlayOn;

        void OnEnable() { if (PlayOn != null) PlayOn.AddListener(Play); }
        void OnDisable() { if (PlayOn != null) PlayOn.RemoveListener(Play); }

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void Play()
        {
            if (AudioService == null || Event.IsNull) return;
            if (DuckMusic) AudioService.PlayStinger(Event);
            else AudioService.PlaySfx(Event, AtThisPosition ? transform.position : Vector3.zero);
        }
    }
}
