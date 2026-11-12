using System;
using UnityEngine;

namespace Metz.JamKit
{
    [Serializable]
    public struct SceneRef
    {
        [SerializeField, Tooltip("Scene name passed to the loader.")]
        string _name;

        [SerializeField, HideInInspector]
        string _guid;

        public SceneRef(string name)
        {
            _name = name;
            _guid = null;
        }

        public readonly string Name => _name;

        public readonly bool HasValue => !string.IsNullOrEmpty(_name);

        public override readonly string ToString() => _name;

        public static implicit operator string(SceneRef sceneRef) => sceneRef._name;
    }
}
