using Ripple;
using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;

namespace Metz.JamKit
{
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public sealed class CameraShake : MonoBehaviour
    {
        [Tooltip("Impulse force at strength 1.")]
        public FloatReference Force = new(1f);

        CinemachineImpulseSource source;

        void Awake() => source = GetComponent<CinemachineImpulseSource>();

        [Button, DisableInEditorMode]
        public void Shake() => Shake(1f);

        public void Shake(float strength) => source.GenerateImpulseWithForce(Force.Value * strength);
    }
}
