using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace Metz.JamKit.Editor
{
    /// <summary>
    /// Creates a JamKitMixer asset with Master / Music / SFX groups and the
    /// MasterVol / MusicVol / SfxVol exposed parameters that <see cref="AudioServiceRunner"/> expects.
    ///
    /// Unity's AudioMixer create API is internal, so we call
    /// <c>UnityEditor.Audio.AudioMixerController.CreateMixerControllerAtPath</c> via reflection.
    /// If reflection fails (API moved/renamed in a future Unity), we log instructions for manual setup.
    /// </summary>
    public static class AudioMixerCreator
    {
        const string DefaultPath = "Assets/_Project/Audio/Resources/JamKitMixer.mixer";

        [MenuItem("JamKit/Setup/Create Audio Mixer")]
        public static void CreateMenu() => CreateAt(DefaultPath);

        public static string CreateAt(string assetPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
            if (File.Exists(assetPath))
            {
                Debug.Log($"[JamKit] AudioMixer already exists at {assetPath}.");
                return assetPath;
            }

            try
            {
                if (TryCreateMixerViaReflection(assetPath, out var mixer))
                {
                    AddGroupsAndExpose(mixer);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
                    Debug.Log($"[JamKit] Created AudioMixer at {assetPath}.");
                    return assetPath;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[JamKit] AudioMixer creation via reflection failed: {e.Message}");
            }

            Debug.LogWarning(
                "[JamKit] Could not auto-create JamKitMixer. " +
                "Please create one manually:\n" +
                "  1. Right-click in Project: Create > Audio Mixer, name it 'JamKitMixer'.\n" +
                "  2. Move it into a 'Resources' folder.\n" +
                "  3. Add child groups named 'Music' and 'SFX'.\n" +
                "  4. Expose Master/Music/SFX volume parameters as MasterVol, MusicVol, SfxVol.");
            return null;
        }

        static bool TryCreateMixerViaReflection(string path, out AudioMixer mixer)
        {
            mixer = null;
            // Find UnityEditor.Audio.AudioMixerController in the UnityEditor.dll assembly.
            var asmEditor = typeof(EditorApplication).Assembly;
            var ctrlType = asmEditor.GetType("UnityEditor.Audio.AudioMixerController");
            if (ctrlType == null) return false;

            var method = ctrlType.GetMethod(
                "CreateMixerControllerAtPath",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string) },
                null);
            if (method == null) return false;

            mixer = method.Invoke(null, new object[] { path }) as AudioMixer;
            return mixer != null;
        }

        static void AddGroupsAndExpose(AudioMixer mixer)
        {
            // Mixer is already created with a Master group. Add Music and SFX as children of Master.
            var asmEditor = typeof(EditorApplication).Assembly;
            var ctrlType = asmEditor.GetType("UnityEditor.Audio.AudioMixerController");
            if (ctrlType == null) return;

            // Cast mixer to AudioMixerController (it's actually that type at runtime).
            var addGroup = ctrlType.GetMethod("CreateNewGroup", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string), typeof(bool) }, null);
            var addChild = ctrlType.GetMethod("AddChildToParent", BindingFlags.Public | BindingFlags.Instance);
            var addToView = ctrlType.GetMethod("AddGroupToCurrentView", BindingFlags.Public | BindingFlags.Instance);
            var masterGroupProp = ctrlType.GetProperty("masterGroup", BindingFlags.Public | BindingFlags.Instance);

            if (addGroup == null || addChild == null || masterGroupProp == null) return;

            var masterGroup = masterGroupProp.GetValue(mixer);

            var music = addGroup.Invoke(mixer, new object[] { "Music", false });
            addChild.Invoke(mixer, new[] { music, masterGroup });
            addToView?.Invoke(mixer, new[] { music });

            var sfx = addGroup.Invoke(mixer, new object[] { "SFX", false });
            addChild.Invoke(mixer, new[] { sfx, masterGroup });
            addToView?.Invoke(mixer, new[] { sfx });

            // Expose the volume parameters on each group.
            ExposeVolume(ctrlType, mixer, masterGroup, "MasterVol");
            ExposeVolume(ctrlType, mixer, music, "MusicVol");
            ExposeVolume(ctrlType, mixer, sfx, "SfxVol");
        }

        static void ExposeVolume(Type ctrlType, AudioMixer mixer, object group, string name)
        {
            // group.GetGUIDForVolume() returns the parameter GUID; mixer.AddExposedParameter(guid, name).
            var groupType = group.GetType();
            var getGuid = groupType.GetMethod("GetGUIDForVolume", BindingFlags.Public | BindingFlags.Instance);
            if (getGuid == null) return;
            var guid = getGuid.Invoke(group, null);

            var addExposed = ctrlType.GetMethod("AddExposedParameter", BindingFlags.Public | BindingFlags.Instance);
            if (addExposed == null) return;
            addExposed.Invoke(mixer, new[] { guid, name });
        }
    }
}
