using System.IO;
using System.Linq;
using Metz.JamKit.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace Metz.JamKit.Tests.Editor
{
    // Scaffolds the project template once, into a project without Assets/_Project, and checks it as Unity loads it.
    public class ScaffoldedProjectTests
    {
        EditorBuildSettingsScene[] scenes;
        bool scaffolded;

        [OneTimeSetUp]
        public void ScaffoldTheTemplate()
        {
            if (Directory.Exists(ProjectTemplate.Root))
                Assert.Ignore($"{ProjectTemplate.Root} already exists, so a scaffold here would keep its files instead of the template's. " +
                              "Run these tests in a project without it.");

            scenes = EditorBuildSettings.scenes;
            scaffolded = true;
            try
            {
                ProjectTemplate.Scaffold();
            }
            catch
            {
                DeleteTheScaffold();
                throw;
            }
        }

        // Only deletes what ScaffoldTheTemplate made, so a project's own Assets/_Project is never touched.
        [OneTimeTearDown]
        public void DeleteTheScaffold()
        {
            if (!scaffolded) return;
            scaffolded = false;
            AssetDatabase.DeleteAsset(ProjectTemplate.Root);
            EditorBuildSettings.scenes = scenes;
        }

        [Test]
        public void TheDoctorFindsNothingWrong()
        {
            string[] problems = JamKitDoctor.ScanProject()
                .Where(issue => issue.Severity != MessageType.Info)
                .Where(issue => issue.Context == null || AssetDatabase.GetAssetPath(issue.Context).StartsWith(ProjectTemplate.Root))
                .Select(issue => issue.Message)
                .ToArray();

            CollectionAssert.IsEmpty(problems);
        }

        [Test]
        public void TheMenuHoldsEveryScreen()
        {
            string[] views = Clone("JamKitMenu").Query(className: "jk-view").ToList().Select(view => view.name).ToArray();

            CollectionAssert.AreEquivalent(new[] { "start-menu", "pause-menu", "settings-menu", "gameover-menu" }, views);
        }

        [Test]
        public void TheGameOverScoreIsBoundToTheScoreVariable()
        {
            var score = Clone("JamKitGameOverMenu").Q<ValueLabel>("gameover-score");

            Assert.IsNotNull(score, "gameover-score did not load as a ValueLabel");
            Assert.IsTrue(score.TryGetBinding(nameof(ValueLabel.value), out Binding binding));
            Assert.AreEqual("CurrentValue", ((DataBinding)binding).dataSourcePath.ToString());
            Assert.AreEqual(AssetDatabase.LoadMainAssetAtPath($"{ProjectTemplate.Root}/Variables/Score.asset"), score.dataSource);
        }

        static VisualElement Clone(string document)
            => AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{ProjectTemplate.Root}/UI/{document}.uxml").CloneTree();
    }
}
