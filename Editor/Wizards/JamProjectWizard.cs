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
            if (!EditorUtility.DisplayDialog("JamKit: New Jam Project",
                    $"Copies the project template into {ProjectTemplate.Root}: the Bootstrap, Game and GameOver scenes, " +
                    "the service assets, the Ripple variables, the menu and the starter prefabs. " +
                    "Files that are already there are left as they are.\n\nProceed?",
                    "Create", "Cancel"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            ProjectTemplate.Scaffold();
            EditorSceneManager.OpenScene(ProjectTemplate.BootstrapScene);
            OfferFastPlayMode();
            Debug.Log("[JamKit] Setup complete. Press Play: Start, Settings, Game (Esc pauses) and GameOver work end to end.");
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
