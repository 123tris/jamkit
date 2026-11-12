using Ripple;
using UnityEngine;

namespace Metz.JamKit
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Bouncer2D : MonoBehaviour
    {
        [Header("Speed")]
        [Min(0.1f)] public float Speed = 8f;
        [Tooltip("Added to speed on every bounce. Classic breakout ramps ~0.1–0.3.")]
        [Min(0f)] public float SpeedGainPerBounce = 0f;
        [Min(0.1f)] public float MaxSpeed = 20f;

        [Header("Launch")]
        public bool LaunchOnEnable = true;
        [Tooltip("Initial travel direction (normalized on use).")]
        public Vector2 LaunchDirection = new(1f, 0.6f);
        [Tooltip("Random ± degrees applied to the launch direction.")]
        [Range(0f, 90f)] public float LaunchAngleJitter = 10f;

        [Header("Bounce Shaping")]
        [Tooltip("How strongly hit-offset on a Paddle component bends the outgoing direction (0 = pure reflection).")]
        [Range(0f, 1f)] public float English = 0.5f;
        [Tooltip("Minimum outgoing angle (degrees) away from both axes, so the ball never travels perfectly flat. 0 disables.")]
        [Range(0f, 40f)] public float MinAngleFromAxis = 10f;

        [Header("Events")]
        [Tooltip("Raised on every bounce — wire SFX / CameraShake / ParticleBurst here.")]
        public VoidEventSO OnBounce;
        public event System.Action<Vector2> Bounced;

        Rigidbody2D _rb;
        Vector2 _lastVelocity;
        float _speed;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        void OnEnable()
        {
            _speed = Speed;
            if (LaunchOnEnable) Launch();
        }

        public void Launch()
        {
            var dir = LaunchDirection.sqrMagnitude > 0f ? LaunchDirection.normalized : Vector2.right;
            dir = Rotate(dir, Random.Range(-LaunchAngleJitter, LaunchAngleJitter));
            Kick(dir);
        }

        public void Kick(Vector2 direction)
        {
            var dir = ClampAwayFromAxes(direction.normalized, MinAngleFromAxis);
            _rb.linearVelocity = dir * _speed;
            _lastVelocity = _rb.linearVelocity;
        }

        void FixedUpdate()
        {
            var v = _rb.linearVelocity;
            if (v.sqrMagnitude > 0.0001f)
                _rb.linearVelocity = v.normalized * _speed;
            _lastVelocity = _rb.linearVelocity;
        }

        void OnCollisionEnter2D(Collision2D c)
        {
            var contact = c.GetContact(0);
            Vector2 inDir = _lastVelocity.sqrMagnitude > 0.0001f ? _lastVelocity.normalized : -contact.normal;
            Vector2 outDir = Vector2.Reflect(inDir, contact.normal);

            var paddle = English > 0f ? c.collider.GetComponentInParent<Paddle>() : null;
            if (paddle != null)
            {
                var pt = paddle.transform;
                float halfExtent = Mathf.Max(0.0001f, c.collider.bounds.extents.magnitude);
                float offset = Vector2.Dot(contact.point - (Vector2)pt.position, (Vector2)pt.right) / halfExtent;
                outDir = (outDir + (Vector2)pt.right * (offset * English * paddle.EnglishMultiplier)).normalized;
            }

            outDir = ClampAwayFromAxes(outDir, MinAngleFromAxis);

            _speed = Mathf.Min(MaxSpeed, _speed + SpeedGainPerBounce);
            _rb.linearVelocity = outDir * _speed;
            _lastVelocity = _rb.linearVelocity;

            Bounced?.Invoke(contact.normal);
            if (OnBounce != null) OnBounce.Invoke();
        }

        public static Vector2 ClampAwayFromAxes(Vector2 dir, float minAngleDeg)
        {
            if (minAngleDeg <= 0f) return dir;
            dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
            float min = Mathf.Sin(minAngleDeg * Mathf.Deg2Rad);
            float max = Mathf.Cos(minAngleDeg * Mathf.Deg2Rad);
            if (Mathf.Abs(dir.y) < min)
                return new Vector2(max * Sign(dir.x), min * Sign(dir.y));
            if (Mathf.Abs(dir.x) < min)
                return new Vector2(min * Sign(dir.x), max * Sign(dir.y));
            return dir;
        }

        static float Sign(float v) => v < 0f ? -1f : 1f;

        static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }
}
