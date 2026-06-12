using Unity.Cinemachine;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Drop-in 2D follow camera. Assigns the Follow target on a <see cref="CinemachineCamera"/>,
    /// ensures a <see cref="CinemachineImpulseListener"/> exists so a Feel MMCameraShake
    /// (or a Cinemachine Impulse Source) can shake the camera.
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera))]
    public sealed class CinemachineFollow2D : MonoBehaviour
    {
        public Transform Target;
        public float OrthographicSize = 5f;

        CinemachineCamera _cam;

        void Awake()
        {
            _cam = GetComponent<CinemachineCamera>();
            _cam.Lens.OrthographicSize = OrthographicSize;
            if (Target != null) _cam.Follow = Target;
            if (GetComponent<CinemachineImpulseListener>() == null)
                gameObject.AddComponent<CinemachineImpulseListener>();
        }

        public void SetTarget(Transform t)
        {
            Target = t;
            if (_cam != null) _cam.Follow = t;
        }
    }
}
