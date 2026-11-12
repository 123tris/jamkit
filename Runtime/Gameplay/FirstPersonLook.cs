using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Metz.JamKit
{
    [DisallowMultipleComponent]
    [InfoBox("Mover3D.RotateToFaceMove also rotates this body and will fight the look input. Untick it.",
        InfoMessageType.Warning, nameof(MoverFightsYaw))]
    public sealed class FirstPersonLook : MonoBehaviour
    {
        [Required] public InputActionReference Look;

        [Required, Tooltip("Pitched up and down. The camera, or its parent.")]
        public Transform PitchTransform;

        [Required, Tooltip("Yawed left and right. Use a child pivot when the body has an interpolated Rigidbody.")]
        public Transform YawTransform;

        [Tooltip("Degrees per mouse-delta unit.")]
        public FloatReference MouseSensitivity = new(0.3f);

        [Tooltip("Degrees per second at full stick deflection.")]
        public FloatReference StickDegreesPerSecond = new(180f);

        public bool InvertY;
        [Range(0f, 89f)] public float PitchLimit = 85f;

        float pitch;

        bool MoverFightsYaw => YawTransform == transform && TryGetComponent(out Mover3D mover) && mover.RotateToFaceMove;

        void Reset() => YawTransform = transform;

        void Awake()
        {
            if (TryGetComponent(out Rigidbody body)) body.freezeRotation = true;
            float angle = PitchTransform.localEulerAngles.x;
            pitch = angle > 180f ? angle - 360f : angle;
        }

        void OnEnable() => Look.action.Enable();

        void Update()
        {
            InputAction look = Look.action;
            Vector2 delta = look.ReadValue<Vector2>();
            bool isMouse = look.activeControl?.device is Mouse;
            float scale = isMouse ? MouseSensitivity.Value : StickDegreesPerSecond.Value * Time.deltaTime;

            YawTransform.Rotate(0f, delta.x * scale, 0f, Space.World);

            pitch = Mathf.Clamp(pitch + delta.y * scale * (InvertY ? 1f : -1f), -PitchLimit, PitchLimit);
            Vector3 euler = PitchTransform.localEulerAngles;
            euler.x = pitch;
            PitchTransform.localEulerAngles = euler;
        }
    }
}
