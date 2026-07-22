using System;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// A reference to a scene, picked from Build Settings at edit time instead of typed as a raw
    /// string. A bare scene-name <c>string</c> can hold "Levl1" as happily as "Level1" and only
    /// fails at run-time when the load returns null (§10.1 primitive obsession); a
    /// <see cref="SceneRef"/> is chosen from a dropdown, so a missing or misspelled scene is an
    /// edit-time problem. The <see cref="_guid"/> lets the drawer re-resolve the name if the scene
    /// asset is renamed. Implicitly converts to the scene-name string the loaders take, so it drops
    /// straight into <see cref="SceneServiceSO.LoadAsync(string)"/>.
    /// </summary>
    [Serializable]
    public struct SceneRef
    {
        [SerializeField, Tooltip("Scene name passed to the loader.")]
        string _name;

        // Asset GUID — editor-only stability across scene renames. The drawer keeps it in sync;
        // the runtime never reads it (SceneManager loads by name).
        [SerializeField, HideInInspector]
        string _guid;

        public SceneRef(string name)
        {
            _name = name;
            _guid = null;
        }

        /// <summary>The scene name to hand the loader. Empty when unset.</summary>
        public readonly string Name => _name;

        /// <summary>True when a scene has been picked.</summary>
        public readonly bool HasValue => !string.IsNullOrEmpty(_name);

        public override readonly string ToString() => _name;

        /// <summary>Drop-in for anywhere a scene-name string is expected.</summary>
        public static implicit operator string(SceneRef sceneRef) => sceneRef._name;
    }
}
