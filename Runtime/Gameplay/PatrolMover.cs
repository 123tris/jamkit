using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Constant-speed movement along waypoints defined as offsets from the spawn position —
    /// frogger cars and logs (<see cref="EndMode.TeleportToStart"/> makes a conveyor), moving
    /// platforms and elevators (PingPong), patrol loops. Offsets-from-start means one prefab
    /// works anywhere you drop or spawn it. Uses Rigidbody(2D).MovePosition when present (make
    /// it kinematic) so platforms and triggers behave; falls back to the transform.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PatrolMover : MonoBehaviour, IPoolable
    {
        public enum EndMode { PingPong, Loop, Stop, TeleportToStart }

        [Header("Path")]
        [Tooltip("Waypoints as offsets from the position at enable/spawn. The start itself is waypoint 0.")]
        public Vector3[] PathOffsets = { new(4f, 0f, 0f) };
        [Min(0f)] public float Speed = 2f;
        public EndMode Mode = EndMode.PingPong;
        [Tooltip("Seconds to wait at each waypoint.")]
        [Min(0f)] public float WaitAtPoints = 0f;

        Rigidbody2D _rb2d;
        Rigidbody _rb;
        Vector3 _start;
        int _index;      // waypoint we are heading to (0 = start)
        int _direction = 1;
        float _waitUntil;
        bool _stopped;

        void Awake()
        {
            _rb2d = GetComponent<Rigidbody2D>();
            _rb = GetComponent<Rigidbody>();
        }

        // Capture the start at every enable, not just Awake — spawned/pooled copies begin
        // their path wherever the spawner put them.
        void OnEnable() => Restart();
        public void OnSpawn() => Restart();
        public void OnDespawn() { }

        public void Restart()
        {
            _start = transform.position;
            _index = PathOffsets != null && PathOffsets.Length > 0 ? 1 : 0;
            _direction = 1;
            _stopped = false;
            _waitUntil = 0f;
        }

        Vector3 Waypoint(int i) => i == 0 ? _start : _start + PathOffsets[i - 1];
        int LastIndex => PathOffsets == null ? 0 : PathOffsets.Length;

        void FixedUpdate()
        {
            if (_stopped || LastIndex == 0 || Speed <= 0f) return;
            if (Time.time < _waitUntil) return;

            Vector3 target = Waypoint(_index);
            Vector3 pos = transform.position;
            Vector3 next = Vector3.MoveTowards(pos, target, Speed * Time.fixedDeltaTime);
            Move(next);

            if ((next - target).sqrMagnitude > 0.0001f) return;

            // Arrived — pick the next waypoint per mode.
            if (WaitAtPoints > 0f) _waitUntil = Time.time + WaitAtPoints;
            int last = LastIndex;
            int candidate = _index + _direction;
            if (candidate > last || candidate < 0)
            {
                switch (Mode)
                {
                    case EndMode.PingPong:
                        _direction = -_direction;
                        candidate = _index + _direction;
                        break;
                    case EndMode.Loop:
                        candidate = 0;
                        break;
                    case EndMode.Stop:
                        _stopped = true;
                        return;
                    case EndMode.TeleportToStart:
                        Teleport(_start);
                        candidate = last >= 1 ? 1 : 0;
                        break;
                }
            }
            _index = candidate;
        }

        void Move(Vector3 p)
        {
            if (_rb2d != null) _rb2d.MovePosition(p);
            else if (_rb != null) _rb.MovePosition(p);
            else transform.position = p;
        }

        void Teleport(Vector3 p)
        {
            if (_rb2d != null) _rb2d.position = p;
            else if (_rb != null) _rb.position = p;
            transform.position = p;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 origin = Application.isPlaying ? _start : transform.position;
            Vector3 prev = origin;
            if (PathOffsets == null) return;
            foreach (var off in PathOffsets)
            {
                Vector3 p = origin + off;
                Gizmos.DrawLine(prev, p);
                Gizmos.DrawWireSphere(p, 0.15f);
                prev = p;
            }
        }
    }
}
