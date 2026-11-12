using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Metz.JamKit
{
    internal sealed class AudioPlayer : MonoBehaviour
    {
        const int MaxVoices = 16;

        readonly List<AudioSource> voices = new();
        AudioSource music;
        Coroutine musicFade;

        public void Play(ClipSound sound, Vector3? position)
        {
            AudioSource voice = FreeVoice();
            voice.resource = sound.Resource;
            voice.outputAudioMixerGroup = sound.Output;
            voice.volume = sound.Volume;
            voice.spatialBlend = position.HasValue ? 1f : 0f;
            voice.transform.position = position ?? Vector3.zero;
            voice.Play();
        }

        public void PlayMusic(ClipMusic track)
        {
            if (music != null && music.clip == track.Clip) return;
            StopMusic(track.FadeSeconds);

            music = NewSource("Music");
            music.clip = track.Clip;
            music.outputAudioMixerGroup = track.Output;
            music.loop = true;
            music.volume = 0f;
            music.Play();
            musicFade = StartCoroutine(Fade(music, track.Volume, track.FadeSeconds, false));
        }

        public void StopMusic(float fadeSeconds)
        {
            if (music == null) return;
            if (musicFade != null) StopCoroutine(musicFade);
            StartCoroutine(Fade(music, 0f, fadeSeconds, true));
            music = null;
        }

        AudioSource FreeVoice()
        {
            foreach (AudioSource voice in voices)
                if (!voice.isPlaying) return voice;

            if (voices.Count < MaxVoices)
            {
                voices.Add(NewSource("Voice"));
                return voices[^1];
            }

            AudioSource oldest = voices[0];
            voices.RemoveAt(0);
            voices.Add(oldest);
            return oldest;
        }

        AudioSource NewSource(string sourceName)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            return source;
        }

        static IEnumerator Fade(AudioSource source, float target, float seconds, bool destroyAfter)
        {
            float from = source.volume;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(from, target, t / seconds);
                yield return null;
            }
            source.volume = target;
            if (destroyAfter) Destroy(source.gameObject);
        }
    }
}
