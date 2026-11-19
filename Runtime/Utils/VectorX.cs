using UnityEngine;

namespace Metz.JamKit
{
    public static class VectorX
    {
        public static Vector3 OnGround(this Vector3 vector) => new(vector.x, 0f, vector.z);
    }
}
