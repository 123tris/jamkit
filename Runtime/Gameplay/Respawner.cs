using Ripple;
using Sirenix.OdinInspector;
using UltEvents;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Teleports this object back to a checkpoint after a delay (or immediately) — frogger
    /// serves, platformer pits, pong ball resets. A pure teleporter with no Health knowledge:
    /// wire <c>Health.OnDied → RespawnAfterDelay()</c> to trigger it, and wire
    /// <c>OnRespawned → Health.ResetFull()</c> to refill — both visible in the inspector
    /// (presets pre-wire them). Move the checkpoint from a <see cref="TriggerZone"/> by wiring
    /// <see cref="SetCheckpoint"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Respawner : MonoBehaviour
    {
        [Header("Where")]
        [Tooltip("Respawn point. Null = the position/rotation this object had at Awake.")]
        public Transform Checkpoint;

        [Header("When")]
        [Tooltip("Seconds between RespawnAfterDelay() and the teleport.")]
        [Min(0f)] public float Delay = 0.6f;

        [FoldoutGroup("Events (this instance)")]
        [Tooltip("After the teleport — wire Health.ResetFull() here, plus invulnerability windows, camera snaps, serve SFX.")]
        public UltEvent OnRespawned;

        [FoldoutGroup("Broadcast (Ripple, global)")]
        [Tooltip("Optional — any respawn sharing this event (global cues, analytics).")]
        public VoidEventSO BroadcastRespawned;

        Rigidbody2D _rb2d;
        Rigidbody _rb;
        Vector3 _startPos;
        Quaternion _startRot;
        float _due = -1f;

        void Awake()
        {
            _rb2d = GetComponent<Rigidbody2D>();
            _rb = GetComponent<Rigidbody>();
            _startPos = transform.position;
            _startRot = transform.rotation;
        }

        void OnEnable() => _due = -1f;

        /// <summary>Respawn after <see cref="Delay"/> — wire Health.OnDied here.</summary>
        public void RespawnAfterDelay()
        {
            if (_due < 0f) _due = Time.unscaledTime + Delay;
        }

        void Update()
        {
            if (_due < 0f || Time.unscaledTime < _due) return;
            _due = -1f;
            Respawn();
        }

        /// <summary>Teleport to the checkpoint immediately and fire the events.</summary>
        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
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

            OnRespawned?.Invoke();
            if (BroadcastRespawned != null) BroadcastRespawned.Invoke();
        }

        /// <summary>Update the checkpoint — wire from a TriggerZone flag via UltEvents.</summary>
        public void SetCheckpoint(Transform t) => Checkpoint = t;
    }
}
