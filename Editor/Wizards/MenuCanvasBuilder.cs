using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit.Editor
{
    /// <summary>
    /// Builds a generic UI Toolkit menu GameObject (UIDocument + <see cref="MenuController"/>)
    /// with the bundled UXML + PanelSettings assigned, but service references left blank.
    /// The user wires those after the fact (or the <see cref="JamProjectWizard"/> sets them).
    /// </summary>
    public static class MenuCanvasBuilder
    {
        public struct Refs
        {
            public GameObject Root;
            public UIDocument Document;
            public MenuController Controller;
        }

        public static Refs Build()
        {
            var refs = new Refs();
            refs.Root = new GameObject("JamKitMenu");
            refs.Document = refs.Root.AddComponent<UIDocument>();
            refs.Controller = refs.Root.AddComponent<MenuController>();

            var uxml = Resources.Load<VisualTreeAsset>("JamKitMenu");
            if (uxml != null)
            {
                refs.Document.visualTreeAsset = uxml;
                refs.Controller.MenuUxml = uxml;
            }

            var ps = PanelSettingsCreator.CreateAt($"{PanelSettingsCreator.DefaultDir}/{PanelSettingsCreator.DefaultName}.asset");
            if (ps != null) refs.Document.panelSettings = ps;

            return refs;
        }
    }
}
