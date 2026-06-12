using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>Drawing helpers for OnDrawGizmos / OnDrawGizmosSelected.</summary>
    public static class GizmoX
    {
        public static void Circle(Vector3 center, float radius, Color color, int segments = 32)
        {
            var prev = Gizmos.color;
            Gizmos.color = color;
            Vector3 last = center + new Vector3(radius, 0, 0);
            for (int i = 1; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                var p = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                Gizmos.DrawLine(last, p);
                last = p;
            }
            Gizmos.color = prev;
        }

        public static void Cross(Vector3 center, float size, Color color)
        {
            var prev = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawLine(center - Vector3.right * size, center + Vector3.right * size);
            Gizmos.DrawLine(center - Vector3.up * size, center + Vector3.up * size);
            Gizmos.DrawLine(center - Vector3.forward * size, center + Vector3.forward * size);
            Gizmos.color = prev;
        }

        public static void Arrow(Vector3 from, Vector3 to, Color color, float headSize = 0.25f)
        {
            var prev = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawLine(from, to);
            var dir = (to - from).normalized;
            var right = Quaternion.Euler(0, 30, 0) * -dir;
            var left = Quaternion.Euler(0, -30, 0) * -dir;
            Gizmos.DrawLine(to, to + right * headSize);
            Gizmos.DrawLine(to, to + left * headSize);
            Gizmos.color = prev;
        }
    }
}
