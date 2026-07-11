using Ripple;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Something the player can "press E" on — doors, chests, NPCs, levers. Pair with an
    /// <see cref="Interactor"/> on the player. Focus feedback needs zero UI plumbing: assign
    /// <see cref="PromptVisual"/> (a child sprite/text object, kept inactive) and the interactor
    /// toggles it. Reactions wire to <see cref="OnInteracted"/> (Ripple/UltEvents) or the C# event.
    /// </summary>
    public sealed class Interactable : MonoBehaviour
    {
        [Tooltip("Shown while an Interactor is in range — a child 'Press E' sprite or TextMesh, kept inactive by default.")]
        public GameObject PromptVisual;
        [Tooltip("Disable after the first interaction (chests, one-time levers).")]
        public bool SingleUse = false;

        [Header("Events (Ripple)")]
        public VoidEventSO OnInteracted;
        public event System.Action<Interactor> Interacted;

        public bool Used { get; private set; }

        void OnEnable()
        {
            Used = false;
            if (PromptVisual != null) PromptVisual.SetActive(false);
        }

        public bool CanInteract => isActiveAndEnabled && !(SingleUse && Used);

        internal void SetFocused(bool focused)
        {
            if (PromptVisual != null) PromptVisual.SetActive(focused && CanInteract);
        }

        internal void DoInteract(Interactor by)
        {
            if (!CanInteract) return;
            Used = true;
            Interacted?.Invoke(by);
            if (OnInteracted != null) OnInteracted.Invoke();
            if (SingleUse) SetFocused(false);
        }
    }
}
