using UnityEngine;
using UnityEngine.InputSystem;

namespace Metz.JamKit
{
    /// <summary>
    /// Points a pivot at the mouse cursor or the gamepad Look stick — twin-stick shooters,
    /// turrets, aimed throws. Mouse: cursor is projected into the world (XY plane for 2D,
    /// the pivot's ground plane for XZ). Gamepad: any meaningful Look-stick deflection takes
    /// over. XY rotates the pivot's right axis to the aim (matches <see cref="ProjectileShooter"/>'s
    /// 2D convention); XZ rotates forward (matches 3D).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Aimer : MonoBehaviour
    {
        public enum AimPlane { XY, XZ }

        [Header("Service")]
        public InputServiceSO InputService;

        [Header("Aim")]
        public AimPlane Plane = AimPlane.XY;
        [Tooltip("Transform to rotate (weapon pivot / turret head). Null = this transform.")]
        public Transform Pivot;
        [Tooltip("Degrees per second. 0 = snap instantly.")]
        [Min(0f)] public float TurnSpeed = 0f;
        [Tooltip("Camera used to project the mouse. Null = Camera.main (cached).")]
        public Camera Cam;

        Vector3 _aimDir = Vector3.right;

        public Vector3 AimDirection => _aimDir;

        void OnEnable()
        {
            if (Cam == null) Cam = Camera.main;
        }

        void Update()
        {
            var pivot = Pivot != null ? Pivot : transform;

            // Gamepad stick wins while deflected; otherwise fall back to the mouse cursor.
            Vector2 stick = (InputService != null && InputService.Look != null)
                ? InputService.Look.ReadValue<Vector2>()
                : Vector2.zero;

            if (stick.sqrMagnitude > 0.04f)
            {
                _aimDir = Plane == AimPlane.XY
                    ? new Vector3(stick.x, stick.y, 0f).normalized
                    : new Vector3(stick.x, 0f, stick.y).normalized;
            }
            else if (Mouse.current != null && Cam != null)
            {
                Vector2 mouse = Mouse.current.position.ReadValue();
                if (Plane == AimPlane.XY)
                {
                    Vector3 world = Cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y,
                        Mathf.Abs(Cam.transform.position.z - pivot.position.z)));
                    Vector3 d = world - pivot.position;
                    d.z = 0f;
                    if (d.sqrMagnitude > 0.0001f) _aimDir = d.normalized;
                }
                else
                {
                    var ray = Cam.ScreenPointToRay(mouse);
                    var ground = new Plane(Vector3.up, pivot.position);
                    if (ground.Raycast(ray, out float enter))
                    {
                        Vector3 d = ray.GetPoint(enter) - pivot.position;
                        d.y = 0f;
                        if (d.sqrMagnitude > 0.0001f) _aimDir = d.normalized;
                    }
                }
            }

            Quaternion target = Plane == AimPlane.XY
                ? Quaternion.Euler(0f, 0f, Mathf.Atan2(_aimDir.y, _aimDir.x) * Mathf.Rad2Deg)
                : Quaternion.LookRotation(_aimDir, Vector3.up);

            pivot.rotation = TurnSpeed <= 0f
                ? target
                : Quaternion.RotateTowards(pivot.rotation, target, TurnSpeed * Time.deltaTime);
        }
    }
}
