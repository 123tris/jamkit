using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Metz.JamKit
{
    /// <summary>
    /// Scene-side host for <see cref="AudioServiceSO"/>: spawns the SFX one-shot pool,
    /// owns the music crossfade sources, and subscribes to volume Ripple variables so changes
    /// drive the AudioMixer. Persists volume to PlayerPrefs.
    /// </summary>
    public sealed class AudioServiceRunner : MonoBehaviour
    {
        [Tooltip("The AudioServiceSO this runner serves. Required.")]
        public AudioServiceSO Service;

        const string PrefsMaster = "JamKit.Vol.Master";
        const string PrefsMusic = "JamKit.Vol.Music";
        const string PrefsSfx = "JamKit.Vol.Sfx";

        readonly List<AudioSource> _sfxPool = new();
        AudioSource _musicA, _musicB;
        bool _musicAActive = true;
        AudioMixerGroup _musicGroup, _sfxGroup;

        void OnEnable()
        {
            if (Service == null)
            {
                Debug.LogWarning($"[JamKit] {name}: AudioServiceRunner has no Service assigned.");
                return;
            }
            Service.RegisterRunner(this);
            BuildPool();
            SubscribeVolumes();
        }

        void Start()
        {
            // AudioMixer.SetFloat is unreliable during Awake/OnEnable (Unity applies the mixer's own
            // snapshot after enable, stomping early writes), so persisted volumes are applied here.
            if (Service != null) ApplyPersistedVolumes();
        }

        void OnDisable()
        {
            if (Service == null) return;
            UnsubscribeVolumes();
            Service.UnregisterRunner(this);
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

        void OnMasterChanged(float v) { ApplyToMixer(Service.MasterParam, v); PlayerPrefs.SetFloat(PrefsMaster, v); }
        void OnMusicChanged(float v)  { ApplyToMixer(Service.MusicParam,  v); PlayerPrefs.SetFloat(PrefsMusic,  v); }
        void OnSfxChanged(float v)    { ApplyToMixer(Service.SfxParam,    v); PlayerPrefs.SetFloat(PrefsSfx,    v); }

        void ApplyToMixer(string exposedParam, float linear)
        {
            if (Service.Mixer == null || string.IsNullOrEmpty(exposedParam)) return;
            float db = linear <= 0.0001f ? -80f : Mathf.Log10(Mathf.Clamp01(linear)) * 20f;
            Service.Mixer.SetFloat(exposedParam, db);
        }

        void ApplyPersistedVolumes()
        {
            if (Service.MasterVolume != null) Service.MasterVolume.SetCurrentValue(PlayerPrefs.GetFloat(PrefsMaster, Service.MasterVolume.CurrentValue));
            if (Service.MusicVolume  != null) Service.MusicVolume.SetCurrentValue(PlayerPrefs.GetFloat(PrefsMusic,  Service.MusicVolume.CurrentValue));
            if (Service.SfxVolume    != null) Service.SfxVolume.SetCurrentValue(PlayerPrefs.GetFloat(PrefsSfx,      Service.SfxVolume.CurrentValue));
        }
    }
}
