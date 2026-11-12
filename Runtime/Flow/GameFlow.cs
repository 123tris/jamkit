using System;
using System.Collections.Generic;
using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    [CreateAssetMenu(menuName = "JamKit/Game Flow", fileName = "GameFlow")]
    public sealed class GameFlow : ScriptableObject
    {
        [Serializable]
        public struct Transition
        {
            [Tooltip("Empty applies in every scene.")]
            public SceneRef From;

            [Required] public VoidEventSO On;

            [Tooltip("Empty reloads the current scene.")]
            public SceneRef To;
        }

        [TableList] public List<Transition> Transitions = new();
    }
}
