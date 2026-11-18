using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using Debug = UnityEngine.Debug;

namespace Metz.JamKit.Editor
{
    public static class ItchPublish
    {
        const string Title = "JamKit: Publish to itch.io";

        [MenuItem("JamKit/Publish to itch.io", priority = 30)]
        public static void Publish()
        {
            JamKitSettings settings = JamKitSettings.instance;
            if (settings.BuildProfile == null && !BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                EditorUtility.DisplayDialog(Title,
                    "The WebGL Build Support module is not installed.\n\nUnity Hub > Installs > your editor > Add modules > WebGL Build Support.", "OK");
                return;
            }

            int errors = JamKitDoctor.Scan().Count(issue => issue.Severity == MessageType.Error);
            if (errors > 0)
            {
                int choice = EditorUtility.DisplayDialogComplex(Title,
                    $"The Doctor found {errors} error(s). A build with them may not work.", "Open the Doctor", "Cancel", "Publish Anyway");
                if (choice == 0) JamKitDoctorWindow.Open();
                if (choice != 2) return;
            }

            string folder = Path.Combine("Builds", "WebGL", FileName(PlayerSettings.productName));
            if (!Build(settings, folder)) return;

            string zip = folder + ".zip";
            File.Delete(zip);
            ZipFile.CreateFromDirectory(folder, zip);

            string butler = Butler(settings);
            if (!settings.ItchTarget.Contains('/') || butler == null)
            {
                Debug.Log($"[JamKit] Built {folder} and zipped it to {zip}, with index.html at the root of the zip. " +
                          "Upload the zip to itch.io and tick \"This file will be played in the browser\". " +
                          "To push it yourself next time, install butler and set your itch.io page in Project Settings > JamKit.");
                EditorUtility.RevealInFinder(zip);
                return;
            }

            Push(butler, folder, $"{settings.ItchTarget.Trim()}:{settings.ItchChannel.Trim()}");
        }

        static bool Build(JamKitSettings settings, string folder)
        {
            Directory.CreateDirectory(folder);
            BuildReport report;
            if (settings.BuildProfile != null)
            {
                report = BuildPipeline.BuildPlayer(new BuildPlayerWithProfileOptions
                {
                    buildProfile = settings.BuildProfile,
                    locationPathName = folder,
                    options = BuildOptions.None,
                });
            }
            else
            {
                string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled && File.Exists(scene.path)).Select(scene => scene.path).ToArray();
                if (scenes.Length == 0)
                {
                    EditorUtility.DisplayDialog(Title, "No scenes are enabled in Build Settings. Run JamKit > New Jam Project, or add your scenes.", "OK");
                    return false;
                }

                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
                PlayerSettings.WebGL.decompressionFallback = true;
                PlayerSettings.runInBackground = true;
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = folder,
                    target = BuildTarget.WebGL,
                    options = BuildOptions.None,
                });
            }

            if (report.summary.result == BuildResult.Succeeded) return true;
            Debug.LogError($"[JamKit] The build {report.summary.result}: {report.summary.totalErrors} error(s). See the console above.");
            return false;
        }

        static string Butler(JamKitSettings settings)
        {
            if (!string.IsNullOrWhiteSpace(settings.ButlerPath)) return File.Exists(settings.ButlerPath) ? settings.ButlerPath : null;

            string name = Environment.OSVersion.Platform == PlatformID.Win32NT ? "butler.exe" : "butler";
            return (Environment.GetEnvironmentVariable("PATH") ?? "")
                .Split(Path.PathSeparator)
                .Where(directory => directory.Length > 0)
                .Select(directory => Path.Combine(directory.Trim('"'), name))
                .FirstOrDefault(File.Exists);
        }

        static void Push(string butler, string folder, string target)
        {
            var output = new List<string>();
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(butler, $"push \"{Path.GetFullPath(folder)}\" {target}")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                },
                EnableRaisingEvents = true,
            };
            process.OutputDataReceived += (_, line) => Collect(output, line.Data);
            process.ErrorDataReceived += (_, line) => Collect(output, line.Data);
            process.Exited += (_, _) =>
            {
                process.WaitForExit();
                string tail;
                lock (output) tail = string.Join("\n", output.Skip(Math.Max(0, output.Count - 12)));
                string page = $"https://{target.Split('/')[0]}.itch.io/{target.Split('/', ':')[1]}";
                if (process.ExitCode == 0) Debug.Log($"[JamKit] Pushed {folder} to {target}. It shows on {page} once itch.io has processed it.\n{tail}");
                else Debug.LogError($"[JamKit] butler push to {target} failed (exit code {process.ExitCode}). If it asks you to log in, run 'butler login' in a terminal.\n{tail}");
                process.Dispose();
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            Debug.Log($"[JamKit] Pushing {folder} to {target} with butler. The editor stays usable; the result is logged here.");
        }

        static void Collect(List<string> output, string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            lock (output) output.Add(line);
        }

        static string FileName(string name)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars()) name = name.Replace(invalid, '_');
            return string.IsNullOrWhiteSpace(name) ? "WebGL" : name.Trim();
        }
    }
}
