using System.IO;
using Metz.JamKit.Editor;
using NUnit.Framework;

namespace Metz.JamKit.Tests.Editor
{
    /// <summary>
    /// Registry ↔ Samples~ ↔ package.json consistency. The one-click setup finds demos by type
    /// name and scenes by folder name, so a sample rename that skips one of the three places
    /// silently breaks it — these tests make that a red bar instead. (Demo *types* can't be
    /// checked here: Samples~ never compiles inside the package project.)
    /// </summary>
    public class SampleSetupRegistryTests
    {
        static string PackageRoot => Path.GetFullPath("Packages/com.metz.jamkit");
        static string SamplesRoot => Path.Combine(PackageRoot, "Samples~");

        [Test]
        public void EverySpecHasFolderDemoScriptAndReadme()
        {
            foreach (var spec in JamKitSampleSetup.Specs)
            {
                var dir = Path.Combine(SamplesRoot, spec.Name);
                Assert.IsTrue(Directory.Exists(dir), $"Missing sample folder: {dir}");

                var script = spec.TypeName.Substring(spec.TypeName.LastIndexOf('.') + 1) + ".cs";
                Assert.IsTrue(File.Exists(Path.Combine(dir, script)),
                    $"{spec.Name}: expected {script} (setup locates the sample by this script).");
                Assert.IsTrue(File.Exists(Path.Combine(dir, "README.md")),
                    $"{spec.Name}: expected README.md.");
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

        [Test]
        public void SpecTypesLiveInTheSamplesNamespace()
        {
            // WarnIfOtherDemoPresent keys off this namespace; a demo outside it would dodge the warning.
            foreach (var spec in JamKitSampleSetup.Specs)
                StringAssert.StartsWith("Metz.JamKit.Samples.", spec.TypeName);
        }
    }
}
