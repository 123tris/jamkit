using System;
using Unity.GraphToolkit.Editor;
using UnityEditor;

namespace Metz.JamKit.Editor.Flow
{
    [Serializable]
    [Graph(Extension)]
    public sealed class GameFlowGraph : Graph
    {
        public const string Extension = "gameflow";

        [MenuItem("Assets/Create/JamKit/Game Flow Graph")]
        static void Create() => GraphDatabase.PromptInProjectBrowserToCreateNewAsset<GameFlowGraph>("GameFlow");
    }
}
