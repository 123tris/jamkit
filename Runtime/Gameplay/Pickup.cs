using Ripple;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Trigger-based collectible. When something on <see cref="CollectorLayers"/> enters its trigger,
    /// it optionally awards score, raises a Ripple event (wire to a sound / Feel via UltEvents),
    /// and despawns. Works for both 2D and 3D triggers — only the matching collider type fires.
    /// Requires a trigger Collider/Collider2D on this object.
    /// </summary>
    public sealed class Pickup : MonoBehaviour
    {
        [Header("Service")]
        [Tooltip("Pool to return to on pickup. If null, the object is destroyed.")]
        public PoolServiceSO PoolService;

        [Header("Collect")]
        [Tooltip("Only objects with this tag can collect. Empty = any tag. Defaults to Unity's built-in 'Player' tag so enemies/projectiles on the same layer don't hoover up pickups.")]
        public string RequiredTag = "Player";
        public LayerMask CollectorLayers = ~0;

        [Header("Score (optional)")]
        public ScoreServiceSO ScoreService;
        public int ScoreValue = 0;

        [Header("Events (Ripple)")]
        [Tooltip("Raised when collected. Wire to AudioServiceSO.PlaySfx() or a Feel MMF_Player via UltEvents.")]
        public VoidEventSO OnCollected;

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
            if (ScoreService != null && ScoreValue != 0) ScoreService.Add(ScoreValue);
            if (OnCollected != null) OnCollected.Invoke();

            if (PoolService != null) PoolService.Despawn(gameObject);
            else Destroy(gameObject);
        }
    }
}
