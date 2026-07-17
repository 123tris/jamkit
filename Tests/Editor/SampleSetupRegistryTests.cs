using System.IO;
using Metz.JamKit.Editor;
using NUnit.Framework;

namespace Metz.JamKit.Tests.Editor
{
    /// <summary>
    /// Registry ↔ Samples~ ↔ package.json consistency. The one-click setup finds samples by the
    /// "&lt;Name&gt;/&lt;Name&gt;.unity" scene convention, so a rename that skips one of the three
    /// places silently breaks it — these tests make that a red bar instead.
    /// </summary>
    public class SampleSetupRegistryTests
    {
        static string PackageRoot => Path.GetFullPath("Packages/com.metz.jamkit");
        static string SamplesRoot => Path.Combine(PackageRoot, "Samples~");

        [Test]
        public void EverySpecHasFolderSceneAndReadme()
        {
            foreach (var spec in JamKitSampleSetup.Specs)
            {
                var dir = Path.Combine(SamplesRoot, spec.Name);
                Assert.IsTrue(Directory.Exists(dir), $"Missing sample folder: {dir}");
                Assert.IsTrue(File.Exists(Path.Combine(dir, spec.Name + ".unity")),
                    $"{spec.Name}: expected {spec.Name}.unity (setup locates the sample by this scene).");
                Assert.IsTrue(File.Exists(Path.Combine(dir, "README.md")),
                    $"{spec.Name}: expected README.md.");
                if (spec.InGameScene)
                    Assert.IsTrue(File.Exists(Path.Combine(dir, "Prefabs", spec.ArenaPrefab + ".prefab")),
                        $"{spec.Name}: expected Prefabs/{spec.ArenaPrefab}.prefab (dropped into Game.unity by setup).");
            }
        }

        [Test]
        public void EverySampleFolderIsRegistered()
        {
            foreach (var dir in Directory.GetDirectories(SamplesRoot))
            {
                var name = Path.GetFileName(dir);
                bool registered = false;
                foreach (var spec in JamKitSampleSetup.Specs)
                    if (spec.Name == name) registered = true;
                Assert.IsTrue(registered,
                    $"Samples~/{name} has no JamKitSampleSetup spec — one-click setup won't cover it.");
            }
        }

        [Test]
        public void PackageJsonListsEverySpec()
        {
            var json = File.ReadAllText(Path.Combine(PackageRoot, "package.json"));
            foreach (var spec in JamKitSampleSetup.Specs)
                StringAssert.Contains($"\"displayName\": \"{spec.Name}\"", json,
                    $"package.json samples[] is missing '{spec.Name}'.");
        }
    }
}
