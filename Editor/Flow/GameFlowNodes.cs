using System;
using Ripple;
using Unity.GraphToolkit.Editor;
using UnityEditor;

namespace Metz.JamKit.Editor.Flow
{
    static class FlowPorts
    {
        public const string Enter = "Enter";
        public const string Exit = "Exit";
    }

    [Serializable]
    [UseWithGraph(typeof(GameFlowGraph))]
    public sealed class SceneNode : Node
    {
        public const string Scene = "Scene";

        protected override void OnDefineOptions(IOptionDefinitionContext context)
            => context.AddOption<SceneAsset>(Scene).Build();

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddInputPort(FlowPorts.Enter).Build();
            context.AddOutputPort(FlowPorts.Exit).Build();
        }
    }

    [Serializable]
    [UseWithGraph(typeof(GameFlowGraph))]
    public sealed class AnySceneNode : Node
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
            => context.AddOutputPort(FlowPorts.Exit).Build();
    }

    [Serializable]
    [UseWithGraph(typeof(GameFlowGraph))]
    public sealed class TransitionNode : Node
    {
        public const string On = "On";

        protected override void OnDefineOptions(IOptionDefinitionContext context)
            => context.AddOption<VoidEventSO>(On).Build();

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddInputPort(FlowPorts.Enter).Build();
            context.AddOutputPort(FlowPorts.Exit).Build();
        }
    }
}
