using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Ripple;
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
        const string RequiredRipple = "0.3.0";

        static readonly Regex Identity = new(@"^guid: ([0-9a-f]{32})", RegexOptions.Multiline);
        static readonly Regex FolderMeta = new(@"^folderAsset: yes", RegexOptions.Multiline);
        static readonly Regex Reference = new(@"guid: ([0-9a-f]{32}), type:|guid=([0-9a-f]{32})");

        public static readonly string[] Scenes = { BootstrapScene, GameScene, GameOverScene };

        public static string Source
            => Path.Combine(UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(ProjectTemplate).Assembly).resolvedPath, "Template~");

        // The template's Signals are saved as Ripple 0.3.0 UnityEventResponse references, which an older Ripple reads as empty.
        internal static string RippleProblem
        {
            get
            {
                if (typeof(Signal).Assembly.GetType("Ripple.UnityEventResponse") != null) return null;
                string installed = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(Signal).Assembly)?.version ?? "an older one";
                return $"JamKit needs Ripple {RequiredRipple} or newer, and this project has {installed}. " +
                       $"The project template's Signals are saved in the {RequiredRipple} shape, which an older Ripple reads as empty, " +
                       "so nothing in the template would be wired. " +
                       $"Set com.metz.ripple to https://github.com/123tris/Ripple.git#v{RequiredRipple} in Packages/manifest.json.";
            }
        }

        // Why scaffolding this project would go wrong, or null.
        internal static string ScaffoldProblem => RippleProblem ?? ConflictProblem(Conflicts(Source, Root));

        public static int Scaffold()
        {
            if (ScaffoldProblem is string problem) throw new InvalidOperationException(problem);
            int copied = CopyMissing(Source, Root).Count;
            AssetDatabase.Refresh();
            RegisterScenes();
            return copied;
        }

        internal static List<string> CopyMissing(string source, string destination)
        {
            var copied = new List<string>();
            foreach (var (relative, meta) in Metas(source))
            {
                string from = Path.Combine(source, relative);
                string to = Path.Combine(destination, relative);

                if (FolderMeta.IsMatch(File.ReadAllText(meta)))
                {
                    Directory.CreateDirectory(to);
                    if (!File.Exists(to + Meta)) Copy(meta, to + Meta);
                }
                else if (File.Exists(from) && !File.Exists(to))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(to));
                    Copy(from, to);
                    Copy(meta, to + Meta);
                    copied.Add(relative.Replace('\\', '/'));
                }
            }
            return copied;
        }

        // The files the destination has under another GUID than the template's that a template file it lacks refers to.
        // A copy of that template file would refer to nothing. An older JamKit gave every file it made a GUID of its own.
        internal static List<string> Conflicts(string source, string destination)
        {
            var theirs = new Dictionary<string, string>();
            var lacking = new List<string>();
            foreach (var (relative, meta) in Metas(source))
            {
                string from = Path.Combine(source, relative);
                string to = Path.Combine(destination, relative);
                if (!File.Exists(from)) continue;
                if (!File.Exists(to)) lacking.Add(from);
                else if (File.Exists(to + Meta) && GuidOf(to + Meta) != GuidOf(meta)) theirs[GuidOf(meta)] = relative.Replace('\\', '/');
            }

            var conflicts = new SortedSet<string>(StringComparer.Ordinal);
            if (theirs.Count > 0)
                foreach (string file in lacking)
                    foreach (Match reference in Reference.Matches(File.ReadAllText(file)))
                        if (theirs.TryGetValue(reference.Groups[1].Success ? reference.Groups[1].Value : reference.Groups[2].Value, out string conflict))
                            conflicts.Add(conflict);
            return conflicts.ToList();
        }

        static string ConflictProblem(List<string> conflicts)
        {
            if (conflicts.Count == 0) return null;
            string named = string.Join(", ", conflicts.Take(3)) + (conflicts.Count > 3 ? $" and {conflicts.Count - 3} more" : "");
            return $"{Root} has its own versions of {named}, with other GUIDs than the project template's, probably because an older JamKit made them. " +
                   "Template files the project lacks refer to those files by the template's GUIDs, so copies of them would point at nothing. " +
                   "Nothing was copied.\n\n" +
                   $"To start from the template, move {Root} out of Assets and run JamKit > New Jam Project again. " +
                   "To keep a file of your own at one of those paths, move it elsewhere first, then try again.";
        }

        // Every .meta in the template, in a stable order, with the path of its file or folder relative to source.
        static IEnumerable<(string relative, string meta)> Metas(string source)
        {
            foreach (string meta in Directory.GetFiles(source, "*" + Meta, SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal))
            {
                string relative = Path.GetRelativePath(source, meta);
                yield return (relative.Substring(0, relative.Length - Meta.Length), meta);
            }
        }

        static string GuidOf(string meta) => Identity.Match(File.ReadAllText(meta)).Groups[1].Value;

        static void Copy(string from, string to)
        {
            File.Copy(from, to, true);
            File.SetAttributes(to, FileAttributes.Normal);
        }

        internal static void RegisterScenes()
        {
            EditorBuildSettingsScene[] build = EditorBuildSettings.scenes;
            EditorBuildSettings.scenes = BuildOrder(build.Select(scene => scene.path), Scenes.Where(File.Exists))
                .Select(path => build.FirstOrDefault(scene => scene.path == path) ?? new EditorBuildSettingsScene(path, true))
                .ToArray();
        }

        // A template scene the build lacks goes right after the template scene before it, so Bootstrap leads a build it
        // joins. Scenes already in the build keep their place.
        internal static List<string> BuildOrder(IEnumerable<string> build, IEnumerable<string> scenes)
        {
            var order = build.ToList();
            int next = 0;
            foreach (string scene in scenes)
            {
                int index = order.IndexOf(scene);
                if (index < 0) order.Insert(index = next, scene);
                next = index + 1;
            }
            return order;
        }
    }
}
