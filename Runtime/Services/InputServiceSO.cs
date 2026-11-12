using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Metz.JamKit
{
    [CreateAssetMenu(menuName = "JamKit/Services/Input Service", fileName = "InputService")]
    public sealed class InputServiceSO : ServiceSO
    {
        [Required] public InputActionAsset Actions;
        public string GameplayMapName = "Gameplay";
        public string UIMapName = "UI";

        [Tooltip("Lock and hide the cursor in gameplay, release it in UI.")]
        public bool ManageCursor = true;

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")]
        public bool InGameplay { get; private set; }

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void SwitchToGameplay() => Switch(GameplayMapName, UIMapName, true);

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void SwitchToUI() => Switch(UIMapName, GameplayMapName, false);

        public override void ResetState() => InGameplay = false;

        void Switch(string enable, string disable, bool gameplay)
        {
            Actions.FindActionMap(disable, true).Disable();
            Actions.FindActionMap(enable, true).Enable();
            InGameplay = gameplay;

            if (!ManageCursor) return;
            Cursor.lockState = gameplay ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !gameplay;
        }
    }
}
