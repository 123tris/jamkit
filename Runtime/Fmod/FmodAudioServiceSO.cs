using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using Ripple;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// FMOD Studio flavor of the audio service. Same shape as <see cref="AudioServiceSO"/> —
    /// methods route to a scene-side <see cref="FmodAudioServiceRunner"/>, and with no runner
    /// present they are silent no-ops — but sounds are FMOD events and volume drives FMOD buses
    /// instead of mixer params. The Ripple volume variables (and the PlayerPrefs keys behind
    /// them) are shared with the Unity-audio path, so menu sliders and persisted settings work
    /// identically with either backend. Unlike the Unity path, the music EventInstance lives on
    /// this SO, so music survives scene loads seamlessly.
    /// This assembly only compiles when FMOD for Unity is installed (the JAMKIT_FMOD define,
    /// managed automatically by the editor).
    /// </summary>
    [CreateAssetMenu(menuName = "JamKit/Services/FMOD Audio Service", fileName = "FmodAudioService")]
    public sealed class FmodAudioServiceSO : ScriptableObject
    {
        [Header("Buses")]
        [Tooltip("Master bus path. 'bus:/' is FMOD's root bus and always exists.")]
        public string MasterBusPath = "bus:/";
        [Tooltip("Group bus for music, as authored in FMOD Studio's mixer. Missing bus = volume control skipped (warned once).")]
        public string MusicBusPath = "bus:/Music";
        [Tooltip("Group bus for sound effects, as authored in FMOD Studio's mixer.")]
        public string SfxBusPath = "bus:/SFX";

        [Header("Volume State (Ripple)")]
        [Tooltip("Float Variable SOs that hold current 0..1 volume per channel. UI sliders bind to these — use the same assets as your AudioServiceSO.")]
        public FloatVariableSO MasterVolume;
        public FloatVariableSO MusicVolume;
        public FloatVariableSO SfxVolume;

        FmodAudioServiceRunner _runner;

        // Music state lives on the SO (not the runner) so it survives scene loads — a thing FMOD
        // can do that per-scene AudioSources can't. Stale handles after play-mode exit are fine:
        // every use is guarded with isValid(). Retiring = instances mid-fade-out; the runner
        // sweeps them on scene teardown so nothing keeps playing unowned.
        internal EventInstance Music;
        internal float MusicTargetVolume = 1f;
        internal readonly List<EventInstance> Retiring = new();

        internal void RegisterRunner(FmodAudioServiceRunner r) { _runner = r; }
        internal void UnregisterRunner(FmodAudioServiceRunner r) { if (_runner == r) _runner = null; }
        public bool HasRunner => _runner != null;

        /// <summary>Fire-and-forget one-shot at the world origin (fine for 2D events).</summary>
        public void PlaySfx(EventReference sfxEvent) => _runner?.PlaySfxImpl(sfxEvent, Vector3.zero);

        /// <summary>Fire-and-forget one-shot at a world position (3D events attenuate/pan from here).</summary>
        public void PlaySfx(EventReference sfxEvent, Vector3 position) => _runner?.PlaySfxImpl(sfxEvent, position);

        /// <summary>One-shot that follows a moving object for its lifetime (engine hums, projectiles).</summary>
        public void PlaySfxAttached(EventReference sfxEvent, GameObject target) => _runner?.PlaySfxAttachedImpl(sfxEvent, target);

        /// <summary>Start (or switch) the music event. The previous one fades out over the same duration.</summary>
        public void PlayMusic(EventReference musicEvent, float fadeSeconds = 0.5f, float volume = 1f)
            => _runner?.PlayMusicImpl(musicEvent, fadeSeconds, volume);

        public void StopMusic(float fadeSeconds = 0.5f) => _runner?.StopMusicImpl(fadeSeconds);

        /// <summary>
        /// Set a parameter on the playing music event — the FMOD way to shift intensity layers
        /// ("Intensity", "Danger", …) that you authored in Studio.
        /// </summary>
        public void SetMusicParameter(string parameterName, float value)
        {
            if (_runner == null || !Music.isValid()) return;
            Music.setParameterByName(parameterName, value);
        }

        /// <summary>Set a global FMOD parameter (shared across all events that reference it).</summary>
        public void SetGlobalParameter(string parameterName, float value)
            => _runner?.SetGlobalParameterImpl(parameterName, value);

        /// <summary>
        /// Temporarily lower the music bus, then restore to the Ripple-variable value. Makes
        /// stingers and voice pops read clearly.
        /// </summary>
        public void DuckMusic(float duckTo = 0.25f, float holdSeconds = 0.6f, float fadeSeconds = 0.15f)
            => _runner?.DuckMusicImpl(duckTo, holdSeconds, fadeSeconds);

        /// <summary>Play a one-shot and duck the music underneath it ("Wave complete!", "New high score!").
        /// Hold defaults to the event's authored length.</summary>
        public void PlayStinger(EventReference stingerEvent, float duckTo = 0.25f, float holdSeconds = -1f)
        {
            if (_runner == null || stingerEvent.IsNull) return;
            _runner.PlaySfxImpl(stingerEvent, Vector3.zero);
            float hold = holdSeconds >= 0f ? holdSeconds : _runner.GetEventLengthSeconds(stingerEvent, 0.6f);
            _runner.DuckMusicImpl(duckTo, hold, 0.15f);
        }
    }
}
