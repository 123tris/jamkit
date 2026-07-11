using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Player side of "press E": finds the nearest <see cref="Interactable"/> within
    /// <see cref="Radius"/>, toggles its prompt, and interacts on the Interact action.
    /// Overlap probing is allocation-free (preallocated buffers) and works for 2D and 3D
    /// colliders at once — whichever your game uses just works.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Interactor : MonoBehaviour
    {
        [Header("Service")]
        public InputServiceSO InputService;

        [Header("Probe")]
        [Min(0.1f)] public float Radius = 1.5f;
        [Tooltip("Layers searched for Interactables. Keep tight for perf on big scenes.")]
        public LayerMask Layers = ~0;

        public Interactable CurrentTarget { get; private set; }
        public event System.Action<Interactable> TargetChanged;

        readonly Collider[] _hits3D = new Collider[16];
        readonly Collider2D[] _hits2D = new Collider2D[16];
        ContactFilter2D _filter2D;

        void Awake()
        {
            _filter2D = new ContactFilter2D { useLayerMask = true, layerMask = Layers, useTriggers = true };
        }

        void Update()
        {
            var nearest = FindNearest();
            if (nearest != CurrentTarget)
            {
                if (CurrentTarget != null) CurrentTarget.SetFocused(false);
                CurrentTarget = nearest;
                if (CurrentTarget != null) CurrentTarget.SetFocused(true);
                TargetChanged?.Invoke(CurrentTarget);
            }

            if (CurrentTarget == null || InputService == null) return;
            var action = InputService.Interact;
            if (action != null && action.WasPressedThisFrame())
                CurrentTarget.DoInteract(this);
        }

        void OnDisable()
        {
            if (CurrentTarget != null) CurrentTarget.SetFocused(false);
            CurrentTarget = null;
        }

        Interactable FindNearest()
        {
            Interactable best = null;
            float bestSqr = float.MaxValue;

            int n3 = Physics.OverlapSphereNonAlloc(transform.position, Radius, _hits3D, Layers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n3; i++) Consider(_hits3D[i].GetComponentInParent<Interactable>(), ref best, ref bestSqr);

            int n2 = Physics2D.OverlapCircle(transform.position, Radius, _filter2D, _hits2D);
            for (int i = 0; i < n2; i++) Consider(_hits2D[i].GetComponentInParent<Interactable>(), ref best, ref bestSqr);

            return best;
        }

        void Consider(Interactable candidate, ref Interactable best, ref float bestSqr)
        {
            if (candidate == null || !candidate.CanInteract) return;
            float sqr = (candidate.transform.position - transform.position).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = candidate; }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }
}
