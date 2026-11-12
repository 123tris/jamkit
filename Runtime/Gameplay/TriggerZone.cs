using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    public sealed class TriggerZone : MonoBehaviour
    {
        [Tooltip("Empty accepts any tag.")]
        public string RequiredTag = "";

        public LayerMask Layers = ~0;

        [Tooltip("React to the first enter only, until re-enabled.")]
        public bool OneShot;

        public bool DestroyEnterer;
        public bool DestroySelf;

        [FoldoutGroup("Events")] public Signal<GameObject> OnEntered = new();
        [FoldoutGroup("Events")] public Signal<GameObject> OnExited = new();

        bool fired;

        void OnEnable() => fired = false;

        void OnTriggerEnter(Collider other) => Enter(other.gameObject);
        void OnTriggerEnter2D(Collider2D other) => Enter(other.gameObject);
        void OnTriggerExit(Collider other) => Exit(other.gameObject);
        void OnTriggerExit2D(Collider2D other) => Exit(other.gameObject);

        bool Accepts(GameObject other)
            => Layers.Contains(other.layer) && (RequiredTag.Length == 0 || other.CompareTag(RequiredTag));

        void Enter(GameObject other)
        {
            if ((OneShot && fired) || !Accepts(other)) return;
            fired = true;

            OnEntered.Invoke(other);
            if (DestroyEnterer) Destroy(other);
            if (DestroySelf) Destroy(gameObject);
        }

        void Exit(GameObject other)
        {
            if (Accepts(other)) OnExited.Invoke(other);
        }
    }
}
