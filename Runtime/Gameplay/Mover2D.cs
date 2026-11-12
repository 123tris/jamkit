using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Metz.JamKit
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class Mover2D : MonoBehaviour
    {
        [Required] public InputActionReference Move;
        [Required, HideIf(nameof(TopDown))] public InputActionReference Jump;

        public FloatReference MoveSpeed = new(6f);
        [HideIf(nameof(TopDown))] public FloatReference JumpSpeed = new(12f);

        [Tooltip("Move on both axes with no gravity or jumping.")]
        public bool TopDown;

        [ShowIf(nameof(TopDown)), Tooltip("Per-axis input multiplier. (1,0) is a horizontal-only paddle.")]
        public Vector2 AxisScale = Vector2.one;

        [HideIf(nameof(TopDown))] public LayerMask GroundLayers = ~0;
        [HideIf(nameof(TopDown))] public float GroundCheckDistance = 0.1f;

        Rigidbody2D body;
        Collider2D bodyCollider;
        bool wantsJump;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            if (TopDown) body.gravityScale = 0f;
        }

        void OnEnable()
        {
            Move.action.Enable();
            if (!TopDown) Jump.action.Enable();
        }

        void Update()
        {
            if (!TopDown && Jump.action.WasPressedThisFrame() && IsGrounded()) wantsJump = true;
        }

        void FixedUpdate()
        {
            Vector2 input = Move.action.ReadValue<Vector2>();
            if (TopDown)
            {
                body.linearVelocity = Vector2.Scale(input, AxisScale) * MoveSpeed.Value;
                return;
            }

            Vector2 velocity = body.linearVelocity;
            velocity.x = input.x * MoveSpeed.Value;
            if (wantsJump) velocity.y = JumpSpeed.Value;
            wantsJump = false;
            body.linearVelocity = velocity;
        }

        bool IsGrounded()
        {
            Bounds bounds = bodyCollider.bounds;
            var origin = new Vector2(bounds.center.x, bounds.min.y - 0.01f);
            return Physics2D.Raycast(origin, Vector2.down, GroundCheckDistance, GroundLayers);
        }
    }
}
