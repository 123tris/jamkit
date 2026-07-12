using Unity.Cinemachine;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Drop-in follow camera for 2D and 3D. Assigns Follow + LookAt on a
    /// <see cref="CinemachineCamera"/> and ensures a <see cref="CinemachineImpulseListener"/>
    /// exists so Feel's Cinemachine impulse feedbacks (or any Impulse Source) can shake the
    /// camera. For 2D, set <see cref="OrthographicSize"/> &gt; 0 to size the lens.
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera))]
    public sealed class FollowCamera : MonoBehaviour
    {
        [Tooltip("What the camera follows (and looks at, when an Aim component is present).")]
        public Transform Target;
        [Tooltip("2D: orthographic size to apply to the lens. 0 = leave the lens alone (3D).")]
        [Min(0f)] public float OrthographicSize = 0f;

        CinemachineCamera _cam;

        void Awake()
        {
            _cam = GetComponent<CinemachineCamera>();
            if (OrthographicSize > 0f) _cam.Lens.OrthographicSize = OrthographicSize;
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
