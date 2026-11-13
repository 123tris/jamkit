using System.Collections.Generic;
using System.IO;
using System.Linq;
using Metz.JamKit.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit.Tests.Editor
{
    public class UIDocumentTests
    {
        const string Templates = "Packages/com.metz.jamkit/Editor/Templates";
        const string Overlays = "Packages/com.metz.jamkit/Runtime/UI/Overlays";

        static VisualElement Clone(string path) => AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path).CloneTree();

        static IEnumerable<string> Documents() =>
            Directory.GetFiles(Templates, "*.uxml").Concat(Directory.GetFiles(Overlays, "*.uxml")).Select(path => path.Replace(Path.DirectorySeparatorChar, '/'));

        static bool Previewed(VisualElement view)
        {
            if (view.ClassListContains("jk-view--hidden")) return false;
            for (VisualElement parent = view.parent; parent != null; parent = parent.parent)
                if (parent.ClassListContains("jk-instance--hidden"))
                    return false;
            return true;
        }

        [Test]
        public void CombinedMenuPreviewsOnlyTheStartScreen()
        {
            string[] previewed = Clone($"{Templates}/JamKitMenu.uxml").Query(className: "jk-view").ToList()
                .Where(Previewed)
                .Select(view => view.name)
                .ToArray();

            CollectionAssert.AreEqual(new[] { "start-menu" }, previewed);
        }

        [Test]
        public void EachScreenPreviewsInItsOwnDocument([Values("JamKitStartMenu", "JamKitPauseMenu", "JamKitSettingsMenu", "JamKitGameOverMenu")] string screen)
        {
            Assert.IsTrue(Previewed(Clone($"{Templates}/{screen}.uxml").Q(className: "jk-view")));
        }

        [Test]
        public void DocumentsLeaveStylingToTheirStylesheets([ValueSource(nameof(Documents))] string path)
        {
            StringAssert.DoesNotContain(" style=\"", File.ReadAllText(path));
        }

        [Test]
        public void GameOverScoreIsBoundToAVariable()
        {
            var score = Clone($"{Templates}/JamKitGameOverMenu.uxml").Q<ValueLabel>("gameover-score");

            Assert.IsTrue(score.TryGetBinding(nameof(ValueLabel.value), out Binding binding));
            Assert.AreEqual("CurrentValue", ((DataBinding)binding).dataSourcePath.ToString());
        }

        [Test]
        public void DesktopOnlySettingsRowsAreMarked()
        {
            VisualElement settings = Clone($"{Templates}/JamKitSettingsMenu.uxml");

            foreach (string control in new[] { "settings-resolution", "settings-vsync" })
                Assert.IsTrue(settings.Q(control).parent.ClassListContains("jk-row--desktop"), control);
            Assert.IsFalse(settings.Q("settings-quality").parent.ClassListContains("jk-row--desktop"));
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
            PanelSettings panel = TemplateAssets.OverlayPanel(overlay);

            Assert.AreEqual(TemplateAssets.Theme, panel.themeStyleSheet);
            Assert.AreEqual(order, panel.sortingOrder);
        }

        [Test]
        public void MenuPanelTemplateUsesTheJamKitTheme()
        {
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>($"{Templates}/JamKitPanelSettings.asset");

            Assert.AreEqual(TemplateAssets.Theme, panel.themeStyleSheet);
        }

        [Test]
        public void NewScreenFadeUsesTheFadeOverlay()
        {
            var fade = ScriptableObject.CreateInstance<ScreenFadeSO>();

            Assert.AreEqual(TemplateAssets.Overlay("JamKitFade"), fade.Document);
            Assert.AreEqual(TemplateAssets.OverlayPanel("JamKitFade"), fade.Panel);
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
