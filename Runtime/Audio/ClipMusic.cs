using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;

namespace Metz.JamKit
{
    [CreateAssetMenu(menuName = "JamKit/Audio/Music", fileName = "Music")]
    public sealed class ClipMusic : Music
    {
        [Required] public AudioClip Clip;
        public AudioMixerGroup Output;
        [Range(0f, 1f)] public float Volume = 1f;
        [Min(0f)] public float FadeSeconds = 0.5f;

        public override void Play() => JamKitHost.Get<AudioPlayer>().PlayMusic(this);

        public override void Stop() => JamKitHost.Get<AudioPlayer>().StopMusic(FadeSeconds);
    }
}
