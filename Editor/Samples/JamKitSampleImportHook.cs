using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Metz.JamKit.Editor
{
    /// <summary>
    /// Offers one-click setup the moment a sample lands in Assets: the demo script's import is
    /// stashed (the type only exists after the reload that compiles it), and once the domain
    /// reload completes the offer dialog runs the same setup as <c>JamKit &gt; Samples</c>.
    /// Each sample is offered once per project (declining leaves the menu as the path back),
    /// and never in batch mode — CI imports must not block on a dialog.
    /// </summary>
    sealed class JamKitSampleImportHook : AssetPostprocessor
    {
        const string PendingKey = "JamKit.PendingSampleOffers";

        static string OfferedKey(JamKitSampleSetup.Spec spec)
            => $"JamKit.SampleOffered.{PlayerSettings.productGUID}.{spec.Name}";

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            if (Application.isBatchMode) return;

            var pending = new List<string>(SessionState.GetString(PendingKey, "")
                .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries));

            foreach (var path in importedAssets)
            {
                if (!path.StartsWith("Assets/")) continue;
                foreach (var spec in JamKitSampleSetup.Specs)
                {
                    var scriptName = spec.TypeName.Substring(spec.TypeName.LastIndexOf('.') + 1) + ".cs";
                    if (path.EndsWith("/" + scriptName)
                        && !pending.Contains(spec.Name)
                        && !EditorPrefs.GetBool(OfferedKey(spec), false))
                        pending.Add(spec.Name);
                }
            }

            if (!didDomainReload || pending.Count == 0)
            {
                SessionState.SetString(PendingKey, string.Join("|", pending));
                return;
            }

            SessionState.SetString(PendingKey, "");
            // Escape the import pipeline before touching scenes or showing dialogs.
            EditorApplication.delayCall += () => Offer(pending);
        }

        static void Offer(List<string> names)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            var specs = new List<JamKitSampleSetup.Spec>();
            foreach (var spec in JamKitSampleSetup.Specs)
                if (names.Contains(spec.Name) && JamKitSampleSetup.IsImported(spec))
                    specs.Add(spec);
            if (specs.Count == 0) return;

            foreach (var spec in specs) EditorPrefs.SetBool(OfferedKey(spec), true);

            if (specs.Count == 1)
            {
                if (EditorUtility.DisplayDialog("JamKit Sample Imported",
                    $"Set up '{specs[0].Name}' now?\n\nBuilds a ready-to-play scene: project scaffold if missing, " +
                    "JamKitCore, the demo component, service references assigned.\n\nAlso available any time: JamKit > Samples.",
                    "Set Up Now", "Later"))
                    JamKitSampleSetup.SetUp(specs[0]);
                return;
            }

            var list = new StringBuilder();
            foreach (var spec in specs) list.Append("  • ").Append(spec.Name).Append('\n');
            if (EditorUtility.DisplayDialog("JamKit Samples Imported",
                $"Set up all {specs.Count} imported samples now?\n\n{list}\nEach gets its own ready-to-play scene " +
                "(the last one stays open).\n\nAlso available one at a time: JamKit > Samples.",
                "Set Up All", "Later"))
                foreach (var spec in specs)
                    JamKitSampleSetup.SetUp(spec);
        }
    }
}
