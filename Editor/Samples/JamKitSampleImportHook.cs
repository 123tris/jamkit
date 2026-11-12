using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Metz.JamKit.Editor
{
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
                    if (path.EndsWith($"/{spec.Name}/{spec.Name}.unity")
                        && !pending.Contains(spec.Name)
                        && !EditorPrefs.GetBool(OfferedKey(spec), false))
                        pending.Add(spec.Name);
                }
            }

            if (pending.Count == 0)
            {
                SessionState.SetString(PendingKey, "");
                return;
            }

            if (!didDomainReload && EditorApplication.isCompiling)
            {
                SessionState.SetString(PendingKey, string.Join("|", pending));
                return;
            }

            SessionState.SetString(PendingKey, "");
            var toOffer = new List<string>(pending);
            EditorApplication.delayCall += () => Offer(toOffer);
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
                    $"Set up '{specs[0].Name}' now?\n\nScaffolds the project if missing, wires the sample's prefabs " +
                    "to your service assets, and opens its ready-to-play scene.\n\nAlso available any time: JamKit > Samples.",
                    "Set Up Now", "Later"))
                    JamKitSampleSetup.SetUp(specs[0]);
                return;
            }

            var list = new StringBuilder();
            foreach (var spec in specs) list.Append("  • ").Append(spec.Name).Append('\n');
            if (EditorUtility.DisplayDialog("JamKit Samples Imported",
                $"Set up all {specs.Count} imported samples now?\n\n{list}\nEach gets its prefabs wired and its scene " +
                "readied (the last one stays open).\n\nAlso available one at a time: JamKit > Samples.",
                "Set Up All", "Later"))
                foreach (var spec in specs)
                    JamKitSampleSetup.SetUp(spec);
        }
    }
}
