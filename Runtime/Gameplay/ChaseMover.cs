using Ripple;
using UnityEngine;

namespace Metz.JamKit
{
    [DisallowMultipleComponent]
    public sealed class ChaseMover : MonoBehaviour
    {
        [Tooltip("Leave empty to chase the nearest member of Targets.")]
        public Transform Target;

        [Tooltip("Runtime set of candidates. Objects join it with a Runtime Set Member component.")]
        public GameObjectListSO Targets;

        public FloatReference Speed = new(3f);
        public FloatReference StopDistance = new(0.6f);
        public bool FaceTarget = true;

        [Tooltip("3D only: chase on the ground plane and leave the Y axis to gravity.")]
        public bool LockY = true;

        Body body;

        bool Grounded => LockY && body.Is3D;

        void Awake() => body = new Body(gameObject);

        void FixedUpdate()
        {
            if (Target == null) Target = Nearest();
            if (Target == null)
            {
                Drive(Vector3.zero);
                return;
            }

            Vector3 toTarget = Target.position - transform.position;
            if (Grounded) toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance <= StopDistance.Value)
            {
                Drive(Vector3.zero);
                return;
            }

            Vector3 direction = toTarget / distance;
            Drive(direction * Speed.Value);
            if (FaceTarget) Face(direction);
        }

        void Drive(Vector3 velocity)
        {
            if (Grounded) velocity.y = body.Velocity.y;
            if (body.Is2D || body.Is3D) body.Velocity = velocity;
            else transform.position += velocity * Time.fixedDeltaTime;
        }

        void Face(Vector3 direction)
        {
            Quaternion facing = body.Is3D
                ? Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction, Vector3.up), 0.2f)
                : Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            body.RotateTo(facing);
        }

        Transform Nearest()
        {
            if (Targets == null) return null;

            Transform nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (GameObject candidate in Targets)
            {
                float distance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (distance >= nearestDistance) continue;
                nearestDistance = distance;
                nearest = candidate.transform;
            }
            return nearest;
        }
    }
}
