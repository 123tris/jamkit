using System.Collections;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Scene-side host for <see cref="FmodAudioServiceSO"/>: subscribes the Ripple volume
    /// variables to the FMOD buses, persists volume to PlayerPrefs (same keys as
    /// <see cref="AudioServiceRunner"/>, so settings carry over if you switch backends), and
    /// runs the music fade/duck coroutines. The music EventInstance itself lives on the SO and
    /// deliberately survives this runner — only instances mid-fade-out are swept on teardown.
    /// </summary>
    public sealed class FmodAudioServiceRunner : MonoBehaviour
    {
        [Tooltip("The FmodAudioServiceSO this runner serves. Required.")]
        public FmodAudioServiceSO Service;

        // Deliberately shared with AudioServiceRunner: one set of persisted volumes per game,
        // whichever audio backend is in the scene.
        const string PrefsMaster = "JamKit.Vol.Master";
        const string PrefsMusic = "JamKit.Vol.Music";
        const string PrefsSfx = "JamKit.Vol.Sfx";

        Bus _masterBus, _musicBus, _sfxBus;
        bool _busesResolved;
        bool _warnedNotReady;

        void OnEnable()
        {
            if (Service == null)
            {
                Debug.LogWarning($"[JamKit] {name}: FmodAudioServiceRunner has no Service assigned.");
                return;
            }
            Service.RegisterRunner(this);
            SubscribeVolumes();
        }

        void Start()
        {
            // Bus lookups need the FMOD system up and the master banks loaded, which is not
            // guaranteed during OnEnable on all platforms (WebGL streams banks in), so persisted
            // volumes are applied here — and re-applied lazily if banks arrive even later.
            if (Service == null) return;
            ApplyPersistedVolumes();
            // A music instance may have survived from the previous scene mid-fade; make sure it
            // sits at its target volume now that no coroutine owns it.
            if (Service.Music.isValid()) Service.Music.setVolume(Service.MusicTargetVolume);
        }

        void OnDisable()
        {
            if (Service == null) return;
            UnsubscribeVolumes();
            // Fade-out coroutines die with the scene; hard-stop their instances so nothing keeps
            // playing unowned. The active music instance intentionally lives on.
            foreach (var inst in Service.Retiring)
            {
                if (!inst.isValid()) continue;
                inst.stop(STOP_MODE.IMMEDIATE);
                inst.release();
            }
            Service.Retiring.Clear();
            Service.UnregisterRunner(this);
        }

        // -------------------- API used by FmodAudioServiceSO --------------------

        internal void PlaySfxImpl(EventReference sfxEvent, Vector3 position)
        {
            if (sfxEvent.IsNull) return;
            try { RuntimeManager.PlayOneShot(sfxEvent, position); }
            catch (EventNotFoundException) { Debug.LogWarning($"[JamKit] FMOD event not found: {sfxEvent}"); }
            catch (SystemNotInitializedException e) { WarnNotReady(e); }
        }

        internal void PlaySfxAttachedImpl(EventReference sfxEvent, GameObject target)
        {
            if (sfxEvent.IsNull || target == null) return;
            try { RuntimeManager.PlayOneShotAttached(sfxEvent, target); }
            catch (EventNotFoundException) { Debug.LogWarning($"[JamKit] FMOD event not found: {sfxEvent}"); }
            catch (SystemNotInitializedException e) { WarnNotReady(e); }
        }

        internal void PlayMusicImpl(EventReference musicEvent, float fadeSeconds, float targetVolume)
        {
            if (musicEvent.IsNull) return;
            StopMusicImpl(fadeSeconds);

            EventInstance inst;
            try { inst = RuntimeManager.CreateInstance(musicEvent); }
            catch (EventNotFoundException) { Debug.LogWarning($"[JamKit] FMOD event not found: {musicEvent}"); return; }
            catch (SystemNotInitializedException e) { WarnNotReady(e); return; }

            Service.Music = inst;
            Service.MusicTargetVolume = Mathf.Clamp01(targetVolume);
            inst.setVolume(fadeSeconds > 0f ? 0f : Service.MusicTargetVolume);
            inst.start();
            if (fadeSeconds > 0f) StartCoroutine(FadeTo(inst, Service.MusicTargetVolume, fadeSeconds, stopWhenDone: false));
        }

        internal void StopMusicImpl(float fadeSeconds)
        {
            if (!Service.Music.isValid()) return;
            var inst = Service.Music;
            Service.Music = default;

            if (fadeSeconds <= 0f)
            {
                inst.stop(STOP_MODE.IMMEDIATE);
                inst.release();
                return;
            }
            Service.Retiring.Add(inst);
            StartCoroutine(FadeTo(inst, 0f, fadeSeconds, stopWhenDone: true));
        }

        internal void SetGlobalParameterImpl(string parameterName, float value)
        {
            try { RuntimeManager.StudioSystem.setParameterByName(parameterName, value); }
            catch (SystemNotInitializedException e) { WarnNotReady(e); }
        }

        internal float GetEventLengthSeconds(EventReference eventReference, float fallback)
        {
            try
            {
                var desc = RuntimeManager.GetEventDescription(eventReference);
                if (desc.getLength(out int ms) == FMOD.RESULT.OK && ms > 0) return ms / 1000f;
            }
            catch (EventNotFoundException) { }
            catch (SystemNotInitializedException) { }
            return fallback;
        }

        Coroutine _duckRoutine;

        internal void DuckMusicImpl(float duckTo, float holdSeconds, float fadeSeconds)
        {
            if (_duckRoutine != null) StopCoroutine(_duckRoutine);
            _duckRoutine = StartCoroutine(DuckRoutine(Mathf.Clamp01(duckTo), holdSeconds, Mathf.Max(0.01f, fadeSeconds)));
        }

        // Ducks the music bus directly (not the instance — that belongs to the fades) and
        // restores to the user's Ripple value, so the settings slider always wins in the end.
        IEnumerator DuckRoutine(float duckTo, float hold, float fade)
        {
            if (!ResolveBuses() || !_musicBus.isValid()) yield break;

            float userLinear = Service.MusicVolume != null ? Service.MusicVolume.CurrentValue : 1f;
            float ducked = userLinear * duckTo;

            for (float t = 0f; t < fade; t += Time.unscaledDeltaTime)
            {
                _musicBus.setVolume(Mathf.Lerp(userLinear, ducked, t / fade));
                yield return null;
            }
            _musicBus.setVolume(ducked);

            for (float t = 0f; t < hold; t += Time.unscaledDeltaTime) yield return null;

            for (float t = 0f; t < fade; t += Time.unscaledDeltaTime)
            {
                userLinear = Service.MusicVolume != null ? Service.MusicVolume.CurrentValue : 1f;
                _musicBus.setVolume(Mathf.Lerp(userLinear * duckTo, userLinear, t / fade));
                yield return null;
            }
            _musicBus.setVolume(Service.MusicVolume != null ? Service.MusicVolume.CurrentValue : 1f);
            _duckRoutine = null;
        }

        IEnumerator FadeTo(EventInstance inst, float target, float seconds, bool stopWhenDone)
        {
            inst.getVolume(out float start);
            float t = 0f; seconds = Mathf.Max(0.0001f, seconds);
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                if (!inst.isValid()) yield break;
                inst.setVolume(Mathf.Lerp(start, target, Mathf.Clamp01(t / seconds)));
                yield return null;
            }
            if (!inst.isValid()) yield break;
            if (stopWhenDone)
            {
                inst.stop(STOP_MODE.IMMEDIATE);
                inst.release();
                Service.Retiring.Remove(inst);
            }
        }

        // -------------------- volume binding + persistence --------------------

        void SubscribeVolumes()
        {
            if (Service.MasterVolume != null) Service.MasterVolume.OnValueChanged += OnMasterChanged;
            if (Service.MusicVolume  != null) Service.MusicVolume.OnValueChanged  += OnMusicChanged;
            if (Service.SfxVolume    != null) Service.SfxVolume.OnValueChanged    += OnSfxChanged;
        }

        void UnsubscribeVolumes()
        {
            if (Service.MasterVolume != null) Service.MasterVolume.OnValueChanged -= OnMasterChanged;
            if (Service.MusicVolume  != null) Service.MusicVolume.OnValueChanged  -= OnMusicChanged;
            if (Service.SfxVolume    != null) Service.SfxVolume.OnValueChanged    -= OnSfxChanged;
        }

        void OnMasterChanged(float v) { ApplyToBus(ref _masterBus, v); PlayerPrefs.SetFloat(PrefsMaster, v); }
        void OnMusicChanged(float v)  { ApplyToBus(ref _musicBus,  v); PlayerPrefs.SetFloat(PrefsMusic,  v); }
        void OnSfxChanged(float v)    { ApplyToBus(ref _sfxBus,    v); PlayerPrefs.SetFloat(PrefsSfx,    v); }

        void ApplyToBus(ref Bus bus, float linear)
        {
            if (!ResolveBuses()) return;
            // Bus volume is linear (unlike mixer dB) — no conversion needed.
            if (bus.isValid()) bus.setVolume(Mathf.Clamp01(linear));
        }

        void ApplyPersistedVolumes()
        {
            if (Service.MasterVolume != null) Service.MasterVolume.SetCurrentValue(PlayerPrefs.GetFloat(PrefsMaster, Service.MasterVolume.CurrentValue));
            if (Service.MusicVolume  != null) Service.MusicVolume.SetCurrentValue(PlayerPrefs.GetFloat(PrefsMusic,  Service.MusicVolume.CurrentValue));
            if (Service.SfxVolume    != null) Service.SfxVolume.SetCurrentValue(PlayerPrefs.GetFloat(PrefsSfx,      Service.SfxVolume.CurrentValue));
            // SetCurrentValue only notifies on change, so unchanged values never reach the buses
            // on a fresh scene. Push them explicitly.
            if (Service.MasterVolume != null) ApplyToBus(ref _masterBus, Service.MasterVolume.CurrentValue);
            if (Service.MusicVolume  != null) ApplyToBus(ref _musicBus,  Service.MusicVolume.CurrentValue);
            if (Service.SfxVolume    != null) ApplyToBus(ref _sfxBus,    Service.SfxVolume.CurrentValue);
        }

        /// <summary>
        /// Bus lookup, retried until the FMOD system and banks are ready (WebGL streams banks in
        /// after scene load). Missing group buses warn once with the authoring fix.
        /// </summary>
        bool ResolveBuses()
        {
            if (_busesResolved) return true;
            try
            {
                if (!RuntimeManager.HaveAllBanksLoaded) return false;
                _masterBus = LookUp(Service.MasterBusPath);
                _musicBus  = LookUp(Service.MusicBusPath);
                _sfxBus    = LookUp(Service.SfxBusPath);
                _busesResolved = true;
                return true;
            }
            catch (SystemNotInitializedException e)
            {
                WarnNotReady(e);
                return false;
            }
        }

        Bus LookUp(string path)
        {
            if (string.IsNullOrEmpty(path)) return default;
            try { return RuntimeManager.GetBus(path); }
            catch (BusNotFoundException)
            {
                Debug.LogWarning($"[JamKit] FMOD bus '{path}' not found — volume control for that channel is off. " +
                                 "Add a matching group bus in FMOD Studio's mixer (or change the path on the FmodAudioServiceSO) and rebuild banks.");
                return default;
            }
        }

        void WarnNotReady(System.Exception e)
        {
            if (_warnedNotReady) return;
            _warnedNotReady = true;
            Debug.LogWarning($"[JamKit] FMOD is not initialized — audio calls are no-ops. Run the FMOD Setup Wizard and build banks. ({e.Message})");
        }
    }
}
