using System.IO;
using Metz.JamKit.Editor;
using NUnit.Framework;

namespace Metz.JamKit.Tests.Editor
{
    public class ProjectTemplateTests
    {
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

        static void Write(string folder, string relative, string text)
        {
            string path = Path.Combine(folder, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, text);
        }

        string Read(string relative) => File.ReadAllText(Path.Combine(destination, relative));
    }
}
