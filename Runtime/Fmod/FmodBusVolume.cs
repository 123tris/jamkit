using FMODUnity;
using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    public sealed class FmodBusVolume : MonoBehaviour
    {
        [Tooltip("For example bus:/Music. bus:/ is the master bus.")]
        public string BusPath = "bus:/";

        [Required, Tooltip("Linear volume from 0 to 1.")]
        public FloatVariableSO Volume;

        void OnEnable() => Volume.AddListener(Apply);

        void OnDisable() => Volume.RemoveListener(Apply);

        void Start() => Apply(Volume.CurrentValue);

        void Apply(float linear) => RuntimeManager.GetBus(BusPath).setVolume(linear);
    }
}
