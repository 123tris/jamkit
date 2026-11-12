using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;

namespace Metz.JamKit
{
    public sealed class MixerVolume : MonoBehaviour
    {
        [Required] public AudioMixer Mixer;

        [Tooltip("Exposed mixer parameter, in decibels.")]
        public string Parameter = "MasterVol";

        [Required, Tooltip("Linear volume from 0 to 1.")]
        public FloatVariableSO Volume;

        void OnEnable() => Volume.AddListener(Apply);

        void OnDisable() => Volume.RemoveListener(Apply);

        void Start() => Apply(Volume.CurrentValue);

        void Apply(float linear) => Mixer.SetFloat(Parameter, linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f);
    }
}
