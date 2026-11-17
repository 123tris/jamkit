using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Metz.JamKit.Editor
{
    public static class JamProjectWizard
    {
        [MenuItem("JamKit/New Jam Project", priority = 0)]
        public static void Run()
        {
            if (ProjectTemplate.ScaffoldProblem is string problem)
            {
                EditorUtility.DisplayDialog("JamKit: New Jam Project", problem, "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("JamKit: New Jam Project",
                    $"Copies the project template into {ProjectTemplate.Root}: the Bootstrap, Game and GameOver scenes, " +
                    "the service assets, the Ripple variables, the menu and the starter prefabs. " +
                    "Files that are already there are left as they are.\n\nProceed?",
                    "Create", "Cancel"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            int copied = ProjectTemplate.Scaffold();
            EditorSceneManager.OpenScene(ProjectTemplate.BootstrapScene);
            OfferFastPlayMode();
            Debug.Log(copied > 0
                ? $"[JamKit] Copied {copied} project template file(s) into {ProjectTemplate.Root}. " +
                  "Press Play: Start, Settings, Game (Esc pauses) and GameOver work end to end."
                : $"[JamKit] {ProjectTemplate.Root} already has every project template file, so nothing was copied. " +
                  "To get the template's copy of a file back, delete yours and run New Jam Project again.");
        }

        static void OfferFastPlayMode()
        {
            if (EditorSettings.enterPlayModeOptionsEnabled) return;
            if (!EditorUtility.DisplayDialog("JamKit: Fast Play Mode",
                    "Disable domain reload so Play starts near-instantly?\n\n" +
                    "JamKit and Ripple reset their own state each session. Your own statics must do the same.\n\n" +
                    "Change it any time in Project Settings > Editor > Enter Play Mode Settings.",
                    "Enable", "Skip"))
                return;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        }
    }
}
