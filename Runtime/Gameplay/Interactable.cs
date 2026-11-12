using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    public sealed class Interactable : MonoBehaviour
    {
        [Tooltip("Shown while an Interactor is in range.")]
        public GameObject PromptVisual;

        public bool SingleUse;

        [FoldoutGroup("Events")] public Signal<Interactor> OnInteracted = new();

        public bool Used { get; private set; }

        public bool CanInteract => isActiveAndEnabled && !(SingleUse && Used);

        void OnEnable()
        {
            Used = false;
            SetFocused(false);
        }

        internal void SetFocused(bool focused)
        {
            if (PromptVisual != null) PromptVisual.SetActive(focused && CanInteract);
        }

        internal void Interact(Interactor interactor)
        {
            if (!CanInteract) return;
            Used = true;
            OnInteracted.Invoke(interactor);
            if (SingleUse) SetFocused(false);
        }
    }
}
