using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Metz.JamKit
{
    /// <summary>
    /// First-person look: yaws THIS transform (the body) and pitches <see cref="PitchTransform"/>
    /// (the camera) from <see cref="InputServiceSO"/>'s Look action. Mouse deltas apply per event
    /// (resolution-true); sticks apply degrees-per-second. Pair with <see cref="Mover3D"/>
    /// (RotateToFaceMove OFF — this component owns yaw) for the classic FPS rig.
    /// Cursor lock follows the input map: Gameplay locks, UI (pause/menus) releases — no extra wiring.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FirstPersonLook : MonoBehaviour
    {
        [Header("Service")]
        [Required] public InputServiceSO InputService;

        [Header("Rig")]
        [Required, Tooltip("Rotated up/down — the camera (or its parent) childed to this body. Yaw goes on this transform.")]
        public Transform PitchTransform;

        [Header("Tuning")]
        [Tooltip("Degrees per mouse-delta unit. Constant or a shared Ripple variable (a settings slider).")]
        public FloatReference MouseSensitivity = new(0.3f);
        [Tooltip("Degrees per second at full stick deflection.")]
        public FloatReference StickDegreesPerSecond = new(180f);
        public bool InvertY = false;
        [Range(0f, 89f), Tooltip("Max look up/down angle.")]
        public float PitchLimit = 85f;

        [Header("Cursor")]
        [Tooltip("Lock + hide the cursor while the Gameplay map is active; release on the UI map and on disable.")]
        public bool ManageCursor = true;

        Rigidbody _rb;
        float _pitch;
        bool _warnedRotateClash;

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")] public float Yaw => transform.eulerAngles.y;
        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")] public float Pitch => _pitch;
        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")] public bool CursorLocked => Cursor.lockState == CursorLockMode.Locked;

        void Awake()
        {
            // This component owns rotation; a free-spinning Y axis would let collisions twist the body.
            _rb = GetComponent<Rigidbody>();
            if (_rb != null) _rb.freezeRotation = true;

            if (PitchTransform != null)
                _pitch = NormalizeAngle(PitchTransform.localEulerAngles.x);
        }

        void OnEnable()
        {
            if (InputService != null)
            {
                InputService.MapSwitched += OnMapSwitched;
                OnMapSwitched(InputService.CurrentMap);
            }
        }

        void OnDisable()
        {
            if (InputService != null) InputService.MapSwitched -= OnMapSwitched;
            ApplyCursor(locked: false);
        }

        void Update()
        {
            if (InputService == null || PitchTransform == null) return;
            var look = InputService.Look;
            if (look == null) return;

            Vector2 delta = look.ReadValue<Vector2>();
            if (delta.sqrMagnitude <= 0f) { WarnIfMoverFightsYaw(); return; }

            // Mouse deltas are already per-frame movements; sticks report deflection and need dt.
            bool isMouse = look.activeControl != null && look.activeControl.device is Mouse;
            float scale = isMouse ? MouseSensitivity.Value : StickDegreesPerSecond.Value * Time.deltaTime;

            float yawDelta = delta.x * scale;
            float pitchDelta = delta.y * scale * (InvertY ? 1f : -1f);

            transform.Rotate(0f, yawDelta, 0f, Space.World);
            _pitch = Mathf.Clamp(_pitch + pitchDelta, -PitchLimit, PitchLimit);
            var e = PitchTransform.localEulerAngles;
            PitchTransform.localEulerAngles = new Vector3(_pitch, e.y, e.z);

            WarnIfMoverFightsYaw();
        }

        void OnMapSwitched(InputActionMap map)
        {
            if (InputService == null) return;
            ApplyCursor(locked: map != null && map == InputService.Gameplay);
        }

        void ApplyCursor(bool locked)
        {
            if (!ManageCursor) return;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        // A sibling Mover3D with RotateToFaceMove on silently re-aims the body every FixedUpdate —
        // the camera judders and mouse-look "doesn't work". Loud once, with the fix.
        void WarnIfMoverFightsYaw()
        {
            if (_warnedRotateClash) return;
            _warnedRotateClash = true;
            var mover = GetComponent<Mover3D>();
            if (mover != null && mover.RotateToFaceMove)
                Debug.LogWarning("[JamKit] FirstPersonLook and Mover3D.RotateToFaceMove both rotate this body — " +
                                 "look input will fight movement. Untick RotateToFaceMove on the Mover3D.", this);
        }

        static float NormalizeAngle(float degrees) => degrees > 180f ? degrees - 360f : degrees;
    }
}
