using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;

namespace Metz.JamKit
{
    /// <summary>
    /// Unity-mixer flavor of the audio service — the fallback backend for projects without
    /// FMOD (when FMOD is installed the scaffold uses <c>FmodAudioServiceSO</c> instead).
    /// Holds the mixer reference, exposed parameter names, and Ripple volume variables
    /// (mark them persistent so settings survive restarts). Methods route to a scene-side
    /// <see cref="AudioServiceRunner"/>; with no runner present they are silent no-ops.
    /// </summary>
    [CreateAssetMenu(menuName = "JamKit/Services/Audio Service (Unity)", fileName = "AudioService")]
    public sealed class AudioServiceSO : ServiceSO<AudioServiceRunner>
    {
        [Header("Mixer")]
        [Tooltip("Optional AudioMixer with exposed volume parameters. Leave null to skip mixing.")]
        public AudioMixer Mixer;
        public string MasterParam = "MasterVol";
        public string MusicParam = "MusicVol";
        public string SfxParam = "SfxVol";

        [Header("Volume State (Ripple)")]
        [Tooltip("Float Variable SOs that hold current 0..1 volume per channel. UI sliders bind to these; tick Persist on the variables so settings survive restarts.")]
        public FloatVariableSO MasterVolume;
        public FloatVariableSO MusicVolume;
        public FloatVariableSO SfxVolume;

        [Header("SFX Pool")]
        [Min(1)] public int SfxPoolSize = 12;

        public AudioSource PlaySfx(AudioClip clip, float volume = 1f, float pitchVariation = 0f)
            => Runner == null ? null : Runner.PlaySfxImpl(clip, volume, pitchVariation);

        public void PlayMusic(AudioClip clip, float fadeSeconds = 0.5f, float volume = 1f)
            => Runner?.PlayMusicImpl(clip, fadeSeconds, volume);

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void StopMusic(float fadeSeconds = 0.5f) => Runner?.StopMusicImpl(fadeSeconds);

        /// <summary>
        /// Temporarily lower the music channel (through the mixer's music param), then restore to
        /// the Ripple-variable value. Makes stingers and voice pops read clearly.
        /// </summary>
        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void DuckMusic(float duckTo = 0.25f, float holdSeconds = 0.6f, float fadeSeconds = 0.15f)
            => Runner?.DuckMusicImpl(duckTo, holdSeconds, fadeSeconds);

        /// <summary>Play a one-shot and duck the music underneath it ("Wave complete!", "New high score!").</summary>
        public AudioSource PlayStinger(AudioClip clip, float volume = 1f, float duckTo = 0.25f, float holdSeconds = -1f)
        {
            if (Runner == null || clip == null) return null;
            var src = Runner.PlaySfxImpl(clip, volume, 0f);
            Runner.DuckMusicImpl(duckTo, holdSeconds < 0f ? clip.length : holdSeconds, 0.15f);
            return src;
        }
    }
}
