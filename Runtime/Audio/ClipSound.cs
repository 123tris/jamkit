using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;

namespace Metz.JamKit
{
    [CreateAssetMenu(menuName = "JamKit/Audio/Sound", fileName = "Sound")]
    public sealed class ClipSound : Sound
    {
        [Required, Tooltip("An AudioClip, or an Audio Random Container for variation.")]
        public AudioResource Resource;

        public AudioMixerGroup Output;
        [Range(0f, 1f)] public float Volume = 1f;

        public override void Play() => JamKitHost.Get<AudioPlayer>().Play(this, null);

        public override void PlayAt(Vector3 position) => JamKitHost.Get<AudioPlayer>().Play(this, position);
    }
}
