using UnityEngine;

namespace Metz.JamKit
{
    internal readonly struct Body
    {
        readonly Transform transform;
        readonly Rigidbody2D body2D;
        readonly Rigidbody body3D;

        public Body(GameObject gameObject)
        {
            transform = gameObject.transform;
            gameObject.TryGetComponent(out body2D);
            gameObject.TryGetComponent(out body3D);
        }

        public bool Is2D => body2D != null;

        public bool Is3D => body3D != null;

        public Vector3 Velocity
        {
            get => Is2D ? body2D.linearVelocity : Is3D ? body3D.linearVelocity : Vector3.zero;
            set
            {
                if (Is2D) body2D.linearVelocity = value;
                else if (Is3D) body3D.linearVelocity = value;
            }
        }

        public void MoveTo(Vector3 position)
        {
            if (Is2D) body2D.MovePosition(position);
            else if (Is3D) body3D.MovePosition(position);
            else transform.position = position;
        }

        public void RotateTo(Quaternion rotation)
        {
            if (Is2D) body2D.MoveRotation(rotation);
            else if (Is3D) body3D.MoveRotation(rotation);
            else transform.rotation = rotation;
        }

        public void Teleport(Vector3 position)
        {
            Halt();
            if (Is2D) body2D.position = position;
            if (Is3D) body3D.position = position;
            transform.position = position;
        }

        public void Halt()
        {
            if (Is2D)
            {
                body2D.linearVelocity = Vector2.zero;
                body2D.angularVelocity = 0f;
            }
            else if (Is3D)
            {
                body3D.linearVelocity = Vector3.zero;
                body3D.angularVelocity = Vector3.zero;
            }
        }
    }
}
