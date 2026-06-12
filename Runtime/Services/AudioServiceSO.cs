using Ripple;
using UnityEngine;
using UnityEngine.Audio;

namespace Metz.JamKit
{
    /// <summary>
    /// ScriptableObject definition of the audio service. Holds the mixer reference,
    /// exposed parameter names, and Ripple volume variables. Methods route to a
    /// scene-side <see cref="AudioServiceRunner"/> — drop one in your bootstrap scene
    /// and assign this SO so the methods can do work. With no runner present the
    /// methods are silent no-ops (useful in tests, editor preview, or main menu scenes).
    /// </summary>
    [CreateAssetMenu(menuName = "JamKit/Services/Audio Service", fileName = "AudioService")]
    public sealed class AudioServiceSO : ScriptableObject
    {
        [Header("Mixer")]
        [Tooltip("Optional AudioMixer with exposed volume parameters. Leave null to skip mixing.")]
        public AudioMixer Mixer;
        public string MasterParam = "MasterVol";
        public string MusicParam = "MusicVol";
        public string SfxParam = "SfxVol";

        [Header("Volume State (Ripple)")]
        [Tooltip("Float Variable SOs that hold current 0..1 volume per channel. UI sliders bind to these.")]
        public FloatVariableSO MasterVolume;
        public FloatVariableSO MusicVolume;
        public FloatVariableSO SfxVolume;

        [Header("SFX Pool")]
        [Min(1)] public int SfxPoolSize = 12;

        AudioServiceRunner _runner;

        internal void RegisterRunner(AudioServiceRunner r) { _runner = r; }
        internal void UnregisterRunner(AudioServiceRunner r) { if (_runner == r) _runner = null; }
        public bool HasRunner => _runner != null;

        public AudioSource PlaySfx(AudioClip clip, float volume = 1f, float pitchVariation = 0f)
            => _runner == null ? null : _runner.PlaySfxImpl(clip, volume, pitchVariation);

        public void PlayMusic(AudioClip clip, float fadeSeconds = 0.5f, float volume = 1f)
            => _runner?.PlayMusicImpl(clip, fadeSeconds, volume);

        public void StopMusic(float fadeSeconds = 0.5f) => _runner?.StopMusicImpl(fadeSeconds);
    }
}
