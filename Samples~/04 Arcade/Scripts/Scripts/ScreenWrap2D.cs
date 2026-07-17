using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Asteroids-style screen wrapping: leave one edge of the orthographic camera view, reappear
    /// at the opposite edge. Teleports the Rigidbody2D when present so physics doesn't see a
    /// huge sweep. Padding keeps the wrap hidden until the object is fully off-screen.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScreenWrap2D : MonoBehaviour
    {
        [Tooltip("Camera defining the visible rect. Null = Camera.main (cached on enable).")]
        public Camera Cam;
        [Tooltip("Extra world units past the edge before wrapping — roughly the object's radius.")]
        [Min(0f)] public float Padding = 0.5f;

        Rigidbody2D _rb;

        void Awake() => _rb = GetComponent<Rigidbody2D>();

        void OnEnable()
        {
            if (Cam == null) Cam = Camera.main;
        }

        void LateUpdate()
        {
            if (Cam == null || !Cam.orthographic) return;

            float halfH = Cam.orthographicSize + Padding;
            float halfW = Cam.orthographicSize * Cam.aspect + Padding;
            Vector3 c = Cam.transform.position;
            Vector3 p = transform.position;
            bool wrapped = false;

            if (p.x > c.x + halfW) { p.x = c.x - halfW; wrapped = true; }
            else if (p.x < c.x - halfW) { p.x = c.x + halfW; wrapped = true; }
            if (p.y > c.y + halfH) { p.y = c.y - halfH; wrapped = true; }
            else if (p.y < c.y - halfH) { p.y = c.y + halfH; wrapped = true; }

            if (!wrapped) return;
            if (_rb != null) _rb.position = p;
            transform.position = p;
        }
    }
}
