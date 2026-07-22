using System;
using Ripple;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Constant-speed movement along waypoints defined as offsets from the spawn position —
    /// frogger cars and logs (<see cref="TeleportToStart"/> makes a conveyor), moving platforms
    /// and elevators (PingPong), patrol loops. Offsets-from-start means one prefab works anywhere
    /// you drop or spawn it. Uses Rigidbody(2D).MovePosition when present (make it kinematic) so
    /// platforms and triggers behave; falls back to the transform.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PatrolMover : MonoBehaviour, IPoolable
    {
        /// <summary>
        /// What happens when the path runs out of waypoints. A strategy object, not an enum + switch,
        /// so a new end behavior is a new class rather than another branch (§11.5, replace conditional
        /// with polymorphism). Pick one from the inspector — <c>[SerializeReference]</c> gives a type
        /// dropdown.
        /// </summary>
        [Serializable]
        public abstract class PathEndBehavior
        {
            /// <summary>
            /// The next candidate index has run off the end of the path. Return the index to head to
            /// next; set <paramref name="stop"/> true to halt. Use <paramref name="mover"/> for the
            /// moves a behavior needs (flip direction, teleport to start).
            /// </summary>
            public abstract int OnPathEnd(PatrolMover mover, int index, int last, out bool stop);
        }

        /// <summary>Reverse at each end — moving platforms, elevators, back-and-forth patrols.</summary>
        [Serializable]
        public sealed class PingPong : PathEndBehavior
        {
            public override int OnPathEnd(PatrolMover mover, int index, int last, out bool stop)
            {
                stop = false;
                mover._direction = -mover._direction;
                return index + mover._direction;
            }
        }

        /// <summary>Snap back to waypoint 0 and run the path again.</summary>
        [Serializable]
        public sealed class Loop : PathEndBehavior
        {
            public override int OnPathEnd(PatrolMover mover, int index, int last, out bool stop)
            {
                stop = false;
                return 0;
            }
        }

        /// <summary>Halt at the far end and stay there.</summary>
        [Serializable]
        public sealed class Stop : PathEndBehavior
        {
            public override int OnPathEnd(PatrolMover mover, int index, int last, out bool stop)
            {
                stop = true;
                return index;
            }
        }

        /// <summary>Teleport back to the start and continue — a conveyor (frogger logs, scrolling hazards).</summary>
        [Serializable]
        public sealed class TeleportToStart : PathEndBehavior
        {
            public override int OnPathEnd(PatrolMover mover, int index, int last, out bool stop)
            {
                stop = false;
                mover.Teleport(mover._start);
                return last >= 1 ? 1 : 0;
            }
        }

        [Header("Path")]
        [Tooltip("Waypoints as offsets from the position at enable/spawn. The start itself is waypoint 0.")]
        public Vector3[] PathOffsets = { new(4f, 0f, 0f) };
        [Tooltip("Travel speed. Constant or a shared Ripple variable.")]
        public FloatReference Speed = new(2f);
        [SerializeReference, Tooltip("What happens at the end of the path.")]
        public PathEndBehavior Mode = new PingPong();
        [Tooltip("Seconds to wait at each waypoint. Constant or a shared Ripple variable.")]
        public FloatReference WaitAtPoints = new(0f);

        IMotor _motor;
        Vector3 _start;
        int _index;      // waypoint we are heading to (0 = start)
        int _direction = 1;
        float _waitUntil;
        bool _stopped;

        void Awake()
        {
            _motor = Motor.Resolve(gameObject);
            Mode ??= new PingPong();   // legacy enum data / empty reference → the old default
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
            if (_stopped || LastIndex == 0 || Speed.Value <= 0f) return;
            if (Time.time < _waitUntil) return;

            Vector3 target = Waypoint(_index);
            Vector3 pos = transform.position;
            Vector3 next = Vector3.MoveTowards(pos, target, Speed.Value * Time.fixedDeltaTime);
            Move(next);

            if ((next - target).sqrMagnitude > 0.0001f) return;

            // Arrived — the strategy decides what happens at a path end.
            if (WaitAtPoints.Value > 0f) _waitUntil = Time.time + WaitAtPoints.Value;
            int last = LastIndex;
            int candidate = _index + _direction;
            if (candidate > last || candidate < 0)
            {
                candidate = Mode.OnPathEnd(this, _index, last, out bool stop);
                if (stop) { _stopped = true; return; }
            }
            _index = candidate;
        }

        void Move(Vector3 p) => _motor.MoveTo(p);

        void Teleport(Vector3 p) => _motor.Teleport(p);

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
