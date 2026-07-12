using System.IO;
using UnityEditor;
using UnityEngine;

namespace Metz.JamKit.Editor
{
    /// <summary>
    /// Wires the FMOD backend into the JamKit scaffold. Registers a post-scaffold step with the
    /// wizard (so New Jam Project / sample setup in an FMOD project come out FMOD-ready) and FMOD
    /// checks with the Validate window. Everything here is idempotent create-or-load, and also
    /// available on demand via JamKit > Setup > Add FMOD Audio Service for projects that
    /// scaffolded before FMOD was installed.
    /// Compiles only when FMOD for Unity is present (JAMKIT_FMOD, see FmodDefineSync).
    /// </summary>
    [InitializeOnLoad]
    public static class FmodJamKitSetup
    {
        const string ServiceAssetPath = JamProjectWizard.ServicesDir + "/FmodAudioService.asset";

        static FmodJamKitSetup()
        {
            JamProjectWizard.PostScaffold.Add(AddFmodToProject);
            JamKitValidateWindow.ExtraScans.Add(Scan);
        }

        [MenuItem("JamKit/Setup/Add FMOD Audio Service", priority = 40)]
        public static void AddFmodToProjectMenu()
        {
            AddFmodToProject();
            AssetDatabase.SaveAssets();
            var so = AssetDatabase.LoadAssetAtPath<FmodAudioServiceSO>(ServiceAssetPath);
            Selection.activeObject = so;
            Debug.Log("[JamKit] FMOD audio service ready. Buses expected in your Studio project's mixer: " +
                      $"'{so.MusicBusPath}' and '{so.SfxBusPath}' (rename on the asset if yours differ).");
        }

        /// <summary>
        /// The FMOD scaffold: an FmodAudioServiceSO sharing the Unity service's Ripple volume
        /// variables, an FmodAudioServiceRunner on the JamKitCore prefab, and FmodMenuSounds on
        /// the menu prefab. The Unity-audio service stays — placeholder clips keep working until
        /// banks exist, and the runners coexist peacefully.
        /// </summary>
        public static void AddFmodToProject()
        {
            var service = AssetDatabase.LoadAssetAtPath<FmodAudioServiceSO>(ServiceAssetPath);
            if (service == null)
            {
                service = ScriptableObject.CreateInstance<FmodAudioServiceSO>();
                // Share the volume variables the wizard wired into the Unity-audio service, so
                // one set of menu sliders drives whichever backend is live.
                var unityAudio = FindSingle<AudioServiceSO>();
                if (unityAudio != null)
                {
                    service.MasterVolume = unityAudio.MasterVolume;
                    service.MusicVolume = unityAudio.MusicVolume;
                    service.SfxVolume = unityAudio.SfxVolume;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(ServiceAssetPath));
                AssetDatabase.CreateAsset(service, ServiceAssetPath);
            }

            AddToPrefab<FmodAudioServiceRunner>(JamProjectWizard.CorePrefabPath, r => r.Service = service);
            AddToPrefab<FmodMenuSounds>(JamProjectWizard.MenuPrefabPath, m => m.AudioService = service);
        }

        static void AddToPrefab<T>(string prefabPath, System.Action<T> configure) where T : Component
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null) return;
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                if (root.GetComponent<T>() == null)
                {
                    configure(root.AddComponent<T>());
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static T FindSingle<T>() where T : ScriptableObject
        {
            var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            return guids.Length == 1
                ? AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }

        // ------------------------------------------------------------------ validate checks

        static void Scan(JamKitValidateWindow.IssueReporter report)
        {
            var settings = FMODUnity.Settings.Instance;
            if (settings != null && settings.HasSourceProject && string.IsNullOrEmpty(settings.SourceProjectPath))
                report(MessageType.Warning,
                    "FMOD is installed but not linked to a Studio project — no banks, no sound. " +
                    "Run the FMOD Setup Wizard (or FMOD > Edit Settings) and set the Studio Project Path.",
                    "Open FMOD Settings", () => EditorApplication.ExecuteMenuItem("FMOD/Edit Settings"));

            var service = AssetDatabase.LoadAssetAtPath<FmodAudioServiceSO>(ServiceAssetPath);
            if (service == null)
            {
                report(MessageType.Warning,
                    "FMOD is installed but there is no FmodAudioServiceSO — JamKit audio still routes through Unity audio only.",
                    "Add FMOD Service", AddFmodToProjectMenu);
                return;
            }

            if (service.MasterVolume == null || service.MusicVolume == null || service.SfxVolume == null)
                report(MessageType.Warning,
                    $"FmodAudioService '{service.name}' is missing Ripple volume variables — settings sliders won't bind.",
                    context: service);

            var core = AssetDatabase.LoadAssetAtPath<GameObject>(JamProjectWizard.CorePrefabPath);
            if (core != null && core.GetComponent<FmodAudioServiceRunner>() == null)
                report(MessageType.Warning,
                    "JamKitCore prefab has no FmodAudioServiceRunner — FMOD service calls are silent no-ops.",
                    "Add Runner", AddFmodToProject, core);
        }
    }
}
