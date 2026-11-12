using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Metz.JamKit.Editor
{
    public static class WebGLItchBuild
    {
        [MenuItem("JamKit/Build/WebGL (itch.io)", priority = 30)]
        public static void Build()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                EditorUtility.DisplayDialog("JamKit: WebGL Build",
                    "The WebGL Build Support module is not installed.\n\n" +
                    "Unity Hub > Installs > your editor > Add modules > WebGL Build Support.", "OK");
                return;
            }

            var scenes = EnabledScenePaths();
            if (scenes.Length == 0)
            {
                EditorUtility.DisplayDialog("JamKit: WebGL Build",
                    "No scenes are enabled in Build Settings. Run JamKit > New Jam Project, or add your scenes " +
                    "(File > Build Profiles), then retry.", "OK");
                return;
            }

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.runInBackground = true;

            string output = Path.Combine("Builds", "WebGL", Sanitize(PlayerSettings.productName));
            Directory.CreateDirectory(output);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[JamKit] WebGL build {report.summary.result}: {report.summary.totalErrors} error(s). See the console above.");
                return;
            }

            Debug.Log($"[JamKit] WebGL build done → {output}\n" +
                      "itch.io: zip the CONTENTS of that folder (index.html at the zip root), upload, " +
                      "tick \"This file will be played in the browser\".");
            EditorUtility.RevealInFinder(Path.Combine(output, "index.html"));
        }

        static string[] EnabledScenePaths()
        {
            var list = new System.Collections.Generic.List<string>();
            foreach (var s in EditorBuildSettings.scenes)
                if (s.enabled && File.Exists(s.path)) list.Add(s.path);
            return list.ToArray();
        }

        static string Sanitize(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return string.IsNullOrWhiteSpace(name) ? "WebGL" : name.Trim();
        }
    }
}
