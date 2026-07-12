using System.Collections;
using FMODUnity;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    /// <summary>
    /// FMOD hover/click sounds for a sibling <see cref="MenuController"/> — the FMOD counterpart
    /// of the controller's own AudioClip fields (leave those empty when using this). Waits for the
    /// menu's visual tree, then hooks every button; FocusIn covers gamepad/keyboard navigation.
    /// Callbacks die with the visual tree on disable, so re-enabling never stacks them.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MenuController))]
    public sealed class FmodMenuSounds : MonoBehaviour
    {
        [Header("Service")]
        public FmodAudioServiceSO AudioService;

        [Header("Sounds")]
        [Tooltip("Played when a button gains hover or keyboard/gamepad focus.")]
        public EventReference HoverSound;
        [Tooltip("Played on button click/submit.")]
        public EventReference ClickSound;

        void OnEnable()
        {
            if (AudioService == null || (HoverSound.IsNull && ClickSound.IsNull)) return;
            StartCoroutine(WireWhenReady());
        }

        // MenuController caches its root in its own OnEnable; execution order between siblings is
        // undefined, so wait for the tree instead of assuming it.
        IEnumerator WireWhenReady()
        {
            var menu = GetComponent<MenuController>();
            while (isActiveAndEnabled && menu.Root == null) yield return null;
            if (!isActiveAndEnabled) yield break;

            menu.Root.Query<Button>().ForEach(b =>
            {
                if (!HoverSound.IsNull)
                {
                    b.RegisterCallback<PointerEnterEvent>(_ => AudioService.PlaySfx(HoverSound));
                    b.RegisterCallback<FocusInEvent>(_ => AudioService.PlaySfx(HoverSound));
                }
                if (!ClickSound.IsNull)
                    b.RegisterCallback<ClickEvent>(_ => AudioService.PlaySfx(ClickSound));
            });
        }
    }
}
