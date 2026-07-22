using Ripple;
using Sirenix.OdinInspector;
using UltEvents;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Burst of speed on the Dash input — FPS dodge, platformer air-dash, top-down roll. One
    /// component for 2D and 3D: drives whichever Rigidbody is present. Dashes along the current
    /// Move input (camera-relative in 3D, like <see cref="Mover3D"/>) or facing when idle, holds
    /// that velocity for <see cref="DashSeconds"/>, then normal movers take back over.
    /// Runs after default-order scripts so its velocity write wins during the dash window.
    /// </summary>
    [DefaultExecutionOrder(50)] // after Mover2D/Mover3D (order 0) — the dash velocity must win the FixedUpdate write
    [DisallowMultipleComponent]
    public sealed class Dasher : MonoBehaviour
    {
        [Header("Service")]
        [Required] public InputServiceSO InputService;

        [Header("Tuning")]
        [Tooltip("Dash velocity. Constant or a shared Ripple variable (upgrades).")]
        public FloatReference DashSpeed = new(18f);
        [Tooltip("How long the dash velocity is held.")]
        public FloatReference DashSeconds = new(0.18f);
        [Tooltip("Seconds between dashes.")]
        public FloatReference CooldownSeconds = new(0.9f);
        [Tooltip("3D only: camera the Move input is relative to. Defaults to Camera.main.")]
        public Transform CameraReference;
        [Tooltip("3D only: keep vertical velocity during the dash (horizontal dash). Off = full-vector override.")]
        public bool PreserveVertical = true;

        [Header("Double-tap dash")]
        [Tooltip("Also dash by quickly tapping a movement direction twice, in addition to the Dash input.")]
        public bool DoubleTapToDash = true;
        [Tooltip("Longest gap between the two taps that still counts as a double-tap.")]
        public FloatReference DoubleTapWindow = new(0.28f);
        [Range(0.1f, 0.99f)]
        [Tooltip("How far a Move axis must deflect to register as a tap. Higher = harder to trigger by accident (stick drift, diagonals).")]
        public float TapThreshold = 0.5f;

        [FoldoutGroup("Events (this instance)")]
        [Tooltip("This instance dashed. Wire feedbacks here: MMF_Player.PlayFeedbacks(), trail toggles, SFX...")]
        public UltEvent OnDashed;

        [FoldoutGroup("Broadcast (Ripple, global)")]
        [Tooltip("Optional — any dash sharing this event (global SFX, cooldown HUD).")]
        public VoidEventSO BroadcastDashed;

        Rigidbody _rb;
        Rigidbody2D _rb2d;
        Vector3 _dir;
        float _dashUntil;
        Cooldown _cooldown;

        // Per-cardinal-direction held-state + last-press timestamp for double-tap detection.
        const int DirRight = 0, DirLeft = 1, DirForward = 2, DirBack = 3;
        readonly bool[] _dirHeld = new bool[4];
        readonly float[] _dirLastTap = new float[4];

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")] public bool IsDashing => Time.time < _dashUntil;
        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")] public float CooldownRemaining => _cooldown.Remaining;

        void Awake()
        {
            _rb2d = GetComponent<Rigidbody2D>();
            _rb = GetComponent<Rigidbody>();
            if (CameraReference == null && Camera.main != null) CameraReference = Camera.main.transform;
            // Far in the past so the first tap of the session can never pair with the (0f) default.
            for (int i = 0; i < _dirLastTap.Length; i++) _dirLastTap[i] = float.NegativeInfinity;
        }

        void Update()
        {
            if (InputService == null) return;
            var dash = InputService.Dash;
            if (dash != null && dash.WasPressedThisFrame()) TryDash();

            if (DoubleTapToDash) DetectDoubleTapDash();
        }

        void FixedUpdate()
        {
            if (!IsDashing) return;

            if (_rb2d != null)
            {
                _rb2d.linearVelocity = _dir * DashSpeed.Value;
            }
            else if (_rb != null)
            {
                var v = _dir * DashSpeed.Value;
                if (PreserveVertical) v.y = _rb.linearVelocity.y;
                _rb.linearVelocity = v;
            }
        }

        /// <summary>Dash if the cooldown allows it (the same path the input takes). Wire-able and debuggable.</summary>
        [Button("Dash"), DisableInEditorMode, FoldoutGroup("Debug")]
        public bool TryDash()
        {
            _cooldown.Duration = CooldownSeconds.Value;
            if (!_cooldown.TryUse()) return false;

            _dir = ComputeDirection();
            _dashUntil = Time.time + DashSeconds.Value;
            OnDashed?.Invoke();
            if (BroadcastDashed != null) BroadcastDashed.Invoke();
            return true;
        }

        Vector3 ComputeDirection()
        {
            Vector2 input = (InputService != null && InputService.Move != null)
                ? InputService.Move.ReadValue<Vector2>()
                : Vector2.zero;

            if (_rb2d != null)
            {
                // 2D: dash along the stick, or facing (+right) when idle.
                if (input.sqrMagnitude > 0.001f) return ((Vector3)input).normalized;
                return transform.right;
            }

            // 3D: camera-relative planar input (same convention as Mover3D), facing fallback.
            Vector3 dir = new(input.x, 0f, input.y);
            if (CameraReference != null && dir.sqrMagnitude > 0.001f)
            {
                var fwd = CameraReference.forward; fwd.y = 0f; fwd.Normalize();
                var right = CameraReference.right; right.y = 0f; right.Normalize();
                dir = right * input.x + fwd * input.y;
            }
            if (dir.sqrMagnitude <= 0.001f)
            {
                dir = transform.forward;
                dir.y = 0f;
            }
            return dir.normalized;
        }

        // -------------------- double-tap --------------------

        /// <summary>
        /// Watches the Move axes for a quick second tap of the same cardinal direction and dashes on
        /// it. Runs alongside the Dash input (both call <see cref="TryDash"/>, so the cooldown gates
        /// them together). The dash still aims along the current Move input, so a forward double-tap
        /// dashes forward.
        /// </summary>
        void DetectDoubleTapDash()
        {
            if (InputService.Move == null) return;
            Vector2 m = InputService.Move.ReadValue<Vector2>();
            CheckDir(DirRight,    m.x);
            CheckDir(DirLeft,    -m.x);
            CheckDir(DirForward,  m.y);
            CheckDir(DirBack,    -m.y);
        }

        // Edge-detects one direction. Hysteresis (press at TapThreshold, release at 60% of it) keeps
        // a stick resting near the line from chattering out phantom taps; holding a direction only
        // ever fires one rising edge, so a hold never dashes.
        void CheckDir(int dir, float amount)
        {
            bool held = _dirHeld[dir];
            bool pressed = held ? amount > TapThreshold * 0.6f : amount > TapThreshold;

            if (pressed && !held)
            {
                if (Time.time - _dirLastTap[dir] <= DoubleTapWindow.Value)
                {
                    TryDash();                                  // cooldown-gated
                    _dirLastTap[dir] = float.NegativeInfinity;  // consume the pair; a 3rd tap must re-arm
                }
                else
                {
                    _dirLastTap[dir] = Time.time;               // first tap — arm the window
                }
            }
            _dirHeld[dir] = pressed;
        }
    }
}
