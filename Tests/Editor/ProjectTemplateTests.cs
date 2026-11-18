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
        static readonly string TemplateScene = $"m_SourcePrefab: {{fileID: 100100000, guid: {Id(3)}, type: 3}}\n";

        string root;
        string source;
        string destination;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "JamKitProjectTemplateTests", Path.GetRandomFileName());
            source = Path.Combine(root, "Template");
            destination = Path.Combine(root, "Project");

            Write(source, "Scenes.meta", FolderMeta(1));
            Write(source, "Scenes/Game.unity", TemplateScene);
            Write(source, "Scenes/Game.unity.meta", AssetMeta(2));
            Write(source, "Prefabs.meta", FolderMeta(4));
            Write(source, "Prefabs/Starters.meta", FolderMeta(5));
            Write(source, "Prefabs/Starters/Pickup.prefab", "template pickup");
            Write(source, "Prefabs/Starters/Pickup.prefab.meta", AssetMeta(3));
            Write(source, "Art.meta", FolderMeta(6));
        }

        [TearDown]
        public void TearDown() => Directory.Delete(root, true);

        [Test]
        public void CopiesEveryFileWithItsMeta()
        {
            var copied = ProjectTemplate.CopyMissing(source, destination);

            CollectionAssert.AreEquivalent(new[] { "Scenes/Game.unity", "Prefabs/Starters/Pickup.prefab" }, copied);
            Assert.AreEqual(TemplateScene, Read("Scenes/Game.unity"));
            Assert.AreEqual(AssetMeta(2), Read("Scenes/Game.unity.meta"));
            Assert.AreEqual(AssetMeta(3), Read("Prefabs/Starters/Pickup.prefab.meta"));
            Assert.AreEqual(FolderMeta(5), Read("Prefabs/Starters.meta"));
        }

        [Test]
        public void CreatesAFolderThatHoldsNoFiles()
        {
            ProjectTemplate.CopyMissing(source, destination);

            Assert.IsTrue(Directory.Exists(Path.Combine(destination, "Art")));
            Assert.AreEqual(FolderMeta(6), Read("Art.meta"));
        }

        [Test]
        public void SkipsAMetaWhoseFileIsGone()
        {
            Write(source, "Prefabs/Starters/Gone.prefab.meta", AssetMeta(7));

            var copied = ProjectTemplate.CopyMissing(source, destination);

            CollectionAssert.DoesNotContain(copied, "Prefabs/Starters/Gone.prefab");
            Assert.IsFalse(Directory.Exists(Path.Combine(destination, "Prefabs/Starters/Gone.prefab")));
            Assert.IsFalse(File.Exists(Path.Combine(destination, "Prefabs/Starters/Gone.prefab.meta")));
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
        public void FindsAProjectFileWithAnotherGuidThatACopyWouldReferTo()
        {
            Write(destination, "Prefabs/Starters/Pickup.prefab", "older pickup");
            Write(destination, "Prefabs/Starters/Pickup.prefab.meta", AssetMeta(99));

            CollectionAssert.AreEqual(new[] { "Prefabs/Starters/Pickup.prefab" }, ProjectTemplate.Conflicts(source, destination));
        }

        [Test]
        public void FindsNoConflictWhenTheProjectFileHasTheTemplatesGuid()
        {
            Write(destination, "Prefabs/Starters/Pickup.prefab", "edited pickup");
            Write(destination, "Prefabs/Starters/Pickup.prefab.meta", AssetMeta(3));

            CollectionAssert.IsEmpty(ProjectTemplate.Conflicts(source, destination));
        }

        [Test]
        public void FindsNoConflictWhenNoCopyWouldReferToTheProjectFile()
        {
            Write(destination, "Scenes/Game.unity", "my scene");
            Write(destination, "Scenes/Game.unity.meta", AssetMeta(99));

            CollectionAssert.IsEmpty(ProjectTemplate.Conflicts(source, destination));
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
        public void EveryTemplateMetaBelongsToAFileOrAFolder()
        {
            string[] orphans = Directory.GetFiles(ProjectTemplate.Source, "*.meta", SearchOption.AllDirectories)
                .Where(meta => !File.Exists(meta.Substring(0, meta.Length - ".meta".Length)) && !File.ReadAllText(meta).Contains("folderAsset: yes"))
                .ToArray();

            CollectionAssert.IsEmpty(orphans);
        }

        [Test]
        public void EveryAssetTheTemplateRefersToIsInEveryScaffoldedProject()
        {
            string template = ProjectTemplate.Source;
            var own = new HashSet<string>(Directory.GetFiles(template, "*.meta", SearchOption.AllDirectories)
                .Select(meta => Identity.Match(File.ReadAllText(meta)).Groups[1].Value));
            string[] packages = PackagesEveryProjectHas();
            var missing = new List<string>();

            foreach (string file in Directory.GetFiles(template, "*", SearchOption.AllDirectories).Where(path => !path.EndsWith(".meta")))
                foreach (Match reference in Reference.Matches(File.ReadAllText(file)))
                {
                    string guid = reference.Groups[1].Success ? reference.Groups[1].Value : reference.Groups[2].Value;
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (own.Contains(guid) || BuiltIn.Contains(guid) || packages.Any(package => path.StartsWith(package, StringComparison.Ordinal))) continue;
                    missing.Add($"{Path.GetFileName(file)} refers to {guid} ({(path == "" ? "not in this project" : path)})");
                }

            CollectionAssert.IsEmpty(missing);
        }

        [Test]
        public void ANewLevelJoinsTheBuildBeforeGameOver()
        {
            string first = ProjectTemplate.Root + "/Scenes/Level1.unity";
            string second = ProjectTemplate.Root + "/Scenes/Level2.unity";
            string[] build = { ProjectTemplate.BootstrapScene, ProjectTemplate.GameScene, ProjectTemplate.GameOverScene };

            CollectionAssert.AreEqual(
                new[] { ProjectTemplate.BootstrapScene, ProjectTemplate.GameScene, first, second, ProjectTemplate.GameOverScene },
                ProjectTemplate.WithLevel(ProjectTemplate.WithLevel(build, first), second));
            CollectionAssert.AreEqual(new[] { "Assets/Other.unity", first }, ProjectTemplate.WithLevel(new[] { "Assets/Other.unity" }, first));
            CollectionAssert.AreEqual(build, ProjectTemplate.WithLevel(build, ProjectTemplate.GameScene));
        }

        [Test]
        public void PutsTheScenesItAddsAtTheFrontOfTheBuild()
        {
            CollectionAssert.AreEqual(new[] { "Bootstrap", "Game", "GameOver", "Sample" },
                ProjectTemplate.BuildOrder(new[] { "Sample" }, new[] { "Bootstrap", "Game", "GameOver" }));
        }

        [Test]
        public void AddsAMissingSceneAfterTheTemplateSceneBeforeIt()
        {
            CollectionAssert.AreEqual(new[] { "Intro", "Bootstrap", "Game", "GameOver" },
                ProjectTemplate.BuildOrder(new[] { "Intro", "Bootstrap", "GameOver" }, new[] { "Bootstrap", "Game", "GameOver" }));
        }

        [Test]
        public void LeavesTheScenesAlreadyInTheBuildWhereTheyAre()
        {
            string[] build = { "Example", "Bootstrap", "Game", "GameOver" };

            CollectionAssert.AreEqual(build, ProjectTemplate.BuildOrder(build, new[] { "Bootstrap", "Game", "GameOver" }));
        }

        // JamKit, Ripple and every package JamKit depends on, directly or not.
        static string[] PackagesEveryProjectHas()
        {
            var jamKit = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(ProjectTemplate).Assembly);
            return jamKit.resolvedDependencies.Select(dependency => dependency.name)
                .Concat(new[] { jamKit.name, "com.metz.ripple" })
                .Select(name => $"Packages/{name}/")
                .ToArray();
        }

        static string Id(int number) => number.ToString("x32");

        static string FolderMeta(int id) => $"fileFormatVersion: 2\nguid: {Id(id)}\nfolderAsset: yes\n";

        static string AssetMeta(int id) => $"fileFormatVersion: 2\nguid: {Id(id)}\n";

        static void Write(string folder, string relative, string text)
        {
            string path = Path.Combine(folder, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, text);
        }

        string Read(string relative) => File.ReadAllText(Path.Combine(destination, relative));
    }
}
