using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Metz.JamKit
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class ThrustMover2D : MonoBehaviour
    {
        [Required] public InputActionReference Move;

        [Header("Tuning")]
        [Min(0f)] public float ThrustForce = 10f;
        [Tooltip("Turn rate in degrees per second.")]
        [Min(0f)] public float TurnSpeed = 220f;
        [Min(0.1f)] public float MaxSpeed = 12f;
        [Tooltip("Linear damping. 0 = drift forever (Asteroids); 1–3 = powered flight.")]
        [Min(0f)] public float Damping = 0f;
        [Tooltip("Keep gravity for lander-style games.")]
        public bool UseGravity = false;

        Rigidbody2D _rb;

        public bool IsThrusting { get; private set; }

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (!UseGravity) _rb.gravityScale = 0f;
            _rb.linearDamping = Damping;
        }

        void FixedUpdate()
        {
            Vector2 input = Move.action.ReadValue<Vector2>();

            if (Mathf.Abs(input.x) > 0.01f)
                _rb.MoveRotation(_rb.rotation - input.x * TurnSpeed * Time.fixedDeltaTime);

            IsThrusting = input.y > 0.01f;
            if (IsThrusting)
                _rb.AddForce((Vector2)transform.up * (ThrustForce * input.y));

            if (_rb.linearVelocity.sqrMagnitude > MaxSpeed * MaxSpeed)
                _rb.linearVelocity = _rb.linearVelocity.normalized * MaxSpeed;
        }
    }
}
