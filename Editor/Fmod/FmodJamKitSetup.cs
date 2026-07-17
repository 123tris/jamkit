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
            JamKitDoctorWindow.ExtraScans.Add(Scan);
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
        /// The FMOD scaffold: an FmodAudioServiceSO wired to the project's Ripple volume
        /// variables, an FmodAudioServiceRunner on the JamKitCore prefab, and FmodMenuSounds on
        /// the menu prefab. FMOD-first: the 0.9 wizard scaffolds no Unity-audio service when FMOD
        /// is installed, so the variables are loaded from the wizard's Variables folder directly
        /// (falling back to a Unity service's, for projects scaffolded before FMOD).
        /// </summary>
        public static void AddFmodToProject()
        {
            var service = AssetDatabase.LoadAssetAtPath<FmodAudioServiceSO>(ServiceAssetPath);
            if (service == null)
            {
                service = ScriptableObject.CreateInstance<FmodAudioServiceSO>();
                service.MasterVolume = FindVolumeVariable("MasterVolume");
                service.MusicVolume = FindVolumeVariable("MusicVolume");
                service.SfxVolume = FindVolumeVariable("SfxVolume");
                Directory.CreateDirectory(Path.GetDirectoryName(ServiceAssetPath));
                AssetDatabase.CreateAsset(service, ServiceAssetPath);
            }

            AddToPrefab<FmodAudioServiceRunner>(JamProjectWizard.CorePrefabPath, r => r.Service = service);
            AddToPrefab<FmodMenuSounds>(JamProjectWizard.MenuPrefabPath, m => m.AudioService = service);
        }

        static Ripple.FloatVariableSO FindVolumeVariable(string name)
        {
            var direct = AssetDatabase.LoadAssetAtPath<Ripple.FloatVariableSO>($"Assets/_Project/Variables/{name}.asset");
            if (direct != null) return direct;
            var unityAudio = FindSingle<AudioServiceSO>();
            if (unityAudio == null) return null;
            return name switch
            {
                "MasterVolume" => unityAudio.MasterVolume,
                "MusicVolume" => unityAudio.MusicVolume,
                _ => unityAudio.SfxVolume,
            };
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

        static void Scan(JamKitDoctorWindow.IssueReporter report)
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
                    "FMOD is installed but there is no FmodAudioServiceSO — JamKit has no audio backend on the FMOD-first scaffold.",
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
