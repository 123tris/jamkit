using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Metz.JamKit.Editor;
using NUnit.Framework;
using UnityEditor;

namespace Metz.JamKit.Tests.Editor
{
    public class ProjectTemplateTests
    {
        static readonly Regex Reference = new(@"guid: ([0-9a-f]{32}), type:|guid=([0-9a-f]{32})");
        static readonly Regex Identity = new(@"^guid: ([0-9a-f]{32})", RegexOptions.Multiline);
        static readonly string[] BuiltIn = { "0000000000000000e000000000000000", "0000000000000000f000000000000000" };

        string root;
        string source;
        string destination;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "JamKitProjectTemplateTests", Path.GetRandomFileName());
            source = Path.Combine(root, "Template");
            destination = Path.Combine(root, "Project");

            Write(source, "Scenes.meta", "folder");
            Write(source, "Scenes/Game.unity", "template scene");
            Write(source, "Scenes/Game.unity.meta", "scene guid");
            Write(source, "Prefabs.meta", "folder");
            Write(source, "Prefabs/Starters.meta", "folder");
            Write(source, "Prefabs/Starters/Pickup.prefab", "template pickup");
            Write(source, "Prefabs/Starters/Pickup.prefab.meta", "pickup guid");
            Write(source, "Art.meta", "folder");
        }

        [TearDown]
        public void TearDown() => Directory.Delete(root, true);

        [Test]
        public void CopiesEveryFileWithItsMeta()
        {
            var copied = ProjectTemplate.CopyMissing(source, destination);

            CollectionAssert.AreEquivalent(new[] { "Scenes/Game.unity", "Prefabs/Starters/Pickup.prefab" }, copied);
            Assert.AreEqual("template scene", Read("Scenes/Game.unity"));
            Assert.AreEqual("scene guid", Read("Scenes/Game.unity.meta"));
            Assert.AreEqual("pickup guid", Read("Prefabs/Starters/Pickup.prefab.meta"));
            Assert.AreEqual("folder", Read("Prefabs/Starters.meta"));
        }

        [Test]
        public void CreatesAFolderThatHoldsNoFiles()
        {
            ProjectTemplate.CopyMissing(source, destination);

            Assert.IsTrue(Directory.Exists(Path.Combine(destination, "Art")));
            Assert.AreEqual("folder", Read("Art.meta"));
        }

        [Test]
        public void LeavesAnExistingFileAndItsMetaUntouched()
        {
            Write(destination, "Scenes/Game.unity", "my scene");
            Write(destination, "Scenes/Game.unity.meta", "my guid");
            Write(destination, "Scenes.meta", "my folder");

            var copied = ProjectTemplate.CopyMissing(source, destination);

            CollectionAssert.AreEquivalent(new[] { "Prefabs/Starters/Pickup.prefab" }, copied);
            Assert.AreEqual("my scene", Read("Scenes/Game.unity"));
            Assert.AreEqual("my guid", Read("Scenes/Game.unity.meta"));
            Assert.AreEqual("my folder", Read("Scenes.meta"));
        }

        [Test]
        public void CopiesNothingTheSecondTime()
        {
            ProjectTemplate.CopyMissing(source, destination);

            CollectionAssert.IsEmpty(ProjectTemplate.CopyMissing(source, destination));
        }

        [Test]
        public void BringsBackOnlyTheFileThatWasDeleted()
        {
            ProjectTemplate.CopyMissing(source, destination);
            Write(destination, "Scenes/Game.unity", "edited scene");
            File.Delete(Path.Combine(destination, "Prefabs/Starters/Pickup.prefab"));

            var copied = ProjectTemplate.CopyMissing(source, destination);

            CollectionAssert.AreEquivalent(new[] { "Prefabs/Starters/Pickup.prefab" }, copied);
            Assert.AreEqual("template pickup", Read("Prefabs/Starters/Pickup.prefab"));
            Assert.AreEqual("edited scene", Read("Scenes/Game.unity"));
        }

        [Test]
        public void EveryTemplateFileHasItsMeta()
        {
            string[] bare = Directory.GetFileSystemEntries(ProjectTemplate.Source, "*", SearchOption.AllDirectories)
                .Where(path => !path.EndsWith(".meta") && !File.Exists(path + ".meta"))
                .ToArray();

            CollectionAssert.IsEmpty(bare);
        }

        [Test]
        public void EveryAssetTheTemplateRefersToExists()
        {
            string template = ProjectTemplate.Source;
            var own = new HashSet<string>(Directory.GetFiles(template, "*.meta", SearchOption.AllDirectories)
                .Select(meta => Identity.Match(File.ReadAllText(meta)).Groups[1].Value));
            var missing = new List<string>();

            foreach (string file in Directory.GetFiles(template, "*", SearchOption.AllDirectories).Where(path => !path.EndsWith(".meta")))
                foreach (Match reference in Reference.Matches(File.ReadAllText(file)))
                {
                    string guid = reference.Groups[1].Success ? reference.Groups[1].Value : reference.Groups[2].Value;
                    if (own.Contains(guid) || BuiltIn.Contains(guid) || AssetDatabase.GUIDToAssetPath(guid) != "") continue;
                    missing.Add($"{Path.GetFileName(file)} refers to {guid}");
                }

            CollectionAssert.IsEmpty(missing);
        }

        [Test]
        public void EveryStarterInTheCreateMenuIsInTheTemplate()
        {
            foreach (Starter starter in Enum.GetValues(typeof(Starter)))
            {
                string relative = ProjectTemplate.StarterPath(starter.ToString()).Substring(ProjectTemplate.Root.Length + 1);
                Assert.IsTrue(File.Exists(Path.Combine(ProjectTemplate.Source, relative)), starter.ToString());
            }
        }

        [Test]
        public void TheDoctorFindsNothingWrongWithAScaffoldedProject()
        {
            bool scaffoldedHere = !Directory.Exists(ProjectTemplate.Root);
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scaffoldedHere) ProjectTemplate.Scaffold();

            try
            {
                string[] problems = JamKitDoctor.ScanProject()
                    .Where(issue => issue.Severity != MessageType.Info)
                    .Where(issue => issue.Context == null || AssetDatabase.GetAssetPath(issue.Context).StartsWith(ProjectTemplate.Root))
                    .Select(issue => issue.Message)
                    .ToArray();

                CollectionAssert.IsEmpty(problems);
            }
            finally
            {
                if (scaffoldedHere)
                {
                    AssetDatabase.DeleteAsset(ProjectTemplate.Root);
                    EditorBuildSettings.scenes = scenes;
                }
            }
        }

        static void Write(string folder, string relative, string text)
        {
            string path = Path.Combine(folder, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, text);
        }

        string Read(string relative) => File.ReadAllText(Path.Combine(destination, relative));
    }
}
