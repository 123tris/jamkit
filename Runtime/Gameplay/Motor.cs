using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Moves a body without the caller caring whether it is a <see cref="Rigidbody2D"/>, a
    /// <see cref="Rigidbody"/>, or a plain <see cref="Transform"/>. Resolve once (see
    /// <see cref="Motor.Resolve"/>) and cache the result, so the "which body do I have?" branch is
    /// decided at Awake instead of re-evaluated every FixedUpdate. See the object-oriented review doc
    /// (§11.6): the body type is a fixed fact about the object, so the conditional belongs at the
    /// edge (resolution), not in the per-frame core.
    /// </summary>
    internal interface IMotor
    {
        /// <summary>Physics-aware step toward <paramref name="position"/> (MovePosition on a body; direct set on a transform).</summary>
        void MoveTo(Vector3 position);

        /// <summary>Hard set to <paramref name="position"/>. Clears no velocity — call <see cref="Halt"/> first if you need that.</summary>
        void Teleport(Vector3 position);

        /// <summary>Zero linear and angular velocity. No-op on a transform-only object.</summary>
        void Halt();
    }

    /// <summary>
    /// Factory + one-shot helpers for <see cref="IMotor"/>. This is the single home of the
    /// rb2d ?? rb ?? transform branch that used to be copy-pasted across the gameplay components.
    /// </summary>
    internal static class Motor
    {
        /// <summary>
        /// Pick the motor for <paramref name="go"/>: a Rigidbody2D wins, then a Rigidbody, else the
        /// transform. Call from Awake and cache — one allocation per component lifetime.
        /// </summary>
        public static IMotor Resolve(GameObject go)
        {
            var rb2d = go.GetComponent<Rigidbody2D>();
            if (rb2d != null) return new Rigidbody2DMotor(rb2d);
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null) return new Rigidbody3DMotor(rb);
            return new TransformMotor(go.transform);
        }

        /// <summary>
        /// Set the linear velocity of a freshly spawned body along <paramref name="velocity"/> —
        /// for launch bursts and projectiles. Allocation-free (no motor object): the spawned body is
        /// a one-shot, not something we drive every frame. Silently ignores transform-only spawns.
        /// </summary>
        public static void LaunchBody(GameObject go, Vector3 velocity)
        {
            var rb2d = go.GetComponent<Rigidbody2D>();
            if (rb2d != null) { rb2d.linearVelocity = velocity; return; }
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = velocity;
        }
    }

    internal sealed class Rigidbody2DMotor : IMotor
    {
        readonly Rigidbody2D _rb;
        public Rigidbody2DMotor(Rigidbody2D rb) => _rb = rb;

        public void MoveTo(Vector3 position) => _rb.MovePosition(position);
        public void Teleport(Vector3 position) { _rb.position = position; _rb.transform.position = position; }
        public void Halt() { _rb.linearVelocity = Vector2.zero; _rb.angularVelocity = 0f; }
    }

    internal sealed class Rigidbody3DMotor : IMotor
    {
        readonly Rigidbody _rb;
        public Rigidbody3DMotor(Rigidbody rb) => _rb = rb;

        public void MoveTo(Vector3 position) => _rb.MovePosition(position);
        public void Teleport(Vector3 position) { _rb.position = position; _rb.transform.position = position; }
        public void Halt() { _rb.linearVelocity = Vector3.zero; _rb.angularVelocity = Vector3.zero; }
    }

    internal sealed class TransformMotor : IMotor
    {
        readonly Transform _tf;
        public TransformMotor(Transform tf) => _tf = tf;

        public void MoveTo(Vector3 position) => _tf.position = position;
        public void Teleport(Vector3 position) => _tf.position = position;
        public void Halt() { }
    }
}
