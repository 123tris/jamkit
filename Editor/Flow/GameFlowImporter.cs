using System.Collections.Generic;
using Ripple;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Metz.JamKit.Editor.Flow
{
    [ScriptedImporter(1, GameFlowGraph.Extension)]
    sealed class GameFlowImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext context)
        {
            var graph = GraphDatabase.LoadGraphForImporter<GameFlowGraph>(context.assetPath);
            var flow = ScriptableObject.CreateInstance<GameFlow>();

            foreach (INode node in graph.GetNodes())
            {
                if (node is not TransitionNode) continue;
                node.GetNodeOptionByName(TransitionNode.On).TryGetValue(out VoidEventSO on);
                if (on == null) continue;

                SceneRef to = SceneOf(node.GetOutputPortByName(FlowPorts.Exit).FirstConnectedPort);
                var sources = new List<IPort>();
                node.GetInputPortByName(FlowPorts.Enter).GetConnectedPorts(sources);
                foreach (IPort source in sources)
                    flow.Transitions.Add(new GameFlow.Transition { From = SceneOf(source), On = on, To = to });
            }

            context.AddObjectToAsset("flow", flow);
            context.SetMainObject(flow);
        }

        static SceneRef SceneOf(IPort port)
        {
            if (port == null || port.GetNode() is not SceneNode sceneNode) return default;
            sceneNode.GetNodeOptionByName(SceneNode.Scene).TryGetValue(out SceneAsset scene);
            return scene == null ? default : new SceneRef(scene.name);
        }
    }
}
