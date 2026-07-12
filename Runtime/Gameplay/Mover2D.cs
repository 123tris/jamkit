using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Prototype-grade 2D character mover. Reads from <see cref="InputServiceSO"/>'s Move / Jump actions.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Mover2D : MonoBehaviour
    {
        [Header("Service")]
        [Required] public InputServiceSO InputService;

        [Header("Tuning")]
        public float MoveSpeed = 6f;
        public float JumpSpeed = 12f;
        public bool TopDown = false;
        [Tooltip("Per-axis input multiplier in TopDown mode. (1,0) = horizontal-only paddle, (0,1) = vertical-only paddle.")]
        public Vector2 AxisScale = Vector2.one;
        public LayerMask GroundLayers = ~0;
        public float GroundCheckDistance = 0.1f;

        Rigidbody2D _rb;
        Collider2D _col;
        bool _wantsJump;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<Collider2D>();
            if (TopDown) _rb.gravityScale = 0f;
        }

        void Update()
        {
            if (InputService == null || TopDown) return;
            var jump = InputService.Jump;
            if (jump != null && jump.WasPressedThisFrame() && IsGrounded())
                _wantsJump = true;
        }

        void FixedUpdate()
        {
            Vector2 input = (InputService == null || InputService.Move == null) ? Vector2.zero : InputService.Move.ReadValue<Vector2>();
            if (TopDown)
            {
                _rb.linearVelocity = Vector2.Scale(input, AxisScale) * MoveSpeed;
            }
            else
            {
                var v = _rb.linearVelocity;
                v.x = input.x * MoveSpeed;
                if (_wantsJump) { v.y = JumpSpeed; _wantsJump = false; }
                _rb.linearVelocity = v;
            }
        }

        bool IsGrounded()
        {
            if (_col == null) return false;
            var b = _col.bounds;
            var origin = new Vector2(b.center.x, b.min.y + 0.01f);
            return Physics2D.Raycast(origin, Vector2.down, GroundCheckDistance, GroundLayers).collider != null;
        }
    }
}
