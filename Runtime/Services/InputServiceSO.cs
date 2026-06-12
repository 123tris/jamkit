using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Metz.JamKit
{
    /// <summary>
    /// Holds the project's <see cref="InputActionAsset"/> and exposes named action maps
    /// (UI + Gameplay) with quick switching. Components reference this SO directly —
    /// no singleton lookup. Action/map lookups are cached so per-frame access (movers reading
    /// Move/Jump in Update + FixedUpdate) doesn't do a string dictionary lookup each call.
    /// </summary>
    [CreateAssetMenu(menuName = "JamKit/Services/Input Service", fileName = "InputService")]
    public sealed class InputServiceSO : ScriptableObject
    {
        [Tooltip("InputActionAsset that contains 'UI' and 'Gameplay' maps. Drag JamKitInput here, or your own.")]
        public InputActionAsset Actions;

        public string UIMapName = "UI";
        public string GameplayMapName = "Gameplay";

        // ---- cache (rebuilt when the asset or a map name changes) ----
        InputActionAsset _cachedAsset;
        string _cachedUIName, _cachedGameplayName;
        InputActionMap _uiMap, _gameplayMap;
        InputAction _move, _look, _jump, _attack, _interact, _pause, _uiSubmit, _uiCancel, _uiNavigate;

        void EnsureCache()
        {
            if (_cachedAsset == Actions && _cachedUIName == UIMapName && _cachedGameplayName == GameplayMapName) return;
            _cachedAsset = Actions;
            _cachedUIName = UIMapName;
            _cachedGameplayName = GameplayMapName;

            _uiMap       = Actions != null ? Actions.FindActionMap(UIMapName, false) : null;
            _gameplayMap = Actions != null ? Actions.FindActionMap(GameplayMapName, false) : null;

            _move     = _gameplayMap?.FindAction("Move");
            _look     = _gameplayMap?.FindAction("Look");
            _jump     = _gameplayMap?.FindAction("Jump");
            _attack   = _gameplayMap?.FindAction("Attack");
            _interact = _gameplayMap?.FindAction("Interact");
            _pause    = _gameplayMap?.FindAction("Pause");

            _uiSubmit   = _uiMap?.FindAction("Submit");
            _uiCancel   = _uiMap?.FindAction("Cancel");
            _uiNavigate = _uiMap?.FindAction("Navigate");
        }

        public InputActionMap UI       { get { EnsureCache(); return _uiMap; } }
        public InputActionMap Gameplay { get { EnsureCache(); return _gameplayMap; } }

        public InputAction Move     { get { EnsureCache(); return _move; } }
        public InputAction Look     { get { EnsureCache(); return _look; } }
        public InputAction Jump     { get { EnsureCache(); return _jump; } }
        public InputAction Attack   { get { EnsureCache(); return _attack; } }
        public InputAction Interact { get { EnsureCache(); return _interact; } }
        public InputAction Pause    { get { EnsureCache(); return _pause; } }

        public InputAction UI_Submit   { get { EnsureCache(); return _uiSubmit; } }
        public InputAction UI_Cancel   { get { EnsureCache(); return _uiCancel; } }
        public InputAction UI_Navigate { get { EnsureCache(); return _uiNavigate; } }

        public event Action<InputActionMap> MapSwitched;
        InputActionMap _current;
        public InputActionMap CurrentMap => _current;

        public void SwitchTo(InputActionMap map)
        {
            if (map == null) return;
            if (_current != null) _current.Disable();
            _current = map;
            _current.Enable();
            MapSwitched?.Invoke(map);
        }

        public void SwitchToUI() => SwitchTo(UI);
        public void SwitchToGameplay() => SwitchTo(Gameplay);

        void OnEnable()
        {
            // Drop any state carried over from a previous play session (Domain Reload disabled).
            _current = null;
            _cachedAsset = null;
        }

        void OnDisable()
        {
            if (_current != null && Application.isPlaying) _current.Disable();
            _current = null;
        }
    }
}
