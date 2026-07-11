using Ripple;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Teleports this object back to a checkpoint after death (or on demand) — frogger serves,
    /// platformer pits, pong ball resets. Hooks the sibling <see cref="Health"/>'s death by
    /// default and refills it on respawn; keep that Health's DestroyOnDeath off. Move the
    /// checkpoint from a <see cref="TriggerZone"/> by wiring <see cref="SetCheckpoint"/> via
    /// UltEvents, or from code.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Respawner : MonoBehaviour
    {
        [Header("Where")]
        [Tooltip("Respawn point. Null = the position/rotation this object had at Awake.")]
        public Transform Checkpoint;

        [Header("When")]
        [Tooltip("Respawn automatically when a Health on this object dies.")]
        public bool OnSiblingDeath = true;
        [Min(0f)] public float Delay = 0.6f;
        public bool RefillHealth = true;

        [Header("Events")]
        [Tooltip("Raised after the teleport — invulnerability windows, camera snaps, serve SFX.")]
        public VoidEventSO OnRespawned;
        public event System.Action Respawned;

        Health _health;
        Rigidbody2D _rb2d;
        Rigidbody _rb;
        Vector3 _startPos;
        Quaternion _startRot;
        float _due = -1f;

        void Awake()
        {
            _health = GetComponent<Health>();
            _rb2d = GetComponent<Rigidbody2D>();
            _rb = GetComponent<Rigidbody>();
            _startPos = transform.position;
            _startRot = transform.rotation;
        }

        void OnEnable()
        {
            _due = -1f;
            if (OnSiblingDeath && _health != null) _health.Died += Schedule;
        }

        void OnDisable()
        {
            if (_health != null) _health.Died -= Schedule;
        }

        void Schedule()
        {
            if (_due < 0f) _due = Time.unscaledTime + Delay;
        }

        /// <summary>Respawn after the configured delay (what death triggers).</summary>
        public void RespawnAfterDelay() => Schedule();

        void Update()
        {
            if (_due < 0f || Time.unscaledTime < _due) return;
            _due = -1f;
            Respawn();
        }

        /// <summary>Teleport to the checkpoint immediately, refill health, raise events.</summary>
        public void Respawn()
        {
            Vector3 pos = Checkpoint != null ? Checkpoint.position : _startPos;
            Quaternion rot = Checkpoint != null ? Checkpoint.rotation : _startRot;

            if (_rb2d != null)
            {
                _rb2d.linearVelocity = Vector2.zero;
                _rb2d.angularVelocity = 0f;
                _rb2d.position = pos;
            }
            else if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.position = pos;
            }
            transform.SetPositionAndRotation(pos, rot);

            if (RefillHealth && _health != null) _health.ResetFull();

            Respawned?.Invoke();
            if (OnRespawned != null) OnRespawned.Invoke();
        }

        /// <summary>Update the checkpoint — wire from a TriggerZone flag via UltEvents.</summary>
        public void SetCheckpoint(Transform t) => Checkpoint = t;
    }
}
