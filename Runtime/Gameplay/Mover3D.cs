using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Metz.JamKit
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class Mover3D : MonoBehaviour
    {
        [Required] public InputActionReference Move;
        [Required] public InputActionReference Jump;

        public FloatReference MoveSpeed = new(5f);
        public FloatReference JumpSpeed = new(6f);
        public bool RotateToFaceMove = true;

        [Tooltip("Movement is relative to this transform. Empty uses the main camera.")]
        public Transform CameraReference;

        public LayerMask GroundLayers = ~0;
        public float GroundCheckDistance = 0.2f;

        Rigidbody body;
        Collider bodyCollider;
        bool wantsJump;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            bodyCollider = GetComponent<Collider>();
            body.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            if (CameraReference == null) CameraReference = Camera.main.transform;
        }

        void OnEnable()
        {
            Move.action.Enable();
            Jump.action.Enable();
        }

        void Update()
        {
            if (Jump.action.WasPressedThisFrame() && IsGrounded()) wantsJump = true;
        }

        void FixedUpdate()
        {
            Vector2 input = Move.action.ReadValue<Vector2>();
            Vector3 direction = CameraReference.right.OnGround().normalized * input.x
                                + CameraReference.forward.OnGround().normalized * input.y;
            direction = Vector3.ClampMagnitude(direction, 1f);

            Vector3 velocity = direction * MoveSpeed.Value;
            velocity.y = wantsJump ? JumpSpeed.Value : body.linearVelocity.y;
            wantsJump = false;
            body.linearVelocity = velocity;

            if (RotateToFaceMove && direction.sqrMagnitude > 0.001f)
                body.MoveRotation(Quaternion.Slerp(body.rotation, Quaternion.LookRotation(direction, Vector3.up), 0.2f));
        }

        bool IsGrounded()
        {
            Bounds bounds = bodyCollider.bounds;
            var origin = new Vector3(bounds.center.x, bounds.min.y + 0.01f, bounds.center.z);
            return Physics.Raycast(origin, Vector3.down, GroundCheckDistance, GroundLayers);
        }
    }
}
