using Unity.Cinemachine;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Drop-in 3D follow camera. Assigns Follow + LookAt on a <see cref="CinemachineCamera"/>,
    /// ensures a <see cref="CinemachineImpulseListener"/> exists so Feel's MMCameraShake feedback
    /// drives camera shake.
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera))]
    public sealed class CinemachineFollow3D : MonoBehaviour
    {
        public Transform Target;

        CinemachineCamera _cam;

        void Awake()
        {
            _cam = GetComponent<CinemachineCamera>();
            if (Target != null) _cam.Follow = _cam.LookAt = Target;
            if (GetComponent<CinemachineImpulseListener>() == null)
                gameObject.AddComponent<CinemachineImpulseListener>();
        }

        public void SetTarget(Transform t)
        {
            Target = t;
            if (_cam != null) _cam.Follow = _cam.LookAt = t;
        }
    }
}
