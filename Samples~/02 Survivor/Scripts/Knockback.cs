using UnityEngine;

namespace Metz.JamKit
{
    public static class Knockback
    {
        public static void Apply(Rigidbody rb, Vector3 source, float force)
        {
            if (rb == null) return;
            Vector3 dir = (rb.position - source);
            if (dir.sqrMagnitude < 1e-6f) dir = Vector3.up;
            else dir.Normalize();
            rb.AddForce(dir * force, ForceMode.Impulse);
        }

        public static void Apply(Rigidbody2D rb, Vector2 source, float force)
        {
            if (rb == null) return;
            Vector2 dir = (rb.position - source);
            if (dir.sqrMagnitude < 1e-6f) dir = Vector2.up;
            else dir.Normalize();
            rb.AddForce(dir * force, ForceMode2D.Impulse);
        }
    }
}
