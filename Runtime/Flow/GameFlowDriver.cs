using System;
using System.Collections.Generic;
using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    public sealed class GameFlowDriver : MonoBehaviour
    {
        [Required] public GameFlow Flow;
        [Required] public SceneServiceSO SceneService;

        readonly List<(VoidEventSO on, Action handler)> active = new();

        void OnEnable()
        {
            string scene = gameObject.scene.name;
            foreach (GameFlow.Transition transition in Flow.Transitions)
            {
                if (transition.From.HasValue && transition.From.Name != scene) continue;

                SceneRef target = transition.To;
                Action handler = target.HasValue ? () => SceneService.Load(target) : SceneService.Reload;
                transition.On.AddListener(handler);
                active.Add((transition.On, handler));
            }
        }

        void OnDisable()
        {
            foreach ((VoidEventSO on, Action handler) in active) on.RemoveListener(handler);
            active.Clear();
        }
    }
}
