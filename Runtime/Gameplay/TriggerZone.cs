using Ripple;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// The universal "something entered this area" primitive: kill pit, water hazard, goal line,
    /// score gate, level exit — all the same component with different toggles. Filters by tag +
    /// layer, then (in order) damages/kills the enterer's <see cref="Health"/>, awards score,
    /// raises events, removes the enterer, and finally loads a scene. Works with 2D and 3D
    /// triggers; needs a trigger collider on this object.
    /// </summary>
    public sealed class TriggerZone : MonoBehaviour
    {
        [Header("Filter")]
        [Tooltip("Only objects with this tag trigger the zone. Empty = any tag.")]
        public string RequiredTag = "";
        public LayerMask Layers = ~0;
        [Tooltip("Fire only once, then disarm until re-enabled — for goals and one-shot triggers.")]
        public bool OneShot = false;

        [Header("Damage (optional)")]
        [Tooltip("Damage applied to the enterer's Health. 0 = none.")]
        [Min(0f)] public float Damage = 0f;
        [Tooltip("Kill the enterer's Health outright (death pits, lava).")]
        public bool Kill = false;

        [Header("Score (optional)")]
        public ScoreServiceSO ScoreService;
        public int ScoreValue = 0;

        [Header("Remove (optional)")]
        [Tooltip("Despawn/destroy the enterer (goal swallowing a ball, off-screen cleanup).")]
        public bool RemoveEnterer = false;
        [Tooltip("Pool used by RemoveEnterer. Null = Destroy.")]
        public PoolServiceSO PoolService;

        [Header("Scene (optional)")]
        [Tooltip("Loaded after everything else. Empty = stay.")]
        public SceneServiceSO SceneService;
        public string LoadScene = "";

        [Header("Events")]
        [Tooltip("Raised per qualifying enter — wire Toast / SFX / respawn logic here.")]
        public VoidEventSO OnEntered;
        public event System.Action<GameObject> Entered;

        bool _fired;

        void OnEnable() => _fired = false;

        void OnTriggerEnter(Collider c) => Handle(c.gameObject);
        void OnTriggerEnter2D(Collider2D c) => Handle(c.gameObject);

        void Handle(GameObject other)
        {
            if (OneShot && _fired) return;
            if (((1 << other.layer) & Layers) == 0) return;
            if (!string.IsNullOrEmpty(RequiredTag) && !other.CompareTag(RequiredTag)) return;
            _fired = true;

            if (Damage > 0f || Kill)
            {
                var h = other.GetComponentInParent<Health>();
                if (h != null)
                {
                    if (Kill) h.Kill();
                    else h.Damage(Damage);
                }
            }

            if (ScoreService != null && ScoreValue != 0) ScoreService.Add(ScoreValue);

            Entered?.Invoke(other);
            if (OnEntered != null) OnEntered.Invoke();

            if (RemoveEnterer)
            {
                if (PoolService != null) PoolService.Despawn(other);
                else Destroy(other);
            }

            if (SceneService != null && !string.IsNullOrEmpty(LoadScene))
                SceneService.LoadAsync(LoadScene);
        }
    }
}
