using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Metz.JamKit.Editor
{
    [InitializeOnLoad]
    static class JamKitDoctorWatch
    {
        static string lastLogged;

        static JamKitDoctorWatch()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += () => ScanAndLog(onlyChanges: true);
            EditorSceneManager.sceneSaved += _ => ScanAndLog(onlyChanges: true);
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode) JamKitDoctor.Scan();
                if (state == PlayModeStateChange.EnteredPlayMode) Log(Report());
            };
        }

        static void ScanAndLog(bool onlyChanges)
        {
            JamKitDoctor.Scan();
            string report = Report();
            if (onlyChanges && report == lastLogged) return;
            lastLogged = report;
            Log(report);
        }

        static void Log(string report)
        {
            if (report != null) Debug.LogWarning(report);
        }

        internal static string Report()
        {
            string[] problems = JamKitDoctor.LastIssues
                .Where(issue => issue.Severity != MessageType.Info)
                .Select(issue => issue.Message)
                .ToArray();
            if (problems.Length == 0) return null;
            string count = problems.Length == 1 ? "1 problem" : $"{problems.Length} problems";
            return $"[JamKit] The Doctor found {count}. JamKit > Doctor has a fix for most.\n- {string.Join("\n- ", problems)}";
        }
    }

    sealed class JamKitDoctorBeforeBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (Application.isBatchMode) return;
            JamKitDoctor.Scan();
            string problems = JamKitDoctorWatch.Report();
            if (problems != null) Debug.LogWarning(problems);
        }
    }
}
