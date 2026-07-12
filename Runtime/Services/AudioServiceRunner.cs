using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Metz.JamKit
{
    /// <summary>
    /// Scene-side host for <see cref="AudioServiceSO"/>: spawns the SFX one-shot pool,
    /// owns the music crossfade sources, and subscribes to the volume Ripple variables so
    /// changes drive the AudioMixer. Volume persistence belongs to the variables themselves
    /// (tick Persist on the assets) — this runner only applies values.
    /// </summary>
    public sealed class AudioServiceRunner : ServiceRunner<AudioServiceSO, AudioServiceRunner>
    {
        readonly List<AudioSource> _sfxPool = new();
        AudioSource _musicA, _musicB;
        bool _musicAActive = true;
        AudioMixerGroup _musicGroup, _sfxGroup;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!IsRegistered) return;
            BuildPool();
            SubscribeVolumes();
        }

        void Start()
        {
            // AudioMixer.SetFloat is unreliable during Awake/OnEnable (Unity applies the mixer's own
            // snapshot after enable, stomping early writes), so current volumes are applied here.
            if (IsRegistered) ApplyCurrentVolumes();
        }

        protected override void OnDisable()
        {
            if (IsRegistered) UnsubscribeVolumes();
            base.OnDisable();
        }

        // -------------------- API used by AudioServiceSO --------------------

        internal AudioSource PlaySfxImpl(AudioClip clip, float volume, float pitchVariation)
        {
            if (clip == null) return null;
            var src = NextFreeSource();
            src.clip = clip;
            src.volume = Mathf.Clamp01(volume);
            src.pitch = pitchVariation <= 0f ? 1f : 1f + Random.Range(-pitchVariation, pitchVariation);
            src.loop = false;
            src.Play();
            return src;
        }

        internal void PlayMusicImpl(AudioClip clip, float fadeSeconds, float targetVolume)
        {
            if (clip == null) return;
            var next = _musicAActive ? _musicB : _musicA;
            var current = _musicAActive ? _musicA : _musicB;
            _musicAActive = !_musicAActive;
            next.clip = clip;
            next.volume = 0f;
            next.loop = true;
            next.Play();
            StartCoroutine(Crossfade(current, next, targetVolume, fadeSeconds));
        }

        internal void StopMusicImpl(float fadeSeconds)
        {
            if (_musicA != null && _musicA.isPlaying) StartCoroutine(FadeOut(_musicA, fadeSeconds));
            if (_musicB != null && _musicB.isPlaying) StartCoroutine(FadeOut(_musicB, fadeSeconds));
        }

        Coroutine _duckRoutine;

        internal void DuckMusicImpl(float duckTo, float holdSeconds, float fadeSeconds)
        {
            if (_duckRoutine != null) StopCoroutine(_duckRoutine);
            _duckRoutine = StartCoroutine(DuckRoutine(Mathf.Clamp01(duckTo), holdSeconds, Mathf.Max(0.01f, fadeSeconds)));
        }

        // Ducks the mixer's music param directly (not the sources — those belong to the crossfade)
        // and restores to the user's Ripple value, so the settings slider always wins in the end.
        IEnumerator DuckRoutine(float duckTo, float hold, float fade)
        {
            float userLinear = Service.MusicVolume != null ? Service.MusicVolume.CurrentValue : 1f;
            float ducked = userLinear * duckTo;

            for (float t = 0f; t < fade; t += Time.unscaledDeltaTime)
            {
                ApplyToMixer(Service.MusicParam, Mathf.Lerp(userLinear, ducked, t / fade));
                yield return null;
            }
            ApplyToMixer(Service.MusicParam, ducked);

            for (float t = 0f; t < hold; t += Time.unscaledDeltaTime) yield return null;

            for (float t = 0f; t < fade; t += Time.unscaledDeltaTime)
            {
                userLinear = Service.MusicVolume != null ? Service.MusicVolume.CurrentValue : 1f;
                ApplyToMixer(Service.MusicParam, Mathf.Lerp(userLinear * duckTo, userLinear, t / fade));
                yield return null;
            }
            ApplyToMixer(Service.MusicParam, Service.MusicVolume != null ? Service.MusicVolume.CurrentValue : 1f);
            _duckRoutine = null;
        }

        // -------------------- pool + groups --------------------

        void BuildPool()
        {
            if (Service.Mixer != null)
            {
                var groups = Service.Mixer.FindMatchingGroups(string.Empty);
                foreach (var g in groups)
                {
                    if (g.name == "Music") _musicGroup = g;
                    else if (g.name == "SFX") _sfxGroup = g;
                }
            }

            for (int i = 0; i < Service.SfxPoolSize; i++)
                _sfxPool.Add(MakeSource("SFX_" + i, _sfxGroup));

            _musicA = MakeSource("Music_A", _musicGroup);
            _musicB = MakeSource("Music_B", _musicGroup);
            _musicA.loop = _musicB.loop = true;
        }

        AudioSource MakeSource(string n, AudioMixerGroup g)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            if (g != null) src.outputAudioMixerGroup = g;
            return src;
        }

        AudioSource NextFreeSource()
        {
            for (int i = 0; i < _sfxPool.Count; i++)
                if (!_sfxPool[i].isPlaying) return _sfxPool[i];
            var stolen = _sfxPool[0];
            _sfxPool.RemoveAt(0);
            _sfxPool.Add(stolen);
            return stolen;
        }

        IEnumerator Crossfade(AudioSource from, AudioSource to, float targetVolume, float seconds)
        {
            float fromStart = from != null ? from.volume : 0f;
            float t = 0f; seconds = Mathf.Max(0.0001f, seconds);
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / seconds);
                if (from != null) from.volume = Mathf.Lerp(fromStart, 0f, p);
                if (to != null) to.volume = Mathf.Lerp(0f, targetVolume, p);
                yield return null;
            }
            if (from != null) { from.Stop(); from.volume = fromStart; }
        }

        IEnumerator FadeOut(AudioSource src, float seconds)
        {
            float start = src.volume;
            float t = 0f; seconds = Mathf.Max(0.0001f, seconds);
            while (t < seconds) { t += Time.unscaledDeltaTime; src.volume = Mathf.Lerp(start, 0f, t / seconds); yield return null; }
            src.Stop(); src.volume = start;
        }

        // -------------------- volume binding --------------------

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

        void OnMasterChanged(float v) => ApplyToMixer(Service.MasterParam, v);
        void OnMusicChanged(float v)  => ApplyToMixer(Service.MusicParam,  v);
        void OnSfxChanged(float v)    => ApplyToMixer(Service.SfxParam,    v);

        void ApplyToMixer(string exposedParam, float linear)
        {
            if (Service.Mixer == null || string.IsNullOrEmpty(exposedParam)) return;
            float db = linear <= 0.0001f ? -80f : Mathf.Log10(Mathf.Clamp01(linear)) * 20f;
            Service.Mixer.SetFloat(exposedParam, db);
        }

        void ApplyCurrentVolumes()
        {
            if (Service.MasterVolume != null) ApplyToMixer(Service.MasterParam, Service.MasterVolume.CurrentValue);
            if (Service.MusicVolume  != null) ApplyToMixer(Service.MusicParam,  Service.MusicVolume.CurrentValue);
            if (Service.SfxVolume    != null) ApplyToMixer(Service.SfxParam,    Service.SfxVolume.CurrentValue);
        }
    }
}
