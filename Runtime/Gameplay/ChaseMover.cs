using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Moves straight toward a target — survivor hordes, zombies, homing pickups. Give it a
    /// <see cref="Target"/> or leave it null and it finds the nearest object with
    /// <see cref="TargetTag"/>, re-scanning on an interval (so it survives player respawns and
    /// pooled reuse). Drives a Rigidbody2D or Rigidbody when present, else the transform.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ChaseMover : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Explicit target. Null = find nearest by tag below.")]
        public Transform Target;
        public string TargetTag = "Player";
        [Tooltip("Seconds between find-by-tag scans while untargeted.")]
        [Min(0.05f)] public float RetargetInterval = 0.5f;

        [Header("Move")]
        [Min(0f)] public float Speed = 3f;
        [Tooltip("Stop when closer than this (keeps melee enemies from vibrating inside the player).")]
        [Min(0f)] public float StopDistance = 0.6f;
        [Tooltip("Rotate to face the move direction (3D: LookRotation, 2D: right axis toward target).")]
        public bool FaceTarget = true;
        [Tooltip("3D only: chase on the ground plane and let gravity own the Y axis.")]
        public bool LockY = true;

        Rigidbody2D _rb2d;
        Rigidbody _rb;
        float _nextScan;

        void Awake()
        {
            _rb2d = GetComponent<Rigidbody2D>();
            _rb = GetComponent<Rigidbody>();
        }

        void OnEnable() => _nextScan = 0f; // pooled respawn re-scans immediately

        void FixedUpdate()
        {
            if (Target == null)
            {
                if (Time.time >= _nextScan)
                {
                    Target = FindNearestByTag();
                    _nextScan = Time.time + RetargetInterval;
                }
                if (Target == null) { Halt(); return; }
            }

            Vector3 to = Target.position - transform.position;
            if (LockY && _rb != null) to.y = 0f;
            float dist = to.magnitude;
            if (dist <= StopDistance) { Halt(); return; }
            Vector3 dir = to / dist;

            if (_rb2d != null)
            {
                _rb2d.linearVelocity = (Vector2)dir * Speed;
                if (FaceTarget)
                    _rb2d.MoveRotation(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            }
            else if (_rb != null)
            {
                var v = dir * Speed;
                if (LockY) v.y = _rb.linearVelocity.y;
                _rb.linearVelocity = v;
                if (FaceTarget && dir.sqrMagnitude > 0.001f)
                {
                    var flat = LockY ? new Vector3(dir.x, 0f, dir.z) : dir;
                    if (flat.sqrMagnitude > 0.001f)
                        _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, Quaternion.LookRotation(flat, Vector3.up), 0.2f));
                }
            }
            else
            {
                transform.position += dir * (Speed * Time.fixedDeltaTime);
                if (FaceTarget) transform.right = dir;
            }
        }

        void Halt()
        {
            if (_rb2d != null) _rb2d.linearVelocity = Vector2.zero;
            else if (_rb != null)
            {
                var v = _rb.linearVelocity;
                _rb.linearVelocity = LockY ? new Vector3(0f, v.y, 0f) : Vector3.zero;
            }
        }

        Transform FindNearestByTag()
        {
            if (string.IsNullOrEmpty(TargetTag)) return null;
            var candidates = GameObject.FindGameObjectsWithTag(TargetTag);
            Transform best = null;
            float bestSqr = float.MaxValue;
            foreach (var go in candidates)
            {
                float sqr = (go.transform.position - transform.position).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = go.transform; }
            }
            return best;
        }
    }
}
