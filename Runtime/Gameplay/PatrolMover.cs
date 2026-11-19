using Ripple;
using UnityEngine;

namespace Metz.JamKit
{
    public enum PathEnd
    {
        PingPong,
        Loop,
        Stop,
        TeleportToStart,
    }

    [DisallowMultipleComponent]
    public sealed class PatrolMover : MonoBehaviour
    {
        [Tooltip("Waypoints as offsets from the position at enable.")]
        public Vector3[] PathOffsets = { new(4f, 0f, 0f) };

        public FloatReference Speed = new(2f);
        public PathEnd AtEnd = PathEnd.PingPong;
        public FloatReference WaitAtPoints = new(0f);

        Body body;
        Vector3 start;
        int index;
        int direction;
        Cooldown wait;
        bool stopped;

        void Awake() => body = new Body(gameObject);

        void OnEnable() => Restart();

        public void Restart()
        {
            start = transform.position;
            index = 1;
            direction = 1;
            wait = default;
            stopped = false;
        }

        Vector3 Waypoint(int waypoint) => waypoint == 0 ? start : start + PathOffsets[waypoint - 1];

        void FixedUpdate()
        {
            if (stopped || PathOffsets.Length == 0 || !wait.Ready) return;

            Vector3 target = Waypoint(index);
            Vector3 next = Vector3.MoveTowards(transform.position, target, Speed.Value * Time.fixedDeltaTime);
            body.MoveTo(next);
            if (next != target) return;

            wait.Start(WaitAtPoints.Value);
            Advance();
        }

        void Advance()
        {
            int next = index + direction;
            if (next >= 0 && next <= PathOffsets.Length)
            {
                index = next;
                return;
            }

            switch (AtEnd)
            {
                case PathEnd.PingPong:
                    direction = -direction;
                    index += direction;
                    break;
                case PathEnd.Loop:
                    index = 0;
                    break;
                case PathEnd.Stop:
                    stopped = true;
                    break;
                case PathEnd.TeleportToStart:
                    body.Teleport(start);
                    index = 1;
                    break;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 origin = Application.isPlaying ? start : transform.position;
            Vector3 previous = origin;
            foreach (Vector3 offset in PathOffsets)
            {
                Gizmos.DrawLine(previous, origin + offset);
                Gizmos.DrawWireSphere(origin + offset, 0.15f);
                previous = origin + offset;
            }
        }
    }
}
