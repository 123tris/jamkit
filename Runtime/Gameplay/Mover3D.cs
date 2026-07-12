using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Prototype-grade 3D character mover. Reads from <see cref="InputServiceSO"/>'s Move + Jump.
    /// Camera-relative movement when <see cref="CameraReference"/> is assigned (or Camera.main exists).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Mover3D : MonoBehaviour
    {
        [Header("Service")]
        [Required] public InputServiceSO InputService;

        [Header("Tuning")]
        public float MoveSpeed = 5f;
        public float JumpSpeed = 6f;
        public bool RotateToFaceMove = true;
        public Transform CameraReference;
        public LayerMask GroundLayers = ~0;
        public float GroundCheckDistance = 0.2f;

        Rigidbody _rb;
        Collider _col;
        bool _wantsJump;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _col = GetComponent<Collider>();
            _rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            if (CameraReference == null && Camera.main != null) CameraReference = Camera.main.transform;
        }

        void Update()
        {
            if (InputService == null) return;
            var jump = InputService.Jump;
            if (jump != null && jump.WasPressedThisFrame() && IsGrounded())
                _wantsJump = true;
        }

        void FixedUpdate()
        {
            Vector2 input = (InputService == null || InputService.Move == null) ? Vector2.zero : InputService.Move.ReadValue<Vector2>();
            Vector3 dir = new(input.x, 0f, input.y);
            if (CameraReference != null && dir.sqrMagnitude > 0f)
            {
                var fwd = CameraReference.forward; fwd.y = 0f; fwd.Normalize();
                var right = CameraReference.right; right.y = 0f; right.Normalize();
                dir = right * input.x + fwd * input.y;
            }
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            var v = _rb.linearVelocity;
            v.x = dir.x * MoveSpeed;
            v.z = dir.z * MoveSpeed;
            if (_wantsJump) { v.y = JumpSpeed; _wantsJump = false; }
            _rb.linearVelocity = v;

            if (RotateToFaceMove && dir.sqrMagnitude > 0.001f)
            {
                var rot = Quaternion.LookRotation(dir, Vector3.up);
                _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, rot, 0.2f));
            }
        }

        bool IsGrounded()
        {
            if (_col == null) return false;
            var b = _col.bounds;
            var origin = new Vector3(b.center.x, b.min.y + 0.01f, b.center.z);
            return Physics.Raycast(origin, Vector3.down, GroundCheckDistance, GroundLayers);
        }
    }
}
