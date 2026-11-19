using Metz.JamKit.Inspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Metz.JamKit
{
    [CreateAssetMenu(menuName = "JamKit/Services/Input Service", fileName = "InputService")]
    public sealed class InputServiceSO : ServiceSO
    {
        [Required] public InputActionAsset Actions;

        [Tooltip("The map menus use. Every other map is gameplay: on in gameplay, off while a menu takes UI input.")]
        public string UIMapName = "UI";

        [Tooltip("Lock and hide the cursor in gameplay, release it in UI.")]
        public bool ManageCursor = true;

        [ShowInInspector]
        public bool InGameplay { get; private set; }

        [Button]
        public void SwitchToGameplay() => Switch(true);

        [Button]
        public void SwitchToUI() => Switch(false);

        public override void ResetState() => InGameplay = false;

        void Switch(bool gameplay)
        {
            Actions.FindActionMap(UIMapName, true);
            foreach (InputActionMap map in Actions.actionMaps)
            {
                bool on = map.name == UIMapName ? !gameplay : gameplay;
                if (on) map.Enable();
                else map.Disable();
            }
            InGameplay = gameplay;

            if (!ManageCursor) return;
            Cursor.lockState = gameplay ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !gameplay;
        }
    }
}
