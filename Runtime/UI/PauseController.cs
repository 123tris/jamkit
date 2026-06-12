using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Drop on any gameplay scene to wire the Pause input action to a <see cref="MenuController"/>.
    /// Both references are required — this component does no auto-discovery so the dependency is
    /// explicit and inspector-visible.
    /// </summary>
    public sealed class PauseController : MonoBehaviour
    {
        public MenuController Menu;
        public InputServiceSO InputService;

        void Update()
        {
            if (Menu == null || InputService == null) return;
            var pause = InputService.Pause;
            if (pause != null && pause.WasPressedThisFrame())
                Menu.HandleBack();
        }
    }
}
