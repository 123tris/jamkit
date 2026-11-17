using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Metz.JamKit.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit.Tests.Editor
{
    public class UIDocumentTests
    {
        const string Overlays = "Packages/com.metz.jamkit/Runtime/UI/Overlays";
        const string Theme = "Packages/com.metz.jamkit/Runtime/UI/JamKitDefaultTheme.tss";

        static readonly XNamespace Ui = "UnityEngine.UIElements";

        static string Package => Directory.GetParent(ProjectTemplate.Source).FullName;

        static string TemplateUI => Path.Combine(ProjectTemplate.Source, "UI");

        static VisualElement Clone(string path) => AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path).CloneTree();

        // Unity doesn't import Template~, so these read the menu documents as XML. ScaffoldedProjectTests loads them as assets.
        static XElement Document(string name) => XDocument.Load(Path.Combine(TemplateUI, name + ".uxml")).Root;

        static IEnumerable<string> Documents() =>
            Directory.GetFiles(TemplateUI, "*.uxml").Concat(Directory.GetFiles(Path.Combine(Package, "Runtime/UI/Overlays"), "*.uxml"))
                .Select(path => Path.GetRelativePath(Package, path).Replace(Path.DirectorySeparatorChar, '/'));

        static bool HasClass(XElement element, string name) => ((string)element.Attribute("class") ?? "").Split(' ').Contains(name);

        static XElement View(XElement document) => document.Descendants().First(element => HasClass(element, "jk-view"));

        static XElement Named(XElement document, string name) => document.Descendants().Single(element => (string)element.Attribute("name") == name);

        static bool Previewed(XElement view)
            => !HasClass(view, "jk-view--hidden") && !view.Ancestors().Any(element => HasClass(element, "jk-instance--hidden"));

        [Test]
        public void CombinedMenuPreviewsOnlyTheStartScreen()
        {
            XElement menu = Document("JamKitMenu");
            Dictionary<string, string> sources = menu.Elements(Ui + "Template")
                .ToDictionary(template => (string)template.Attribute("name"), template => (string)template.Attribute("src"));

            string[] previewed = menu.Descendants(Ui + "Instance")
                .Where(instance => !instance.AncestorsAndSelf().Any(element => HasClass(element, "jk-instance--hidden")))
                .Select(instance => View(Document(Path.GetFileNameWithoutExtension(sources[(string)instance.Attribute("template")]))))
                .Where(Previewed)
                .Select(view => (string)view.Attribute("name"))
                .ToArray();

            CollectionAssert.AreEqual(new[] { "start-menu" }, previewed);
        }

        [Test]
        public void EachScreenPreviewsInItsOwnDocument([Values("JamKitStartMenu", "JamKitPauseMenu", "JamKitSettingsMenu", "JamKitGameOverMenu")] string screen)
        {
            Assert.IsTrue(Previewed(View(Document(screen))));
        }

        [Test]
        public void DocumentsLeaveStylingToTheirStylesheets([ValueSource(nameof(Documents))] string document)
        {
            StringAssert.DoesNotContain(" style=\"", File.ReadAllText(Path.Combine(Package, document)));
        }

        [Test]
        public void GameOverScoreIsBoundToTheScoreVariable()
        {
            XElement score = Named(Document("JamKitGameOverMenu"), "gameover-score");
            XElement binding = score.Descendants(Ui + "DataBinding").Single(element => (string)element.Attribute("property") == "value");
            string scoreMeta = File.ReadAllText(Path.Combine(ProjectTemplate.Source, "Variables/Score.asset.meta"));

            Assert.AreEqual("CurrentValue", (string)binding.Attribute("data-source-path"));
            StringAssert.Contains("guid=" + Regex.Match(scoreMeta, "guid: ([0-9a-f]{32})").Groups[1].Value, (string)score.Attribute("data-source"));
        }

        [Test]
        public void DesktopOnlySettingsRowsAreMarked()
        {
            XElement settings = Document("JamKitSettingsMenu");

            foreach (string control in new[] { "settings-resolution", "settings-vsync" })
                Assert.IsTrue(HasClass(Named(settings, control).Parent, "jk-row--desktop"), control);
            Assert.IsFalse(HasClass(Named(settings, "settings-quality").Parent, "jk-row--desktop"));
        }

        [Test]
        public void OverlaysHaveTheElementsTheirComponentsUse()
        {
            Assert.IsNotNull(Clone($"{Overlays}/JamKitToast.uxml").Q<Label>("toast-message"));
            Assert.IsNotNull(Clone($"{Overlays}/JamKitFade.uxml").Q("fade"));

            VisualElement debug = Clone($"{Overlays}/JamKitDebug.uxml");
            foreach (string element in new[] { "debug-panel", "debug-fps", "debug-time-scale", "debug-reload", "debug-scenes", "debug-quit" })
                Assert.IsNotNull(debug.Q(element), element);
        }

        [TestCase("JamKitToast", 60)]
        [TestCase("JamKitDebug", 30000)]
        [TestCase("JamKitFade", 32767)]
        public void OverlayPanelsUseTheJamKitTheme(string overlay, int order)
        {
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>($"{Overlays}/{overlay}PanelSettings.asset");

            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(Theme), panel.themeStyleSheet);
            Assert.AreEqual(order, panel.sortingOrder);
        }

        [Test]
        public void MenuPanelSettingsUseTheJamKitTheme()
        {
            string panel = File.ReadAllText(Path.Combine(TemplateUI, "JamKitPanelSettings.asset"));

            StringAssert.Contains($"guid: {AssetDatabase.AssetPathToGUID(Theme)}", Regex.Match(panel, "themeUss: .*").Value);
        }

        [Test]
        public void NewScreenFadeUsesTheFadeOverlay()
        {
            var fade = ScriptableObject.CreateInstance<ScreenFadeSO>();

            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{Overlays}/JamKitFade.uxml"), fade.Document);
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<PanelSettings>($"{Overlays}/JamKitFadePanelSettings.asset"), fade.Panel);
            Object.DestroyImmediate(fade);
        }

        [Test]
        public void DoctorWindowKnowsItsDocuments()
        {
            var window = ScriptableObject.CreateInstance<JamKitDoctorWindow>();
            var serialized = new SerializedObject(window);

            Assert.IsNotNull(serialized.FindProperty("layout").objectReferenceValue);
            Assert.IsNotNull(serialized.FindProperty("issueRow").objectReferenceValue);
            Object.DestroyImmediate(window);
        }

        [Test]
        public void SceneRefOutsideTheBuildShowsAsMissing()
        {
            var flow = ScriptableObject.CreateInstance<GameFlow>();
            flow.Transitions = new List<GameFlow.Transition> { new() { To = new SceneRef("NotARealScene") } };
            var serialized = new SerializedObject(flow);

            var field = (DropdownField)new SceneRefDrawer().CreatePropertyGUI(serialized.FindProperty("Transitions.Array.data[0].To"));

            Assert.AreEqual("NotARealScene  (not in Build Settings)", field.value);
            Assert.AreEqual("(none)", field.choices[0]);
            Object.DestroyImmediate(flow);
        }
    }
}
