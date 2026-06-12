using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>Vector extension helpers.</summary>
    public static class VectorX
    {
        public static Vector3 WithX(this Vector3 v, float x) => new(x, v.y, v.z);
        public static Vector3 WithY(this Vector3 v, float y) => new(v.x, y, v.z);
        public static Vector3 WithZ(this Vector3 v, float z) => new(v.x, v.y, z);
        public static Vector2 WithX(this Vector2 v, float x) => new(x, v.y);
        public static Vector2 WithY(this Vector2 v, float y) => new(v.x, y);

        public static Vector2 XY(this Vector3 v) => new(v.x, v.y);
        public static Vector2 XZ(this Vector3 v) => new(v.x, v.z);
        public static Vector3 ToXY(this Vector2 v, float z = 0f) => new(v.x, v.y, z);
        public static Vector3 ToXZ(this Vector2 v, float y = 0f) => new(v.x, y, v.y);

        public static Vector2 Rotate(this Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        public static Vector3 OnGround(this Vector3 v) => new(v.x, 0f, v.z);

        public static Vector2 RandomInCircle(float radius) => UnityEngine.Random.insideUnitCircle * radius;
        public static Vector3 RandomInSphere(float radius) => UnityEngine.Random.insideUnitSphere * radius;
    }
}
