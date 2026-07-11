using Ripple;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Tile-stepped movement (frogger, sokoban, snake, roguelikes). Reads the Move action,
    /// quantizes to 4-way steps of <see cref="CellSize"/>, slides between cells over
    /// <see cref="MoveDuration"/>, and refuses steps into <see cref="BlockedBy"/> colliders.
    /// Transform-based — pair with a kinematic Rigidbody(2D) if triggers need to see it.
    /// AI/code can drive it with <see cref="TryStep"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GridMover : MonoBehaviour
    {
        public enum GridPlane { XY, XZ }

        [Header("Service")]
        public InputServiceSO InputService;

        [Header("Grid")]
        public GridPlane Plane = GridPlane.XY;
        [Min(0.01f)] public float CellSize = 1f;
        [Tooltip("Seconds to slide one cell. 0 = instant snap.")]
        [Min(0f)] public float MoveDuration = 0.12f;
        [Tooltip("Delay between repeated steps while the input is held.")]
        [Min(0f)] public float RepeatDelay = 0.14f;
        [Tooltip("Snap to the grid on enable so off-grid placement can't accumulate drift.")]
        public bool SnapOnEnable = true;

        [Header("Blocking")]
        [Tooltip("Steps into these layers are refused (walls, crates).")]
        public LayerMask BlockedBy = 0;
        [Tooltip("Radius of the overlap probe at the target cell, as a fraction of CellSize.")]
        [Range(0.05f, 0.49f)] public float ProbeRadius = 0.4f;

        [Header("Events")]
        [Tooltip("Raised when a step starts — hop SFX, dust puffs.")]
        public VoidEventSO OnStep;
        public event System.Action<Vector3> Stepped; // arg: destination

        Vector3 _from, _to;
        float _moveT = -1f;
        float _nextRepeat;

        void OnEnable()
        {
            _moveT = -1f;
            if (SnapOnEnable) transform.position = Snap(transform.position);
        }

        void Update()
        {
            if (_moveT >= 0f)
            {
                _moveT += MoveDuration <= 0f ? 1f : Time.deltaTime / MoveDuration;
                if (_moveT >= 1f)
                {
                    transform.position = _to;
                    _moveT = -1f;
                }
                else
                {
                    transform.position = Vector3.Lerp(_from, _to, _moveT);
                }
                return;
            }

            if (InputService == null || InputService.Move == null) return;
            if (Time.time < _nextRepeat) return;

            Vector2 input = InputService.Move.ReadValue<Vector2>();
            if (input.sqrMagnitude < 0.25f) return;

            // 4-way: dominant axis wins.
            Vector2Int dir = Mathf.Abs(input.x) >= Mathf.Abs(input.y)
                ? new Vector2Int(input.x > 0f ? 1 : -1, 0)
                : new Vector2Int(0, input.y > 0f ? 1 : -1);

            if (TryStep(dir)) _nextRepeat = Time.time + RepeatDelay;
        }

        /// <summary>Attempt one step. Returns false while sliding or when the target cell is blocked.</summary>
        public bool TryStep(Vector2Int dir)
        {
            if (_moveT >= 0f) return false;
            Vector3 delta = Plane == GridPlane.XY
                ? new Vector3(dir.x, dir.y, 0f) * CellSize
                : new Vector3(dir.x, 0f, dir.y) * CellSize;
            Vector3 target = Snap(transform.position) + delta;

            if (IsBlocked(target)) return false;

            _from = transform.position;
            _to = target;
            _moveT = 0f;
            Stepped?.Invoke(target);
            if (OnStep != null) OnStep.Invoke();
            return true;
        }

        public bool IsMoving => _moveT >= 0f;

        bool IsBlocked(Vector3 target)
        {
            if (BlockedBy == 0) return false;
            float r = ProbeRadius * CellSize;
            return Plane == GridPlane.XY
                ? Physics2D.OverlapCircle(target, r, BlockedBy) != null
                : Physics.CheckSphere(target, r, BlockedBy);
        }

        Vector3 Snap(Vector3 p) => SnapToGrid(p, CellSize, Plane);

        /// <summary>Round a position to the nearest cell center on the given plane. Pure — covered by tests.</summary>
        public static Vector3 SnapToGrid(Vector3 p, float cellSize, GridPlane plane)
        {
            float x = Mathf.Round(p.x / cellSize) * cellSize;
            if (plane == GridPlane.XY)
                return new Vector3(x, Mathf.Round(p.y / cellSize) * cellSize, p.z);
            return new Vector3(x, p.y, Mathf.Round(p.z / cellSize) * cellSize);
        }
    }
}
