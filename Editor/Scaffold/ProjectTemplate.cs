using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace Metz.JamKit.Editor
{
    public static class ProjectTemplate
    {
        public const string Root = "Assets/_Project";
        public const string BootstrapScene = Root + "/Scenes/Bootstrap.unity";
        public const string GameScene = Root + "/Scenes/Game.unity";
        public const string GameOverScene = Root + "/Scenes/GameOver.unity";
        public const string CorePrefab = Root + "/Prefabs/JamKitCore.prefab";
        public const string StartersFolder = Root + "/Prefabs/Starters";
        public const string ScriptsFolder = Root + "/Scripts";

        const string Meta = ".meta";

        public static readonly string[] Scenes = { BootstrapScene, GameScene, GameOverScene };

        public static string Source
            => Path.Combine(UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(ProjectTemplate).Assembly).resolvedPath, "Template~");

        public static bool IsScaffolded => Scenes.All(File.Exists) && File.Exists(CorePrefab);

        public static string StarterPath(string starter) => $"{StartersFolder}/{starter}.prefab";

        public static int Scaffold()
        {
            int copied = CopyMissing(Source, Root).Count;
            AssetDatabase.Refresh();
            RegisterScenes();
            return copied;
        }

        internal static List<string> CopyMissing(string source, string destination)
        {
            var copied = new List<string>();
            foreach (string meta in Directory.GetFiles(source, "*" + Meta, SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal))
            {
                string relative = Path.GetRelativePath(source, meta);
                relative = relative.Substring(0, relative.Length - Meta.Length);
                string from = Path.Combine(source, relative);
                string to = Path.Combine(destination, relative);

                if (!File.Exists(from))
                {
                    Directory.CreateDirectory(to);
                    if (!File.Exists(to + Meta)) Copy(meta, to + Meta);
                    continue;
                }

                if (File.Exists(to)) continue;
                Directory.CreateDirectory(Path.GetDirectoryName(to));
                Copy(from, to);
                Copy(meta, to + Meta);
                copied.Add(relative.Replace('\\', '/'));
            }
            return copied;
        }

        static void Copy(string from, string to)
        {
            File.Copy(from, to, true);
            File.SetAttributes(to, FileAttributes.Normal);
        }

        static void RegisterScenes()
        {
            EditorBuildSettings.scenes = EditorBuildSettings.scenes
                .Concat(Scenes.Where(path => EditorBuildSettings.scenes.All(scene => scene.path != path))
                    .Select(path => new EditorBuildSettingsScene(path, true)))
                .ToArray();
        }
    }
}
