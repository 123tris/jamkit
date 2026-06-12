using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace Metz.JamKit.Editor
{
    /// <summary>
    /// Creates (or repairs) a JamKitMixer asset with Master / Music / SFX groups and the
    /// MasterVol / MusicVol / SfxVol exposed parameters that <see cref="AudioServiceRunner"/> expects.
    ///
    /// Unity's AudioMixer authoring API is internal (UnityEditor.Audio.AudioMixerController), so
    /// groups and exposure are set up via reflection. The whole routine is idempotent: re-running
    /// it on an existing mixer fills in whatever is missing, and the result is verified with
    /// <see cref="AudioMixer.GetFloat(string, out float)"/> — if any parameter is still missing we
    /// say so loudly with manual-setup instructions instead of failing silently.
    /// </summary>
    public static class AudioMixerCreator
    {
        const string DefaultPath = "Assets/_Project/Audio/Resources/JamKitMixer.mixer";
        static readonly string[] ExposedNames = { "MasterVol", "MusicVol", "SfxVol" };

        [MenuItem("JamKit/Setup/Create Audio Mixer")]
        public static void CreateMenu() => CreateAt(DefaultPath);

        public static string CreateAt(string assetPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));

            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(assetPath);
            if (mixer == null)
            {
                try
                {
                    TryCreateMixerViaReflection(assetPath, out mixer);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[JamKit] AudioMixer creation via reflection failed: {e.Message}");
                }
                if (mixer == null) { LogManualInstructions(); return null; }
            }

            try
            {
                EnsureGroupsAndExposure(mixer);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[JamKit] AudioMixer group/exposure setup failed: {e.Message}");
            }

            EditorUtility.SetDirty(mixer);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

            // Reload after import so verification runs against the saved state.
            mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(assetPath);
            if (Verify(mixer))
                Debug.Log($"[JamKit] AudioMixer ready at {assetPath} — MasterVol / MusicVol / SfxVol exposed.");
            else
                LogManualInstructions();
            return assetPath;
        }

        // -------------------- creation --------------------

        static bool TryCreateMixerViaReflection(string path, out AudioMixer mixer)
        {
            mixer = null;
            var ctrlType = typeof(EditorApplication).Assembly.GetType("UnityEditor.Audio.AudioMixerController");
            var method = ctrlType?.GetMethod(
                "CreateMixerControllerAtPath",
                BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(string) }, null);
            if (method == null) return false;

            mixer = method.Invoke(null, new object[] { path }) as AudioMixer;
            return mixer != null;
        }

        // -------------------- groups + exposure (idempotent) --------------------

        static void EnsureGroupsAndExposure(AudioMixer mixer)
        {
            // The loaded asset is an AudioMixerController; reflect on its real type.
            var ctrlType = mixer.GetType();

            var masterGroupProp = ctrlType.GetProperty("masterGroup", BindingFlags.Public | BindingFlags.Instance);
            object master = masterGroupProp?.GetValue(mixer);
            if (master == null)
            {
                Debug.LogWarning("[JamKit] Could not read the mixer's master group via reflection.");
                return;
            }

            object music = FindGroup(mixer, "Music") ?? CreateChildGroup(ctrlType, mixer, master, "Music");
            object sfx   = FindGroup(mixer, "SFX")   ?? CreateChildGroup(ctrlType, mixer, master, "SFX");

            ExposeVolumes(ctrlType, mixer, new[]
            {
                (group: master, name: "MasterVol"),
                (group: music,  name: "MusicVol"),
                (group: sfx,    name: "SfxVol"),
            });
        }

        static object FindGroup(AudioMixer mixer, string name)
        {
            var matches = mixer.FindMatchingGroups(name);
            if (matches == null) return null;
            foreach (var g in matches)
                if (g != null && g.name == name) return g;
            return null;
        }

        static object CreateChildGroup(Type ctrlType, AudioMixer mixer, object parent, string name)
        {
            var addGroup = ctrlType.GetMethod("CreateNewGroup", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string), typeof(bool) }, null);
            var addChild = ctrlType.GetMethod("AddChildToParent", BindingFlags.Public | BindingFlags.Instance);
            if (addGroup == null || addChild == null)
            {
                Debug.LogWarning($"[JamKit] Could not create mixer group '{name}' via reflection.");
                return null;
            }

            var group = addGroup.Invoke(mixer, new object[] { name, false });
            addChild.Invoke(mixer, new[] { group, parent });
            ctrlType.GetMethod("AddGroupToCurrentView", BindingFlags.Public | BindingFlags.Instance)
                ?.Invoke(mixer, new[] { group });
            return group;
        }

        /// <summary>
        /// Expose each group's volume under the given name by appending to the controller's
        /// exposedParameters array. (Earlier versions probed for an AddExposedParameter method,
        /// which doesn't exist — the array property is the path the mixer inspector itself uses.)
        /// </summary>
        static void ExposeVolumes(Type ctrlType, AudioMixer mixer, (object group, string name)[] wanted)
        {
            var exposedProp = ctrlType.GetProperty("exposedParameters", BindingFlags.Public | BindingFlags.Instance);
            if (exposedProp == null)
            {
                Debug.LogWarning("[JamKit] AudioMixerController.exposedParameters not found via reflection.");
                return;
            }

            var paramType = exposedProp.PropertyType.GetElementType(); // ExposedAudioParameter
            var existing = exposedProp.GetValue(mixer) as Array ?? Array.CreateInstance(paramType, 0);

            var have = new HashSet<string>();
            foreach (var p in existing)
            {
                if (GetMember(paramType, p, "name") is string n) have.Add(n);
            }

            var toAdd = new List<object>();
            foreach (var (group, name) in wanted)
            {
                if (group == null || have.Contains(name)) continue;
                var getGuid = group.GetType().GetMethod("GetGUIDForVolume", BindingFlags.Public | BindingFlags.Instance);
                if (getGuid == null) continue;

                var param = Activator.CreateInstance(paramType);
                SetMember(paramType, param, "guid", getGuid.Invoke(group, null));
                SetMember(paramType, param, "name", name);
                toAdd.Add(param);
            }
            if (toAdd.Count == 0) return;

            var merged = Array.CreateInstance(paramType, existing.Length + toAdd.Count);
            existing.CopyTo(merged, 0);
            for (int i = 0; i < toAdd.Count; i++) merged.SetValue(toAdd[i], existing.Length + i);
            exposedProp.SetValue(mixer, merged);
        }

        static object GetMember(Type type, object instance, string member)
        {
            var f = type.GetField(member, BindingFlags.Public | BindingFlags.Instance);
            if (f != null) return f.GetValue(instance);
            var p = type.GetProperty(member, BindingFlags.Public | BindingFlags.Instance);
            return p?.GetValue(instance);
        }

        static void SetMember(Type type, object instance, string member, object value)
        {
            var f = type.GetField(member, BindingFlags.Public | BindingFlags.Instance);
            if (f != null) { f.SetValue(instance, value); return; }
            var p = type.GetProperty(member, BindingFlags.Public | BindingFlags.Instance);
            p?.SetValue(instance, value);
        }

        // -------------------- verification --------------------

        static bool Verify(AudioMixer mixer)
        {
            if (mixer == null) return false;
            foreach (var name in ExposedNames)
                if (!mixer.GetFloat(name, out _)) return false;
            return true;
        }

        static void LogManualInstructions()
        {
            Debug.LogWarning(
                "[JamKit] Could not fully auto-configure JamKitMixer (volume parameters are not exposed, " +
                "so volume sliders will not work). Finish it manually:\n" +
                "  1. If missing, create it: right-click in Project > Create > Audio Mixer, name it 'JamKitMixer', put it in a 'Resources' folder.\n" +
                "  2. Add child groups named 'Music' and 'SFX' under Master.\n" +
                "  3. Select each group, right-click its Volume in the Inspector > 'Expose ... to script'.\n" +
                "  4. In the mixer's 'Exposed Parameters' dropdown, rename them to MasterVol, MusicVol, SfxVol.");
        }
    }
}
