using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    [DisallowMultipleComponent]
    public sealed class Respawner : MonoBehaviour
    {
        [Tooltip("Empty respawns where this object was at Awake.")]
        public Transform Checkpoint;

        public FloatReference Delay = new(0.6f);

        [FoldoutGroup("Events")] public Signal OnRespawned = new();

        Body body;
        Vector3 startPosition;
        Quaternion startRotation;
        Cooldown delay = new(unscaled: true);
        bool pending;

        void Awake()
        {
            body = new Body(gameObject);
            startPosition = transform.position;
            startRotation = transform.rotation;
        }

        void OnEnable() => pending = false;

        public void RespawnAfterDelay()
        {
            if (pending) return;
            pending = true;
            delay.Start(Delay.Value);
        }

        void Update()
        {
            if (pending && delay.Ready) Respawn();
        }

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void Respawn()
        {
            pending = false;
            body.Teleport(Checkpoint != null ? Checkpoint.position : startPosition);
            transform.rotation = Checkpoint != null ? Checkpoint.rotation : startRotation;
            OnRespawned.Invoke();
        }

        public void SetCheckpoint(Transform checkpoint) => Checkpoint = checkpoint;
    }
}
