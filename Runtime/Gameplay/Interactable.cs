using Ripple;
using Sirenix.OdinInspector;
using UltEvents;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Something the player can "press E" on — doors, chests, NPCs, levers. Pair with an
    /// <see cref="Interactor"/> on the player. Focus feedback needs zero UI plumbing: assign
    /// <see cref="PromptVisual"/> (a child sprite/text object, kept inactive) and the interactor
    /// toggles it. Reactions wire to <see cref="OnInteracted"/> (this lever opens THIS gate) or
    /// the global Ripple broadcast.
    /// </summary>
    public sealed class Interactable : MonoBehaviour
    {
        [Tooltip("Shown while an Interactor is in range — a child 'Press E' sprite or TextMesh, kept inactive by default.")]
        public GameObject PromptVisual;
        [Tooltip("Disable after the first interaction (chests, one-time levers).")]
        public bool SingleUse = false;

        [FoldoutGroup("Events (this instance)")]
        [Tooltip("This exact object was used, with the user — wire doors, loot, dialogue here.")]
        public UltEvent<Interactor> OnInteracted;

        [FoldoutGroup("Broadcast (Ripple, global)")]
        [Tooltip("Optional — any interaction sharing this event (global SFX, tutorial counters).")]
        public VoidEventSO BroadcastInteracted;

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
            OnInteracted?.Invoke(by);
            if (BroadcastInteracted != null) BroadcastInteracted.Invoke();
            if (SingleUse) SetFocused(false);
        }
    }
}
