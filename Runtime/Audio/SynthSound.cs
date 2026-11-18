using Metz.JamKit.Inspector;
using UnityEngine;
using UnityEngine.Audio;

namespace Metz.JamKit
{
    [CreateAssetMenu(menuName = "JamKit/Audio/Synth Sound", fileName = "SynthSound")]
    public sealed class SynthSound : Sound
    {
        [Tooltip("The kind of placeholder sound. Swap the asset for a ClipSound when the real one exists.")]
        public SynthPreset Preset = SynthPreset.Pickup;

        [Tooltip("Picks one sound of that kind. Randomize and Mutate change it while playing.")]
        public int Seed = 1;

        public AudioMixerGroup Output;
        [Range(0f, 1f)] public float Volume = 0.6f;

        AudioClip clip;
        SynthPreset clipPreset;
        int clipSeed;

        public override void Play() => JamKitHost.Get<AudioPlayer>().Play(Clip, Output, Volume, null);

        public override void PlayAt(Vector3 position) => JamKitHost.Get<AudioPlayer>().Play(Clip, Output, Volume, position);

        [Button]
        public void Mutate()
        {
            Seed++;
            Changed();
        }

        [Button]
        public void Randomize()
        {
            Seed = Random.Range(1, 1000000);
            Changed();
        }

        void Changed()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            Play();
        }

        AudioClip Clip
        {
            get
            {
                if (clip != null && clipPreset == Preset && clipSeed == Seed) return clip;
                float[] samples = Synth.Render(Preset, Seed);
                if (clip != null) Destroy(clip);
                clip = AudioClip.Create($"{name} ({Preset} {Seed})", samples.Length, 1, Synth.SampleRate, false);
                clip.SetData(samples, 0);
                clipPreset = Preset;
                clipSeed = Seed;
                return clip;
            }
        }
    }
}
