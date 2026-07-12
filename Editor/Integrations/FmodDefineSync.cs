using System;
using UnityEditor;
using UnityEditor.Build;

namespace Metz.JamKit.Editor
{
    /// <summary>
    /// Keeps the JAMKIT_FMOD scripting define in sync with whether FMOD for Unity is installed.
    /// FMOD ships as loose assets (not a UPM package), so asmdef versionDefines can't detect it —
    /// instead this probes for the FMODUnity assembly after every domain reload and toggles the
    /// define on the jam target groups. The define gates the Metz.JamKit.Fmod assemblies:
    /// install FMOD and the FMOD service/juice components appear; remove it and they vanish
    /// instead of breaking the compile.
    /// </summary>
    [InitializeOnLoad]
    static class FmodDefineSync
    {
        const string Define = "JAMKIT_FMOD";

        static readonly NamedBuildTarget[] Targets =
        {
            NamedBuildTarget.Standalone, NamedBuildTarget.WebGL, NamedBuildTarget.Android, NamedBuildTarget.iOS,
        };

        static FmodDefineSync()
        {
            // PlayerSettings writes are unreliable while InitializeOnLoad is still running.
            EditorApplication.delayCall += Sync;
        }

        static void Sync()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            bool fmodInstalled = Type.GetType("FMODUnity.RuntimeManager, FMODUnity") != null;
            bool changed = false;

            foreach (var target in Targets)
            {
                string current;
                try { current = PlayerSettings.GetScriptingDefineSymbols(target); }
                catch { continue; } // platform support not installed

                var defines = new System.Collections.Generic.List<string>(
                    current.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                bool has = defines.Contains(Define);
                if (has == fmodInstalled) continue;

                if (fmodInstalled) defines.Add(Define);
                else defines.Remove(Define);
                try
                {
                    PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", defines));
                    changed = true;
                }
                catch { /* platform support not installed */ }
            }

            if (changed)
                UnityEngine.Debug.Log(fmodInstalled
                    ? "[JamKit] FMOD for Unity detected — added the JAMKIT_FMOD define. FMOD service and juice components are now available."
                    : "[JamKit] FMOD for Unity no longer present — removed the JAMKIT_FMOD define.");
        }
    }
}
