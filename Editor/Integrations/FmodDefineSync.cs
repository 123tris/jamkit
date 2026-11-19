using System;
using UnityEditor;
using UnityEditor.Build;

namespace Metz.JamKit.Editor
{
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
                catch { continue; }

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
                    ? "[JamKit] FMOD for Unity detected — added the JAMKIT_FMOD define; FMOD-aware code paths are now active."
                    : "[JamKit] FMOD for Unity no longer present — removed the JAMKIT_FMOD define.");
        }
    }
}
