using Ripple;
using Sirenix.OdinInspector;
using UltEvents;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Trigger-based collectible. When something on <see cref="CollectorLayers"/> enters its
    /// trigger, it optionally adds to a score variable, fires its events, and despawns.
    /// Score is a plain Ripple variable — no service involved (see PILLARS.md).
    /// Works for both 2D and 3D triggers; requires a trigger Collider/Collider2D on this object.
    /// </summary>
    public sealed class Pickup : MonoBehaviour
    {
        [Header("Collect")]
        [Tooltip("Only objects with this tag can collect. Empty = any tag. Defaults to Unity's built-in 'Player' tag so enemies/projectiles on the same layer don't hoover up pickups.")]
        public string RequiredTag = "Player";
        public LayerMask CollectorLayers = ~0;

        [Header("Score (optional)")]
        [Tooltip("Ripple variable the value is added to (the project's Score). Null = no score.")]
        public FloatVariableSO ScoreVariable;
        [Tooltip("Amount added to ScoreVariable. Constant or a shared Ripple variable (e.g. one 'coin value' every pickup reads).")]
        public FloatReference ScoreValue = new(0f);

        [Header("Despawn")]
        [Tooltip("Pool to return to on pickup. If null, the object is destroyed.")]
        public PoolServiceSO PoolService;

        [FoldoutGroup("Events (this instance)")]
        [Tooltip("This exact pickup was collected — wire feedbacks or game logic here.")]
        public UltEvent OnCollected;

        [FoldoutGroup("Broadcast (Ripple, global)")]
        [Tooltip("Optional — raised when any pickup sharing this event is collected (global SFX, counters).")]
        public VoidEventSO BroadcastCollected;

        bool _collected;

        void OnEnable() => _collected = false;

        void OnTriggerEnter(Collider c) => TryCollect(c.gameObject);
        void OnTriggerEnter2D(Collider2D c) => TryCollect(c.gameObject);

        void TryCollect(GameObject other)
        {
            if (_collected) return;
            if (((1 << other.layer) & CollectorLayers) == 0) return;
            if (!string.IsNullOrEmpty(RequiredTag) && !other.CompareTag(RequiredTag)) return;

            _collected = true;
            if (ScoreVariable != null && ScoreValue.Value != 0f) ScoreVariable.Add(ScoreValue.Value);
            OnCollected?.Invoke();
            if (BroadcastCollected != null) BroadcastCollected.Invoke();

            if (PoolService != null) PoolService.Despawn(gameObject);
            else Destroy(gameObject);
        }
    }
}
