using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Metz.JamKit
{
    [DisallowMultipleComponent]
    public sealed class Interactor : MonoBehaviour
    {
        [Required] public InputActionReference Interact;
        [Min(0.1f)] public float Radius = 1.5f;
        public LayerMask Layers = ~0;

        readonly Collider[] hits3D = new Collider[16];
        readonly Collider2D[] hits2D = new Collider2D[16];

        public Interactable CurrentTarget { get; private set; }

        public event Action<Interactable> TargetChanged;

        void OnEnable() => Interact.action.Enable();

        void OnDisable() => SetTarget(null);

        void Update()
        {
            SetTarget(FindNearest());
            if (CurrentTarget != null && Interact.action.WasPressedThisFrame()) CurrentTarget.Interact(this);
        }

        void SetTarget(Interactable target)
        {
            if (target == CurrentTarget) return;
            if (CurrentTarget != null) CurrentTarget.SetFocused(false);
            CurrentTarget = target;
            if (CurrentTarget != null) CurrentTarget.SetFocused(true);
            TargetChanged?.Invoke(CurrentTarget);
        }

        Interactable FindNearest()
        {
            Interactable nearest = null;
            float nearestDistance = float.MaxValue;

            int count3D = Physics.OverlapSphereNonAlloc(transform.position, Radius, hits3D, Layers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count3D; i++) Consider(hits3D[i]);

            var filter = new ContactFilter2D { useLayerMask = true, layerMask = Layers, useTriggers = true };
            int count2D = Physics2D.OverlapCircle(transform.position, Radius, filter, hits2D);
            for (int i = 0; i < count2D; i++) Consider(hits2D[i]);

            return nearest;

            void Consider(Component hit)
            {
                var candidate = hit.GetComponentInParent<Interactable>();
                if (candidate == null || !candidate.CanInteract) return;
                float distance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (distance >= nearestDistance) return;
                nearestDistance = distance;
                nearest = candidate;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }
}
